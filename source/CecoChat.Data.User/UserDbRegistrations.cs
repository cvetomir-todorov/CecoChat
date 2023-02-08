using CecoChat.Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Data.User;

public static class UserDbRegistrations
{
    public static IServiceCollection AddUserDb(this IServiceCollection services, NpgsqlOptions options)
    {
        int commandTimeout = Convert.ToInt32(Math.Ceiling(options.CommandTimeout.TotalSeconds));

        return services.AddDbContext<UserDbContext>(builder =>
        {
            builder.UseNpgsql(options.ConnectionString, configure =>
            {
                configure.CommandTimeout(commandTimeout);
            });
        });
    }
}
