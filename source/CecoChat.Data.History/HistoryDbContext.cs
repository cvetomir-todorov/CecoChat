using Cassandra;
using CecoChat.Cassandra;
using Microsoft.Extensions.Options;

namespace CecoChat.Data.History
{
    public interface IHistoryDbContext : ICassandraDbContext
    {
        string MessagingKeyspace { get; }

        ISession Messaging { get; }
    }

    internal sealed class HistoryDbContext : CassandraDbContext, IHistoryDbContext
    {
        public HistoryDbContext(IOptions<CassandraOptions> options) : base(options)
        {}

        public string MessagingKeyspace => "messaging";

        public ISession Messaging => GetSession(MessagingKeyspace);
    }
}
