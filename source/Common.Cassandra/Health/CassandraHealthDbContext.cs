using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Common.Cassandra.Health;

public interface ICassandraHealthDbContext : ICassandraDbContext
{ }

public class CassandraHealthDbContext : CassandraDbContext, ICassandraHealthDbContext
{
    public CassandraHealthDbContext(ILogger<CassandraHealthDbContext> logger, IOptions<CassandraOptions<CassandraHealthDbContext>> options) : base(logger, options)
    { }
}
