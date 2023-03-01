using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Npgsql.Health;

public static class NpgsqlHealthRegistrations
{
    public static IHealthChecksBuilder AddNpgsql(
        this IHealthChecksBuilder builder,
        NpgsqlOptions npgsqlOptions,
        string name,
        string[]? tags = null,
        TimeSpan? timeout = null)
    {
        return builder.AddNpgSql(
            npgsqlConnectionString: npgsqlOptions.ConnectionString,
            name: name,
            tags: tags,
            timeout: timeout);
    }
}
