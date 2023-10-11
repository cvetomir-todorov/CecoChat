using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Npgsql.Health;

public static class NpgsqlHealthRegistrations
{
    public static IHealthChecksBuilder AddNpgsql(
        this IHealthChecksBuilder builder,
        string name,
        NpgsqlOptions npgsqlOptions,
        string[]? tags = null)
    {
        return builder.AddNpgSql(
            connectionString: npgsqlOptions.ConnectionString,
            name: name,
            tags: tags,
            timeout: npgsqlOptions.HealthTimeout);
    }
}
