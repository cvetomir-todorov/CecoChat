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
        await GetServerPartitions(usage, values);
        await GetServerAddresses(usage, values);

        return values;
    }

    private async Task<int> GetPartitionCount()
    {
        IDatabase database = _redisContext.GetDatabase();
        RedisValue value = await database.StringGetAsync(PartitioningKeys.PartitionCount);
        value.TryParse(out int partitionCount);
        return partitionCount;
    }

    private async Task GetServerPartitions(PartitioningConfigUsage usage, PartitioningConfigValues values)
    {
        if (!usage.UseServerPartitions && !usage.UseServerAddresses)
        {
            return;
        }

        IDatabase database = _redisContext.GetDatabase();
        HashEntry[] pairs = await database.HashGetAllAsync(PartitioningKeys.ServerPartitions);

        foreach (HashEntry pair in pairs)
        {
            string? server = pair.Name;
            string? partitionsValue = pair.Value;

            if (server == null || partitionsValue == null)
            {
                _logger.LogError("Empty values are present in hash config {HashConfig}", PartitioningKeys.ServerPartitions);
                continue;
            }

            if (PartitionRange.TryParse(partitionsValue, separator: '-', out PartitionRange partitions))
            {
                for (int partition = partitions.Lower; partition <= partitions.Upper; ++partition)
                {
                    values.PartitionServerMap[partition] = server;
                }
            }

            values.ServerPartitionsMap[server] = partitions;
        }
    }

    private async Task GetServerAddresses(PartitioningConfigUsage usage, PartitioningConfigValues values)
    {
        if (!usage.UseServerAddresses)
        {
            return;
        }

        IDatabase database = _redisContext.GetDatabase();
        HashEntry[] pairs = await database.HashGetAllAsync(PartitioningKeys.ServerAddresses);

        foreach (HashEntry pair in pairs)
        {
            string? server = pair.Name;
            string? address = pair.Value;

            if (server == null || address == null)
            {
                _logger.LogError("Empty values are present in hash config {HashConfig}", PartitioningKeys.ServerAddresses);
                continue;
            }

            values.ServerAddressMap[server] = address;
        }
    }
}