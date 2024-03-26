using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Common.Redis.Health;

public static class RedisHealthRegistrations
{
    public static IHealthChecksBuilder AddRedis(
        this IHealthChecksBuilder builder,
        string name,
        RedisOptions configOptions,
        string[]? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"Argument {nameof(name)} should not be null or whitespace.", nameof(name));
        }

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
