using Common.Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.User.Data;

public static class UserDbRegistrations
{
    public static IServiceCollection AddUserDb(this IServiceCollection services, NpgsqlOptions options)
    {
        return services.AddDbContextPool<UserDbContext>(
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
