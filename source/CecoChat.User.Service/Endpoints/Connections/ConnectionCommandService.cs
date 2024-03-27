using CecoChat.Server.Identity;
using CecoChat.User.Contracts;
using CecoChat.User.Data.Entities.Connections;
using CecoChat.User.Service.Backplane;
using Common;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.User.Service.Endpoints.Connections;

public class ConnectionCommandService : ConnectionCommand.ConnectionCommandBase
{
    private readonly ILogger _logger;
    private readonly IConnectionQueryRepo _queryRepo;
    private readonly IConnectionCommandRepo _commandRepo;
    private readonly IConnectionNotifyProducer _producer;

    public ConnectionCommandService(
        ILogger<ConnectionCommandService> logger,
        IConnectionQueryRepo queryRepo,
        IConnectionCommandRepo commandRepo,
        IConnectionNotifyProducer producer)
    {
        _logger = logger;
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
        _producer = producer;
    }

    [Authorize(Policy = "user")]
    public override async Task<InviteResponse> Invite(InviteRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        Connection? existingConnection = await _queryRepo.GetConnection(userClaims.UserId, request.ConnectionId);
        if (existingConnection == null)
        {
            return await AddNewConnectionForInvite(userClaims.UserId, request.ConnectionId);
        }
        else
        {
            return await UpdateExistingConnectionForInvite(userClaims.UserId, existingConnection);
        }
    }

    private async Task<InviteResponse> AddNewConnectionForInvite(long userId, long connectionId)
    {
        Connection connection = new()
        {
            ConnectionId = connectionId,
            Status = ConnectionStatus.Pending,
            TargetUserId = connectionId
        };

        AddConnectionResult result = await _commandRepo.AddConnection(userId, connection);
        if (result.Success)
        {
            NotifyConnectionChange(userId, connection, result.Version);

            _logger.LogTrace("Responding with a successful invite from {UserId} to {ConnectionId}", userId, connectionId);
            return new InviteResponse
            {
                Success = true,
                Version = result.Version.ToTimestamp()
            };
        }
        if (result.MissingUser)
        {
            _logger.LogTrace("Responding with a failed invite from {UserId} to {ConnectionId} because the connection ID is missing", userId, connectionId);
            return new InviteResponse
            {
                MissingUser = true
            };
        }
        if (result.AlreadyExists)
        {
            _logger.LogTrace("Responding with a failed invite from {UserId} to {ConnectionId} because the connection already exists", userId, connectionId);
            return new InviteResponse
            {
                AlreadyExists = true
            };
        }

        throw new ProcessingFailureException(typeof(AddConnectionResult));
    }

