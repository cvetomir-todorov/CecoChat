using System.Threading.Tasks;
using CecoChat.Kafka;
using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.Partitioning
{
    internal interface IPartitioningConfigurationRepository
    {
        Task<PartitioningConfigurationValues> GetValues(PartitioningConfigurationUsage usage);
    }

    internal sealed class PartitioningConfigurationRepository : IPartitioningConfigurationRepository
    {
        private readonly IRedisContext _redisContext;

        public PartitioningConfigurationRepository(
            IRedisContext redisContext)
        {
            _redisContext = redisContext;
        }

        public async Task<PartitioningConfigurationValues> GetValues(PartitioningConfigurationUsage usage)
        {
            PartitioningConfigurationValues values = new();

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

        private async Task GetServerPartitions(PartitioningConfigurationUsage usage, PartitioningConfigurationValues values)
        {
            if (!usage.UseServerPartitions && !usage.UseServerAddresses)
            {
                return;
            }

            IDatabase database = _redisContext.GetDatabase();
            HashEntry[] pairs = await database.HashGetAllAsync(PartitioningKeys.ServerPartitions);

            foreach (HashEntry pair in pairs)
            {
                string server = pair.Name;
                if (PartitionRange.TryParse(pair.Value, separator: '-', out PartitionRange partitions))
                {
                    for (int partition = partitions.Lower; partition <= partitions.Upper; ++partition)
                    {
                        values.PartitionServerMap[partition] = server;
                    }
                }

                values.ServerPartitionsMap[server] = partitions;
            }
        }

        public async Task GetServerAddresses(PartitioningConfigurationUsage usage, PartitioningConfigurationValues values)
        {
            if (!usage.UseServerAddresses)
            {
                return;
            }

            IDatabase database = _redisContext.GetDatabase();
            HashEntry[] pairs = await database.HashGetAllAsync(PartitioningKeys.ServerAddresses);

            foreach (HashEntry pair in pairs)
            {
                string server = pair.Name;
                string address = pair.Value;
                values.ServerAddressMap[server] = address;
            }
        }
    }
}
