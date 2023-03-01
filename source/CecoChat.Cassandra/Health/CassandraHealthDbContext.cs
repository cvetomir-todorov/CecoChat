using Microsoft.Extensions.Options;

namespace CecoChat.Cassandra.Health;

public interface ICassandraHealthDbContext : ICassandraDbContext
{ }

public class CassandraHealthDbContext : CassandraDbContext, ICassandraHealthDbContext
{
    public CassandraHealthDbContext(IOptions<CassandraOptions> options) : base(options)
    { }
}
