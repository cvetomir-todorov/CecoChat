using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Data.User.Connections;
using CecoChat.Server.Identity;
using Grpc.Core;

namespace CecoChat.Server.User.Endpoints;

public class ConnectionCommandService : ConnectionCommand.ConnectionCommandBase
{
    private readonly ILogger _logger;
    private readonly IConnectionQueryRepo _queryRepo;
    private readonly IConnectionCommandRepo _commandRepo;

    public ConnectionCommandService(
        ILogger<ConnectionCommandService> logger,
        IConnectionQueryRepo queryRepo,
        IConnectionCommandRepo commandRepo)
    {
        _logger = logger;
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
    }

    public override async Task<InviteResponse> Invite(InviteRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        Connection connection = new()
        {
            ConnectionId = request.ConnectionId,
            Status = ConnectionStatus.Pending,
            TargetUserId = request.ConnectionId
        };

        AddConnectionResult result = await _commandRepo.AddConnection(userClaims.UserId, connection);
        if (result.Success)
        {
            _logger.LogTrace("Responding with a successful invite from {UserId} to {ConnectionId}", userClaims.UserId, request.ConnectionId);
            return new InviteResponse
            {
                Success = true,
                Version = result.Version.ToUuid()
            };
        }
        if (result.AlreadyExists)
        {
            _logger.LogTrace("Responding with a failed invite from {UserId} to {ConnectionId} because the connection has already exists", userClaims.UserId, request.ConnectionId);
            return new InviteResponse
            {
                AlreadyExists = true
            };
        }

        throw new ProcessingFailureException(typeof(AddConnectionResult));
    }

    public override async Task<ApproveResponse> Approve(ApproveRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        Connection? existingConnection = await _queryRepo.GetConnection(userClaims.UserId, request.ConnectionId);
        if (existingConnection == null)
        {
            _logger.LogTrace("Responding with failed approval from {UserId} to {ConnectionId} because the connection is missing", userClaims.UserId, request.ConnectionId);
            return new ApproveResponse
            {
                MissingConnection = true
            };
        }
        if (existingConnection.TargetUserId != userClaims.UserId || existingConnection.Status != ConnectionStatus.Pending)
        {
            _logger.LogTrace("Responding with failed approval from {UserId} to {ConnectionId} because of invalid target or status", userClaims.UserId, request.ConnectionId);
            return new ApproveResponse
            {
                Invalid = true
            };
        }

        existingConnection.Version = request.Version;
        existingConnection.Status = ConnectionStatus.Connected;
        existingConnection.TargetUserId = 0;

        UpdateConnectionResult result = await _commandRepo.UpdateConnection(userClaims.UserId, existingConnection);
        if (result.Success)
        {
            _logger.LogTrace("Responding with a successful approval from {UserId} to {ConnectionId}", userClaims.UserId, request.ConnectionId);
            return new ApproveResponse
            {
                Success = true,
                NewVersion = result.NewVersion.ToUuid()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed approval from {UserId} to {ConnectionId}", userClaims.UserId, request.ConnectionId);
            return new ApproveResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(UpdateConnectionResult));
    }

    public override async Task<CancelResponse> Cancel(CancelRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        Connection? existingConnection = await _queryRepo.GetConnection(userClaims.UserId, request.ConnectionId);
        if (existingConnection == null)
        {
            _logger.LogTrace("Responding with failed cancel from {UserId} to {ConnectionId} because the connection is missing", userClaims.UserId, request.ConnectionId);
            return new CancelResponse
            {
                MissingConnection = true
            };
        }
        if (existingConnection.Status != ConnectionStatus.Pending)
        {
            _logger.LogTrace("Responding with failed cancel from {UserId} to {ConnectionId} because of invalid status", userClaims.UserId, request.ConnectionId);
            return new CancelResponse
            {
                Invalid = true
            };
        }

        existingConnection.Version = request.Version;

        RemoveConnectionResult result = await _commandRepo.RemoveConnection(userClaims.UserId, existingConnection);
        if (result.Success)
        {
            _logger.LogTrace("Responding with a successful cancel from {UserId} to {ConnectionId}", userClaims.UserId, request.ConnectionId);
            return new CancelResponse
            {
                Success = true
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed cancel from {UserId} to {ConnectionId}", userClaims.UserId, request.ConnectionId);
            return new CancelResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(RemoveConnectionResult));
    }

    public override async Task<RemoveResponse> Remove(RemoveRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        Connection? existingConnection = await _queryRepo.GetConnection(userClaims.UserId, request.ConnectionId);
        if (existingConnection == null)
        {
            _logger.LogTrace("Responding with failed removal from {UserId} to {ConnectionId} because the connection is missing", userClaims.UserId, request.ConnectionId);
            return new RemoveResponse
            {
                MissingConnection = true
            };
        }
        if (existingConnection.Status != ConnectionStatus.Connected)
        {
            _logger.LogTrace("Responding with a failed removal from {UserId} to {ConnectionId} because of invalid status", userClaims.UserId, request.ConnectionId);
            return new RemoveResponse
            {
                Invalid = true
            };
        }

        existingConnection.Version = request.Version;

        RemoveConnectionResult result = await _commandRepo.RemoveConnection(userClaims.UserId, existingConnection);
        if (result.Success)
        {
            _logger.LogTrace("Responding with a successful removal from {UserId} to {ConnectionId}", userClaims.UserId, request.ConnectionId);
            return new RemoveResponse
            {
                Success = true
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed removal from {UserId} to {ConnectionId}", userClaims.UserId, request.ConnectionId);
            return new RemoveResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(RemoveConnectionResult));
    }
}
