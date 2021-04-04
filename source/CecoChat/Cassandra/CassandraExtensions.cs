using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Cassandra
{
    public static class CassandraExtensions
    {
        public static IServiceCollection AddCassandra(this IServiceCollection services, IConfiguration cassandraConfiguration)
        {
            return services
                .AddSingleton<ICassandraDbContext, CassandraDbContext>()
                .Configure<CassandraOptions>(cassandraConfiguration);
        }

        public static IServiceCollection AddCassandra<TDbContext, TDbContextImplementation>(
                this IServiceCollection services, IConfiguration cassandraConfiguration)
            where TDbContext : class, ICassandraDbContext
            where TDbContextImplementation : CassandraDbContext, TDbContext
        {
            return services
                .AddSingleton<TDbContext, TDbContextImplementation>()
                .Configure<CassandraOptions>(cassandraConfiguration);
        }
    }
}
