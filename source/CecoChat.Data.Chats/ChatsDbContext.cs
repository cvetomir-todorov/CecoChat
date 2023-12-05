using Cassandra;
using CecoChat.Cassandra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Data.Chats;

public interface IChatsDbContext : ICassandraDbContext
{
    string Keyspace { get; }

    ISession Session { get; }

    PreparedStatement PrepareQuery(string cql);
}

internal sealed class ChatsDbContext : CassandraDbContext, IChatsDbContext
{
    private readonly ILogger _logger;

    public ChatsDbContext(
        ILogger<ChatsDbContext> logger,
        IOptions<CassandraOptions> options) : base(logger, options)
    {
        _logger = logger;
    }

    public string Keyspace => "chats";

    public ISession Session => GetSession(Keyspace);

    public PreparedStatement PrepareQuery(string cql)
    {
        PreparedStatement preparedQuery = Session.Prepare(cql);
        _logger.LogInformation("Prepared CQL '{Cql}'", cql);
        return preparedQuery;
    }
}
