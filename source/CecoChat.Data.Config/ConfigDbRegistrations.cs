using CecoChat.Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Data.Config;

public static class ConfigDbRegistrations
{
    public static IServiceCollection AddConfigDb(this IServiceCollection services, NpgsqlOptions options)
    {
        return services.AddDbContextPool<ConfigDbContext>(
            optionsAction: builder =>
            {
                builder.UseNpgsql(options.ConnectionString, npgsql =>
                {
                    // TODO: move these into NpgsqlOptions class
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                });
            },
            poolSize: options.DbContextPoolSize);
    }
}
