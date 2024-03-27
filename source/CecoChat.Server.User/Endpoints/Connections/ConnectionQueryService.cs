using CecoChat.Server.Identity;
using CecoChat.User.Contracts;
using CecoChat.User.Data.Entities.Connections;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.User.Endpoints.Connections;

public class ConnectionQueryService : ConnectionQuery.ConnectionQueryBase
{
    private readonly ILogger _logger;
    private readonly ICachingConnectionQueryRepo _repo;

    public ConnectionQueryService(ILogger<ConnectionQueryService> logger, ICachingConnectionQueryRepo repo)
    {
        _logger = logger;
        _repo = repo;
    }

    [Authorize(Policy = "user")]
    public override async Task<GetConnectionResponse> GetConnection(GetConnectionRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        Connection? connection = await _repo.GetConnection(userClaims.UserId, request.ConnectionId);

        GetConnectionResponse response = new();
        response.Connection = connection;

        _logger.LogTrace("Responding with {Connection} connection for user {UserId} and {ConnectionId}",
            connection == null ? "null" : "existing", userClaims.UserId, request.ConnectionId);
        return response;
    }

    [Authorize(Policy = "user")]
    public override async Task<GetConnectionsResponse> GetConnections(GetConnectionsRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        IEnumerable<Connection> connections = await _repo.GetConnections(userClaims.UserId);

        GetConnectionsResponse response = new();
        response.Connections.AddRange(connections);

        _logger.LogTrace("Responding with {ConnectionCount} connections for user {UserId}", response.Connections.Count, userClaims.UserId);
        return response;
    }
}
