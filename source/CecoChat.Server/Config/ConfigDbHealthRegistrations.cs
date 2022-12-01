using CecoChat.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CecoChat.Server.Config;

public static class ConfigDbHealthRegistrations
{
    public static IHealthChecksBuilder AddConfigDb(
        this IHealthChecksBuilder builder,
        RedisOptions configOptions,
        string name = "config-db",
        string[]? tags = null)
    {
        ConfigurationOptions configConfig = CreateRedisConfiguration(configOptions);
        string connectionString = configConfig.ToString(includePassword: true);

        return builder.AddRedis(connectionString, name, tags: tags, timeout: configOptions.HealthTimeout);
    }

    private static ConfigurationOptions CreateRedisConfiguration(RedisOptions redisOptions)
    {
        ConfigurationOptions redisConfiguration = new()
        {
            ConnectRetry = redisOptions.ConnectRetry,
            ConnectTimeout = redisOptions.ConnectTimeout,
            KeepAlive = redisOptions.KeepAlive,
            ReconnectRetryPolicy = new ExponentialRetry(deltaBackOffMilliseconds: 1000)
        };

        foreach (string endpoint in redisOptions.Endpoints)
        {
            redisConfiguration.EndPoints.Add(endpoint);
        }

        return redisConfiguration;
    }
}
