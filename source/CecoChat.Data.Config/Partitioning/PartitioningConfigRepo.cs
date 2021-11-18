using System.Threading.Tasks;
using CecoChat.Kafka;
using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Config.Partitioning
{
    internal interface IPartitioningConfigRepo
    {
        Task<PartitioningConfigValues> GetValues(PartitioningConfigUsage usage);
    }

    internal sealed class PartitioningConfigRepo : IPartitioningConfigRepo
    {
        private readonly IRedisContext _redisContext;

        public PartitioningConfigRepo(
            IRedisContext redisContext)
        {
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
                string server = pair.Name;
                string address = pair.Value;
                values.ServerAddressMap[server] = address;
            }
        }
    }
}