    private async Task<InviteResponse> UpdateExistingConnectionForInvite(long userId, Connection existingConnection)
    {
        if (existingConnection.Status != ConnectionStatus.NotConnected)
        {
            _logger.LogTrace("Responding with a failed invite from {UserId} to {ConnectionId} because the connection already exists", userId, existingConnection.ConnectionId);
            return new InviteResponse
            {
                AlreadyExists = true
            };
        }

        existingConnection.Status = ConnectionStatus.Pending;
        existingConnection.TargetUserId = existingConnection.ConnectionId;

        UpdateConnectionResult result = await _commandRepo.UpdateConnection(userId, existingConnection);
        if (result.Success)
        {
            NotifyConnectionChange(userId, existingConnection, result.NewVersion);

            _logger.LogTrace("Responding with a successful invite from {UserId} to {ConnectionId}", userId, existingConnection.ConnectionId);
            return new InviteResponse
            {
                Success = true,
                Version = result.NewVersion.ToTimestamp()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed invite from {UserId} to {ConnectionId} because the connection has been concurrently updated", userId, existingConnection.ConnectionId);
            return new InviteResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(UpdateConnectionResult));
    }

    [Authorize(Policy = "user")]
    public override async Task<ApproveResponse> Approve(ApproveRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        Connection? existingConnection = await _queryRepo.GetConnection(userClaims.UserId, request.ConnectionId);
        if (existingConnection == null)
        {
            _logger.LogTrace("Responding with a failed approval from {UserId} to {ConnectionId} because the connection is missing", userClaims.UserId, request.ConnectionId);
            return new ApproveResponse
            {
                MissingConnection = true
            };
        }
        if (existingConnection.TargetUserId != userClaims.UserId || existingConnection.Status != ConnectionStatus.Pending)
        {
            _logger.LogTrace("Responding with a failed approval from {UserId} to {ConnectionId} because of invalid target or status", userClaims.UserId, request.ConnectionId);
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
            NotifyConnectionChange(userClaims.UserId, existingConnection, result.NewVersion);

            _logger.LogTrace("Responding with a successful approval from {UserId} to {ConnectionId}", userClaims.UserId, request.ConnectionId);
            return new ApproveResponse
            {
                Success = true,
                NewVersion = result.NewVersion.ToTimestamp()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed approval from {UserId} to {ConnectionId} because the connection has been concurrently updated", userClaims.UserId, request.ConnectionId);
            return new ApproveResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(UpdateConnectionResult));
    }

    [Authorize(Policy = "user")]
    public override async Task<CancelResponse> Cancel(CancelRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        Connection? existingConnection = await _queryRepo.GetConnection(userClaims.UserId, request.ConnectionId);
        if (existingConnection == null)
        {
            _logger.LogTrace("Responding with a failed cancel from {UserId} to {ConnectionId} because the connection is missing", userClaims.UserId, request.ConnectionId);
            return new CancelResponse
            {
                MissingConnection = true
            };
        }
        if (existingConnection.Status != ConnectionStatus.Pending)
        {
            _logger.LogTrace("Responding with a failed cancel from {UserId} to {ConnectionId} because of invalid status", userClaims.UserId, request.ConnectionId);
            return new CancelResponse
            {
                Invalid = true
            };
        }

        existingConnection.Version = request.Version;
        existingConnection.Status = ConnectionStatus.NotConnected;
        existingConnection.TargetUserId = 0;

        UpdateConnectionResult result = await _commandRepo.UpdateConnection(userClaims.UserId, existingConnection);
        if (result.Success)
        {
            NotifyConnectionChange(userClaims.UserId, existingConnection, result.NewVersion);

            _logger.LogTrace("Responding with a successful cancel from {UserId} to {ConnectionId}", userClaims.UserId, request.ConnectionId);
            return new CancelResponse
            {
                Success = true,
                NewVersion = result.NewVersion.ToTimestamp()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed cancel from {UserId} to {ConnectionId} because the connection has been concurrently updated", userClaims.UserId, request.ConnectionId);
            return new CancelResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(RemoveConnectionResult));
    }

    [Authorize(Policy = "user")]
    public override async Task<RemoveResponse> Remove(RemoveRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        Connection? existingConnection = await _queryRepo.GetConnection(userClaims.UserId, request.ConnectionId);
        if (existingConnection == null)
        {
            _logger.LogTrace("Responding with a failed removal from {UserId} to {ConnectionId} because the connection is missing", userClaims.UserId, request.ConnectionId);
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
        existingConnection.Status = ConnectionStatus.NotConnected;
        existingConnection.TargetUserId = 0;

        UpdateConnectionResult result = await _commandRepo.UpdateConnection(userClaims.UserId, existingConnection);
        if (result.Success)
        {
            NotifyConnectionChange(userClaims.UserId, existingConnection, result.NewVersion);

            _logger.LogTrace("Responding with a successful removal from {UserId} to {ConnectionId}", userClaims.UserId, request.ConnectionId);
            return new RemoveResponse
            {
                Success = true,
                NewVersion = result.NewVersion.ToTimestamp()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed removal from {UserId} to {ConnectionId} because the connection has been concurrently updated", userClaims.UserId, request.ConnectionId);
            return new RemoveResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(RemoveConnectionResult));
    }

    private void NotifyConnectionChange(long userId, Connection changedConnection, DateTime newVersion)
    {
        changedConnection.Version = newVersion.ToTimestamp();
        _producer.NotifyConnectionChange(userId, changedConnection);
    }
}
