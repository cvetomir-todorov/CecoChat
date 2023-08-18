using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Connections;

public interface IConnectionQueryRepo
{
    Task<Connection?> GetConnection(long userId, long connectionId);

    Task<IEnumerable<Connection>> GetConnections(long userId);
}
