using CecoChat.Contracts.User;
using CecoChat.Data.User.Connections;
using CecoChat.Server.Identity;
using Grpc.Core;

namespace CecoChat.Server.User.Endpoints;

public class ConnectionQueryService : ConnectionQuery.ConnectionQueryBase
{
    private readonly ILogger _logger;
    private readonly IConnectionQueryRepo _repo;

    public ConnectionQueryService(ILogger<ConnectionQueryService> logger, IConnectionQueryRepo repo)
    {
        _logger = logger;
        _repo = repo;
    }

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
