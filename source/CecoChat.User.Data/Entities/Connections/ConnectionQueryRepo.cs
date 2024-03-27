using CecoChat.User.Contracts;
using Common;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CecoChat.User.Data.Entities.Connections;

internal class ConnectionQueryRepo : IConnectionQueryRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;

    public ConnectionQueryRepo(
        ILogger<ConnectionQueryRepo> logger,
        UserDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Connection?> GetConnection(long userId, long connectionId)
    {
        long smallerId = Math.Min(userId, connectionId);
        long biggerId = Math.Max(userId, connectionId);

        ConnectionEntity? entity = await _dbContext.Connections
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.User1Id == smallerId && entity.User2Id == biggerId);

        if (entity == null)
        {
            _logger.LogTrace("Failed to fetch connection {ConnectionId} for user {UserId}", connectionId, userId);
            return null;
        }

        _logger.LogTrace("Fetched connection {ConnectionId} for user {UserId}", connectionId, userId);
        return MapConnection(userId, entity);
    }

    public async Task<IReadOnlyCollection<Connection>> GetConnections(long userId)
    {
        List<Connection> connections = await _dbContext.Connections
            .Where(entity => entity.User1Id == userId)
            .Union(_dbContext.Connections.Where(entity => entity.User2Id == userId))
            .Select(entity => MapConnection(userId, entity))
            .AsNoTracking()
            .ToListAsync();

        _logger.LogTrace("Fetched {ConnectionCount} connections for user {UserId}", connections.Count, userId);
        return connections;
    }

    private static Connection MapConnection(long userId, ConnectionEntity entity)
    {
        return new Connection
        {
            ConnectionId = MapConnectionId(userId, entity),
            Version = entity.Version.ToTimestamp(),
            Status = MapStatus(entity.Status),
            TargetUserId = entity.TargetId
        };
    }

    private static long MapConnectionId(long userId, ConnectionEntity entity)
    {
        return entity.User1Id == userId ?
            entity.User2Id :
            entity.User1Id;
    }

    private static ConnectionStatus MapStatus(ConnectionEntityStatus entityStatus)
    {
        switch (entityStatus)
        {
            case ConnectionEntityStatus.NotConnected:
                return ConnectionStatus.NotConnected;
            case ConnectionEntityStatus.Pending:
                return ConnectionStatus.Pending;
            case ConnectionEntityStatus.Connected:
                return ConnectionStatus.Connected;
            default:
                throw new EnumValueNotSupportedException(entityStatus);
        }
    }
}
