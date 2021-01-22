using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Cassandra
{
    public static class CassandraExtensions
    {
        public static IServiceCollection AddCassandra(this IServiceCollection services, IConfiguration cassandraConfigurationSection)
        {
            return services
                .AddSingleton<ICassandraDbContext, CassandraDbContext>()
                .Configure<CassandraOptions>(cassandraConfigurationSection);
        }
    }
}
