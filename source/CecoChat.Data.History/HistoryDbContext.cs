using Cassandra;
using CecoChat.Cassandra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Data.History
{
    public interface IHistoryDbContext : ICassandraDbContext
    {
        string Keyspace { get; }

        ISession Session { get; }

        PreparedStatement PrepareQuery(string cql);
    }

    internal sealed class HistoryDbContext : CassandraDbContext, IHistoryDbContext
    {
        private readonly ILogger _logger;

        public HistoryDbContext(
            ILogger<HistoryDbContext> logger,
            IOptions<CassandraOptions> options) : base(options)
        {
            _logger = logger;
        }

        public string Keyspace => "history";

        public ISession Session => GetSession(Keyspace);

        public PreparedStatement PrepareQuery(string cql)
        {
            PreparedStatement preparedQuery = Session.Prepare(cql);
            _logger.LogDebug("Prepared CQL '{0}'.", cql);
            return preparedQuery;
        }
    }
}
