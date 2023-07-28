using CecoChat.Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Data.User;

public static class UserDbRegistrations
{
    public static IServiceCollection AddUserDb(this IServiceCollection services, NpgsqlOptions options)
    {
        return services.AddDbContextPool<UserDbContext>(
            optionsAction: builder =>
            {
                builder.UseNpgsql(options.ConnectionString, npgsql =>
                {
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                });
            },
            poolSize: options.DbContextPoolSize);
    }
}
