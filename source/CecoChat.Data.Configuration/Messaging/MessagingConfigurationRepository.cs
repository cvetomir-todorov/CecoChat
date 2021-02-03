using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.Messaging
{
    public interface IMessagingConfigurationRepository
    {
        Task<int> GetPartitionCount();

        IAsyncEnumerable<KeyValuePair<string, PartitionRange>> GetServerPartitions();

        IAsyncEnumerable<KeyValuePair<string, string>> GetServerAddresses();
    }

    public sealed class MessagingConfigurationRepository : IMessagingConfigurationRepository
    {
        private readonly IRedisContext _redisContext;

        public MessagingConfigurationRepository(
            IRedisContext redisContext)
        {
            _redisContext = redisContext;
        }

        public async Task<int> GetPartitionCount()
        {
            IDatabase database = _redisContext.GetDatabase();
            const string key = MessagingKeys.PartitionCount;
            RedisValue value = await database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                throw new ConfigurationException(key);
            }
            if (!value.TryParse(out int partitionCount))
            {
                throw new ConfigurationException(key, value);
            }

            return partitionCount;
        }

        public async IAsyncEnumerable<KeyValuePair<string, PartitionRange>> GetServerPartitions()
        {
            IDatabase database = _redisContext.GetDatabase();
            const string key = MessagingKeys.ServerPartitions;
            HashEntry[] values = await database.HashGetAllAsync(key);

            foreach (HashEntry hashEntry in values)
            {
                string server = hashEntry.Name;

                if (hashEntry.Value.IsNullOrEmpty)
                {
                    throw new ConfigurationException(key);
                }
                if (!PartitionRange.TryParse(hashEntry.Value.ToString(), separator: '-', out PartitionRange partitions))
                {
                    throw new ConfigurationException(key, hashEntry.Value);
                }

                yield return new KeyValuePair<string, PartitionRange>(server, partitions);
            }
        }

        public async IAsyncEnumerable<KeyValuePair<string, string>> GetServerAddresses()
        {
            IDatabase database = _redisContext.GetDatabase();
            const string key = MessagingKeys.ServerAddresses;
            HashEntry[] values = await database.HashGetAllAsync(key);

            foreach (HashEntry hashEntry in values)
            {
                string server = hashEntry.Name;

                if (hashEntry.Value.IsNullOrEmpty)
                {
                    throw new ConfigurationException(key);
                }

                string address = hashEntry.Value.ToString();

                yield return new KeyValuePair<string, string>(server, address);
            }
        }
    }
}
