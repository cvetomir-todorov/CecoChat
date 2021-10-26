using Cassandra;
using CecoChat.Cassandra;
using Microsoft.Extensions.Options;

namespace CecoChat.Data.State
{
    public interface IStateDbContext : ICassandraDbContext
    {
        string Keyspace { get; }

        ISession Session { get; }
    }

    internal sealed class StateDbContext : CassandraDbContext, IStateDbContext
    {
        public StateDbContext(IOptions<CassandraOptions> options) : base(options)
        {}

        public string Keyspace => "state";

        public ISession Session => GetSession(Keyspace);
    }
}