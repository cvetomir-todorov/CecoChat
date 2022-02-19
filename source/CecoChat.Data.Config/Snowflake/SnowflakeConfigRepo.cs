using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Config.Snowflake
{
    internal interface ISnowflakeConfigRepo
    {
        Task<SnowflakeConfigValues> GetValues();
    }

    internal sealed class SnowflakeConfigRepo : ISnowflakeConfigRepo
    {
        private readonly IRedisContext _redisContext;

        public SnowflakeConfigRepo(IRedisContext redisContext)
        {
            _redisContext = redisContext;
        }

        public async Task<SnowflakeConfigValues> GetValues()
        {
            SnowflakeConfigValues values = new();
            await GetServerGeneratorIDs(values);
            return values;
        }

        private async Task GetServerGeneratorIDs(SnowflakeConfigValues values)
        {
            IDatabase database = _redisContext.GetDatabase();
            HashEntry[] pairs = await database.HashGetAllAsync(SnowflakeKeys.ServerGeneratorIDs);

            foreach (HashEntry pair in pairs)
            {
                string server = pair.Name;
                List<short> generatorIDs = new();
                values.ServerGeneratorIDs[server] = generatorIDs;

                string value = pair.Value;
                string[] generatorIDStrings = value.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (string generatorIDString in generatorIDStrings)
                {
                    if (short.TryParse(generatorIDString, out short generatorID))
                    {
                        generatorIDs.Add(generatorID);
                    }
                }
            }
        }
    }
}