using Common.Npgsql;
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
                    if (options.EnableRetryOnFailure)
                    {
                        npgsql.EnableRetryOnFailure(options.MaxRetryCount, options.MaxRetryDelay, errorCodesToAdd: null);
                    }
                });
            },
            poolSize: options.DbContextPoolSize);
    }
}
