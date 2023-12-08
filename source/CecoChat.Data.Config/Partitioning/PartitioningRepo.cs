﻿using CecoChat.Kafka;
using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Config.Partitioning;

internal interface IPartitioningRepo
{
    Task<PartitioningValues> GetValues();
}

internal sealed class PartitioningRepo : IPartitioningRepo
{
    private readonly ILogger _logger;
    private readonly IRedisContext _redisContext;

    public PartitioningRepo(
        ILogger<PartitioningRepo> logger,
        IRedisContext redisContext)
    {
        _logger = logger;
        _redisContext = redisContext;
    }

    public async Task<PartitioningValues> GetValues()
    {
        PartitioningValues values = new();

        values.PartitionCount = await GetPartitionCount();
        await GetPartitions(values);
        await GetAddresses(values);

        return values;
    }

    private async Task<int> GetPartitionCount()
    {
        IDatabase database = _redisContext.GetDatabase();
        RedisValue value = await database.StringGetAsync(PartitioningKeys.PartitionCount);
        value.TryParse(out int partitionCount);
        return partitionCount;
    }

    private async Task GetPartitions(PartitioningValues values)
    {
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

    private async Task GetAddresses(PartitioningValues values)
    {
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