using CecoChat.User.Contracts;
using Common;
using Common.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.User;

internal sealed class ConnectionClient : IConnectionClient
{
    private readonly ILogger _logger;
    private readonly UserClientOptions _options;
    private readonly ConnectionQuery.ConnectionQueryClient _connectionQueryClient;
    private readonly ConnectionCommand.ConnectionCommandClient _connectionCommandClient;
    private readonly IClock _clock;

    public ConnectionClient(
        ILogger<ConnectionClient> logger,
        IOptions<UserClientOptions> options,
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

    public async Task<IReadOnlyCollection<Connection>> GetConnections(long userId, string accessToken, CancellationToken ct)
    {
        GetConnectionsRequest request = new();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetConnectionsResponse response = await _connectionQueryClient.GetConnectionsAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {ConnectionCount} connections for user {UserId}", response.Connections.Count, userId);
        return response.Connections;
    }

    public async Task<Connection?> GetConnection(long userId, long connectionId, string accessToken, CancellationToken ct)
    {
        GetConnectionRequest request = new();
        request.ConnectionId = connectionId;

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetConnectionResponse response = await _connectionQueryClient.GetConnectionAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {Connection} connection for user {UserId} and {ConnectionId}",
            response.Connection == null ? "null" : "existing", userId, connectionId);
        return response.Connection;
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
            _logger.LogTrace("Received a successful invite from {UserId} to {ConnectionId}", userId, connectionId);
            return new InviteResult
            {
                Success = true,
                Version = response.Version.ToDateTime()
            };
        }
        if (response.MissingUser)
        {
            _logger.LogTrace("Received a failed invite from {UserId} to {ConnectionId} because the user is missing", userId, connectionId);
            return new InviteResult
            {
                MissingUser = true
            };
        }
        if (response.AlreadyExists)
        {
            _logger.LogTrace("Received a failed invite from {UserId} to {ConnectionId} because the connection already exists", userId, connectionId);
            return new InviteResult
            {
                AlreadyExists = true
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Received a failed invite from {UserId} to {ConnectionId} because the connection has been concurrently updated", userId, request.ConnectionId);
            return new InviteResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(InviteResponse));
    }

    public async Task<ApproveResult> Approve(long connectionId, DateTime version, long userId, string accessToken, CancellationToken ct)
    {
        ApproveRequest request = new();
        request.ConnectionId = connectionId;
        request.Version = version.ToTimestamp();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        ApproveResponse response = await _connectionCommandClient.ApproveAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace("Received a successful approval from {UserId} to {ConnectionId}", userId, request.ConnectionId);
            return new ApproveResult
            {
                Success = true,
                NewVersion = response.NewVersion.ToDateTime()
            };
        }
        if (response.MissingConnection)
        {
            _logger.LogTrace("Received a failed approval from {UserId} to {ConnectionId} because the connection is missing", userId, request.ConnectionId);
            return new ApproveResult
            {
                MissingConnection = true
            };
        }
        if (response.Invalid)
        {
            _logger.LogTrace("Received a failed approval from {UserId} to {ConnectionId} because of invalid target or status", userId, request.ConnectionId);
            return new ApproveResult
            {
                Invalid = true
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Received a failed approval from {UserId} to {ConnectionId} because the connection has been concurrently updated", userId, request.ConnectionId);
            return new ApproveResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(ApproveResponse));
    }

    public async Task<CancelResult> Cancel(long connectionId, DateTime version, long userId, string accessToken, CancellationToken ct)
    {
        CancelRequest request = new();
        request.ConnectionId = connectionId;
        request.Version = version.ToTimestamp();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        CancelResponse response = await _connectionCommandClient.CancelAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace("Received a successful cancel from {UserId} to {ConnectionId}", userId, request.ConnectionId);
            return new CancelResult
            {
                Success = true,
                NewVersion = response.NewVersion.ToDateTime()
            };
        }
        if (response.MissingConnection)
        {
            _logger.LogTrace("Received a failed cancel from {UserId} to {ConnectionId} because the connection is missing", userId, request.ConnectionId);
            return new CancelResult
            {
                MissingConnection = true
            };
        }
        if (response.Invalid)
        {
            _logger.LogTrace("Received a failed cancel from {UserId} to {ConnectionId} because of invalid status", userId, request.ConnectionId);
            return new CancelResult
            {
                Invalid = true
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Received a failed cancel from {UserId} to {ConnectionId} because the connection has been concurrently updated", userId, request.ConnectionId);
            return new CancelResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(CancelResponse));
    }

    public async Task<RemoveResult> Remove(long connectionId, DateTime version, long userId, string accessToken, CancellationToken ct)
    {
        RemoveRequest request = new();
        request.ConnectionId = connectionId;
        request.Version = version.ToTimestamp();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        RemoveResponse response = await _connectionCommandClient.RemoveAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace("Received a successful removal from {UserId} to {ConnectionId}", userId, request.ConnectionId);
            return new RemoveResult
            {
                Success = true,
                NewVersion = response.NewVersion.ToDateTime()
            };
        }
        if (response.MissingConnection)
        {
            _logger.LogTrace("Received a failed removal from {UserId} to {ConnectionId} because the connection is missing", userId, request.ConnectionId);
            return new RemoveResult
            {
                MissingConnection = true
            };
        }
        if (response.Invalid)
        {
            _logger.LogTrace("Received a failed removal from {UserId} to {ConnectionId} because of invalid status", userId, request.ConnectionId);
            return new RemoveResult
            {
                Invalid = true
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Received a failed removal from {UserId} to {ConnectionId} because the connection has been concurrently updated", userId, request.ConnectionId);
            return new RemoveResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(RemoveResponse));
    }
}
