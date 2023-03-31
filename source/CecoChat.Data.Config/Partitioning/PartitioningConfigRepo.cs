using CecoChat.Kafka;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Config.Partitioning;

internal interface IPartitioningConfigRepo
{
    Task<PartitioningConfigValues> GetValues(PartitioningConfigUsage usage);
}

internal sealed class PartitioningConfigRepo : IPartitioningConfigRepo
{
    private readonly ILogger _logger;
    private readonly IRedisContext _redisContext;

    public PartitioningConfigRepo(
        ILogger<PartitioningConfigRepo> logger,
        IRedisContext redisContext)
    {
        _logger = logger;
        _redisContext = redisContext;
    }

    public async Task<PartitioningConfigValues> GetValues(PartitioningConfigUsage usage)
    {
        PartitioningConfigValues values = new();

        values.PartitionCount = await GetPartitionCount();
        await GetPartitions(usage, values);
        await GetAddresses(usage, values);

        return values;
    }

    private async Task<int> GetPartitionCount()
    {
        IDatabase database = _redisContext.GetDatabase();
        RedisValue value = await database.StringGetAsync(PartitioningKeys.PartitionCount);
        value.TryParse(out int partitionCount);
        return partitionCount;
    }

    private async Task GetPartitions(PartitioningConfigUsage usage, PartitioningConfigValues values)
    {
        if (!usage.UsePartitions && !usage.UseAddresses)
        {
            return;
        }

        IDatabase database = _redisContext.GetDatabase();
        HashEntry[] pairs = await database.HashGetAllAsync(PartitioningKeys.Partitions);

        foreach (HashEntry pair in pairs)
        {
            string? server = pair.Name;
            string? partitionsValue = pair.Value;

            if (server == null || partitionsValue == null)
            {
                _logger.LogError("Empty values are present in hash config {HashConfig}", PartitioningKeys.Partitions);
                continue;
            }

            if (PartitionRange.TryParse(partitionsValue, separator: '-', out PartitionRange partitions))
            {
                for (int partition = partitions.Lower; partition <= partitions.Upper; ++partition)
                {
                    values.PartitionServerMap[partition] = server;
                }
            }

            values.ServerPartitionMap[server] = partitions;
        }
    }

    private async Task GetAddresses(PartitioningConfigUsage usage, PartitioningConfigValues values)
    {
        if (!usage.UseAddresses)
        {
            return;
        }

        IDatabase database = _redisContext.GetDatabase();
        HashEntry[] pairs = await database.HashGetAllAsync(PartitioningKeys.Addresses);

        foreach (HashEntry pair in pairs)
        {
            string? server = pair.Name;
            string? address = pair.Value;

            if (server == null || address == null)
            {
                _logger.LogError("Empty values are present in hash config {HashConfig}", PartitioningKeys.Addresses);
                continue;
            }

            values.ServerAddressMap[server] = address;
        }
    }
}