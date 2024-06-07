using Cassandra;
using Common.Cassandra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Chats.Data;

public interface IChatsDbContext : ICassandraDbContext
{
    string Keyspace { get; }

    ISession Session { get; }

    PreparedStatement PrepareStatement(string cql);

    void PrepareUdt<TUserDefinedType>(string keyspace, string typeName)
        where TUserDefinedType : new();
}

internal sealed class ChatsDbContext : CassandraDbContext, IChatsDbContext
{
    private readonly ILogger _logger;

    public ChatsDbContext(
        ILogger<ChatsDbContext> logger,
        IOptions<CassandraOptions<IChatsDbContext>> options) : base(logger, options)
    {
        _logger = logger;
    }

    public string Keyspace => "chats";

    public ISession Session => GetSession(Keyspace);

    public PreparedStatement PrepareStatement(string cql)
    {
        PreparedStatement preparedQuery = Session.Prepare(cql);
        _logger.LogInformation("Prepared statement '{Cql}'", cql);
        return preparedQuery;
    }

    public void PrepareUdt<TUserDefinedType>(string keyspace, string typeName)
        where TUserDefinedType : new()
    {
        Session.UserDefinedTypes.Define(
            UdtMap.For<TUserDefinedType>(typeName, keyspace));
        _logger.LogInformation("Prepared user-defined type {UserDefinedType} to match {Keyspace}.{Udt}", typeof(TUserDefinedType).Name, keyspace, typeName);
    }
}
