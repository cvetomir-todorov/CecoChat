using Cassandra;
using CecoChat.Cassandra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Data.State;

public interface IStateDbContext : ICassandraDbContext
{
    string Keyspace { get; }

    ISession Session { get; }

    PreparedStatement PrepareQuery(string cql);
}

internal sealed class StateDbContext : CassandraDbContext, IStateDbContext
{
    private readonly ILogger _logger;

    public StateDbContext(
        ILogger<StateDbContext> logger,
        IOptions<CassandraOptions> options) : base(options)
    {
        _logger = logger;
    }

    public string Keyspace => "state";

    public ISession Session => GetSession(Keyspace);

    public PreparedStatement PrepareQuery(string cql)
    {
        PreparedStatement preparedQuery = Session.Prepare(cql);
        _logger.LogInformation("Prepared CQL '{0}'.", cql);
        return preparedQuery;
    }
}