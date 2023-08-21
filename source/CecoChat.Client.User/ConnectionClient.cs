using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.User;

internal sealed class ConnectionClient : IConnectionClient
{
    private readonly ILogger _logger;
    private readonly UserOptions _options;
    private readonly ConnectionQuery.ConnectionQueryClient _connectionQueryClient;
    private readonly ConnectionCommand.ConnectionCommandClient _connectionCommandClient;
    private readonly IClock _clock;

    public ConnectionClient(
        ILogger<ConnectionClient> logger,
        IOptions<UserOptions> options,
        ConnectionQuery.ConnectionQueryClient connectionQueryClient,
        ConnectionCommand.ConnectionCommandClient connectionCommandClient,
        IClock clock)
    {
        _logger = logger;
        _options = options.Value;
        _connectionQueryClient = connectionQueryClient;
        _connectionCommandClient = connectionCommandClient;
        _clock = clock;

        _logger.LogInformation("User address set to {Address}", _options.Address);
    }

    public async Task<IEnumerable<Connection>> GetConnections(long userId, string accessToken, CancellationToken ct)
    {
        GetConnectionsRequest request = new();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetConnectionsResponse response = await _connectionQueryClient.GetConnectionsAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {ConnectionCount} connections for user {UserId}", response.Connections.Count, userId);
        return response.Connections;
    }

    public async Task<InviteResult> Invite(long connectionId, long userId, string accessToken, CancellationToken ct)
    {
        InviteRequest request = new();
        request.ConnectionId = connectionId;

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        InviteResponse response = await _connectionCommandClient.InviteAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace("Received successful invite from {UserId} to {ConnectionId}", userId, connectionId);
            return new InviteResult
            {
                Success = true,
                Version = response.Version.ToGuid()
            };
        }
        if (response.MissingUser)
        {
            _logger.LogTrace("Received failed invite from {UserId} to {ConnectionId} because the user is missing", userId, connectionId);
            return new InviteResult
            {
                MissingUser = true
            };
        }
        if (response.AlreadyExists)
        {
            _logger.LogTrace("Received failed invite from {UserId} to {ConnectionId} because the connection already exists", userId, connectionId);
            return new InviteResult
            {
                AlreadyExists = true
            };
        }

        throw new ProcessingFailureException(typeof(InviteResponse));
    }
    
    // TODO: add logging

    public async Task<ApproveResult> Approve(long connectionId, Guid version, long userId, string accessToken, CancellationToken ct)
    {
        ApproveRequest request = new();
        request.ConnectionId = connectionId;
        request.Version = version.ToUuid();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        ApproveResponse response = await _connectionCommandClient.ApproveAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            return new ApproveResult
            {
                Success = true,
                NewVersion = response.NewVersion.ToGuid()
            };
        }
        if (response.MissingConnection)
        {
            return new ApproveResult
            {
                MissingConnection = true
            };
        }
        if (response.Invalid)
        {
            return new ApproveResult
            {
                Invalid = true
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            return new ApproveResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(ApproveResponse));
    }

    public async Task<CancelResult> Cancel(long connectionId, Guid version, long userId, string accessToken, CancellationToken ct)
    {
        CancelRequest request = new();
        request.ConnectionId = connectionId;
        request.Version = version.ToUuid();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        CancelResponse response = await _connectionCommandClient.CancelAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            return new CancelResult
            {
                Success = true
            };
        }
        if (response.MissingConnection)
        {
            return new CancelResult
            {
                MissingConnection = true
            };
        }
        if (response.Invalid)
        {
            return new CancelResult
            {
                Invalid = true
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            return new CancelResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(CancelResponse));
    }

    public async Task<RemoveResult> Remove(long connectionId, Guid version, long userId, string accessToken, CancellationToken ct)
    {
        RemoveRequest request = new();
        request.ConnectionId = connectionId;
        request.Version = version.ToUuid();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        RemoveResponse response = await _connectionCommandClient.RemoveAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            return new RemoveResult
            {
                Success = true
            };
        }
        if (response.MissingConnection)
        {
            return new RemoveResult
            {
                MissingConnection = true
            };
        }
        if (response.Invalid)
        {
            return new RemoveResult
            {
                Invalid = true
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            return new RemoveResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(RemoveResponse));
    }
}
