using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Connections;

public interface IConnectionQueryRepo
{
    Task<Connection?> GetConnection(long userId, long connectionId);

    Task<IReadOnlyCollection<Connection>> GetConnections(long userId);
}

public interface ICachingConnectionQueryRepo : IConnectionQueryRepo
{ }
