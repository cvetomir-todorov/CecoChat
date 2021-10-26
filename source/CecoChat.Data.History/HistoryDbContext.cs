using Cassandra;
using CecoChat.Cassandra;
using Microsoft.Extensions.Options;

namespace CecoChat.Data.History
{
    public interface IHistoryDbContext : ICassandraDbContext
    {
        string Keyspace { get; }

        ISession Session { get; }
    }

    internal sealed class HistoryDbContext : CassandraDbContext, IHistoryDbContext
    {
        public HistoryDbContext(IOptions<CassandraOptions> options) : base(options)
        {}

        public string Keyspace => "history";

        public ISession Session => GetSession(Keyspace);
    }
}
