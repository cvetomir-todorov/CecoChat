using CecoChat.User.Contracts;

namespace CecoChat.Data.User.Entities.Connections;

public interface IConnectionQueryRepo
{
    Task<Connection?> GetConnection(long userId, long connectionId);

    Task<IReadOnlyCollection<Connection>> GetConnections(long userId);
}

public interface ICachingConnectionQueryRepo : IConnectionQueryRepo
{ }
