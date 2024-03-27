using CecoChat.User.Contracts;
using CecoChat.User.Data.Infra;
using Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CecoChat.User.Data.Entities.Connections;

internal class ConnectionCommandRepo : IConnectionCommandRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;
    private readonly IDataUtility _dataUtility;

    public ConnectionCommandRepo(
        ILogger<ConnectionCommandRepo> logger,
        UserDbContext dbContext,
        IDataUtility dataUtility)
    {
        _logger = logger;
        _dbContext = dbContext;
        _dataUtility = dataUtility;
    }

    public async Task<AddConnectionResult> AddConnection(long userId, Connection connection)
    {
        ConnectionEntity entity = CreateEntityWithUserIds(userId, connection.ConnectionId);
        entity.Version = DateTime.UtcNow;
        entity.Status = MapStatus(connection.Status);
        entity.TargetId = connection.TargetUserId;

        _dbContext.Connections.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Added a new connection from {UserId} to {ConnectionId}", userId, connection.ConnectionId);

            return new AddConnectionResult
            {
                Success = true,
                Version = entity.Version
            };
        }
        catch (DbUpdateException dbUpdateException) when (dbUpdateException.InnerException is PostgresException postgresException)
        {
            // https://www.postgresql.org/docs/current/errcodes-appendix.html
            if (postgresException.SqlState == "23503") // foreign key violation
            {
                if (postgresException.MessageText.Contains("connections_user1_id_foreign") ||
                    postgresException.MessageText.Contains("connections_user2_id_foreign"))
                {
                    return new AddConnectionResult
                    {
                        MissingUser = true
                    };
                }
            }
            if (postgresException.SqlState == "23505") // unique key violation
            {
                if (postgresException.MessageText.Contains("connections_pkey"))
                {
                    return new AddConnectionResult
                    {
                        AlreadyExists = true
                    };
                }
            }

            throw;
        }
    }

    public async Task<UpdateConnectionResult> UpdateConnection(long userId, Connection connection)
    {
        ConnectionEntity entity = CreateEntityWithUserIds(userId, connection.ConnectionId);
        entity.Status = MapStatus(connection.Status);
        entity.TargetId = connection.TargetUserId;

        EntityEntry<ConnectionEntity> entry = _dbContext.Connections.Attach(entity);
        entry.Property(e => e.Status).IsModified = true;
        entry.Property(e => e.TargetId).IsModified = true;
        DateTime newVersion = _dataUtility.SetVersion(entry, connection.Version.ToDateTime());

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Updated existing connection from {UserId} to {ConnectionId} with status {ConnectionStatus}", userId, connection.ConnectionId, connection.Status);

            return new UpdateConnectionResult
            {
                Success = true,
                NewVersion = newVersion
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateConnectionResult
            {
                ConcurrentlyUpdated = true
            };
        }
    }

    public async Task<RemoveConnectionResult> RemoveConnection(long userId, Connection connection)
    {
        ConnectionEntity entity = CreateEntityWithUserIds(userId, connection.ConnectionId);

        EntityEntry<ConnectionEntity> entry = _dbContext.Connections.Attach(entity);
        entry.State = EntityState.Deleted;
        _dataUtility.SetVersion(entry, connection.Version.ToDateTime());

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Removed existing connection from {UserId} to {ConnectionId}", userId, connection.ConnectionId);

            return new RemoveConnectionResult
            {
                Success = true
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return new RemoveConnectionResult
            {
                ConcurrentlyUpdated = true
            };
        }
    }

    private static ConnectionEntity CreateEntityWithUserIds(long userId, long connectionId)
    {
        if (userId == connectionId)
        {
            throw new InvalidOperationException($"User ID and connection ID should be different, but have the same value {userId}.");
        }

        long smallerId = Math.Min(userId, connectionId);
        long biggerId = Math.Max(userId, connectionId);

        return new ConnectionEntity
        {
            User1Id = smallerId,
            User2Id = biggerId
        };
    }

    private static ConnectionEntityStatus MapStatus(ConnectionStatus status)
    {
        switch (status)
        {
            case ConnectionStatus.NotConnected:
                return ConnectionEntityStatus.NotConnected;
            case ConnectionStatus.Pending:
                return ConnectionEntityStatus.Pending;
            case ConnectionStatus.Connected:
                return ConnectionEntityStatus.Connected;
            default:
                throw new EnumValueNotSupportedException(status);
        }
    }
}
