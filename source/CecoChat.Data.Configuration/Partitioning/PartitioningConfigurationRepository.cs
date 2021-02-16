using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Kafka;
using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.Partitioning
{
    public interface IPartitioningConfigurationRepository
    {
        Task<RedisValueResult<int>> GetPartitionCount();

        IAsyncEnumerable<RedisValueResult<KeyValuePair<string, PartitionRange>>> GetServerPartitions();

        IAsyncEnumerable<RedisValueResult<KeyValuePair<string, string>>> GetServerAddresses();
    }

    public sealed class PartitioningConfigurationRepository : IPartitioningConfigurationRepository
    {
        private readonly IRedisContext _redisContext;

        public PartitioningConfigurationRepository(
            IRedisContext redisContext)
        {
            _redisContext = redisContext;
        }

        public async Task<RedisValueResult<int>> GetPartitionCount()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(PartitioningKeys.PartitionCount);

            if (value.IsNullOrEmpty ||
                !value.TryParse(out int partitionCount))
            {
                return RedisValueResult<int>.Failure();
            }

            return RedisValueResult<int>.Success(partitionCount);
        }

        public async IAsyncEnumerable<RedisValueResult<KeyValuePair<string, PartitionRange>>> GetServerPartitions()
        {
            IDatabase database = _redisContext.GetDatabase();
            HashEntry[] values = await database.HashGetAllAsync(PartitioningKeys.ServerPartitions);

            foreach (HashEntry hashEntry in values)
            {
                string server = hashEntry.Name;

                if (hashEntry.Value.IsNullOrEmpty ||
                    !PartitionRange.TryParse(hashEntry.Value, separator: '-', out PartitionRange partitions))
                {
                    yield return RedisValueResult<KeyValuePair<string, PartitionRange>>.Failure();
                }
                else
                {
                    KeyValuePair<string, PartitionRange> serverPartitions = new(server, partitions);
                    yield return RedisValueResult<KeyValuePair<string, PartitionRange>>.Success(serverPartitions);
                }
            }
        }

        public async IAsyncEnumerable<RedisValueResult<KeyValuePair<string, string>>> GetServerAddresses()
        {
            IDatabase database = _redisContext.GetDatabase();
            HashEntry[] values = await database.HashGetAllAsync(PartitioningKeys.ServerAddresses);

            foreach (HashEntry hashEntry in values)
            {
                string server = hashEntry.Name;

                if (hashEntry.Value.IsNullOrEmpty)
                {
                    yield return RedisValueResult<KeyValuePair<string, string>>.Failure();
                }
                else
                {
                    KeyValuePair<string, string> serverAddress = new(server, hashEntry.Value);
                    yield return RedisValueResult<KeyValuePair<string, string>>.Success(serverAddress);
                }
            }
        }
    }
}
