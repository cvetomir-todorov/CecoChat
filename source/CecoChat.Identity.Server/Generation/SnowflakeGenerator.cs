using System.Collections.Generic;
using System.Linq;
using IdGen;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Identity.Server.Generation
{
    public interface IIdentityGenerator
    {
        long GenerateOne(long originatorID);

        IEnumerable<long> GenerateMany(long originatorID, int count);
    }

    public sealed class SnowflakeGenerator : IIdentityGenerator
    {
        private readonly ILogger _logger;
        private readonly INonCryptoHash _hashFunction;
        private readonly List<IdGenerator> _generators;

        public SnowflakeGenerator(
            ILogger<SnowflakeGenerator> logger,
            IOptions<SnowflakeOptions> options,
            INonCryptoHash hashFunction)
        {
            _logger = logger;
            _hashFunction = hashFunction;
            ISnowflakeOptions snowflakeOptions = options.Value;

            // IdGen doesn't use the sign bit so the sum of bits is 63
            IdStructure idStructure = new(timestampBits: 41, generatorIdBits: 8, sequenceBits: 14);
            ITimeSource timeSource = new DefaultTimeSource(Snowflake.Epoch);
            IdGeneratorOptions idGenOptions = new(idStructure, timeSource, SequenceOverflowStrategy.SpinWait);

            _generators = new List<IdGenerator>();
            foreach (short generatorID in snowflakeOptions.GeneratorIDs)
            {
                _generators.Add(new IdGenerator(generatorID, idGenOptions));
            }
        }

        public long GenerateOne(long originatorID)
        {
            long id = ChooseGenerator(originatorID, out int generatorIndex).CreateId();
            _logger.LogTrace("Generated ID {0} for originator {1} using generator {2}.", id, originatorID, generatorIndex);
            return id;
        }

        public IEnumerable<long> GenerateMany(long originatorID, int count)
        {
            IEnumerable<long> ids = ChooseGenerator(originatorID, out int generatorIndex).Take(count);
            _logger.LogTrace("Generated {0} IDs for originator {1} using generator {2}.", count, originatorID, generatorIndex);
            return ids;
        }

        private IdGenerator ChooseGenerator(long originatorID, out int generatorIndex)
        {
            int hash = _hashFunction.Compute(originatorID);
            generatorIndex = hash % _generators.Count;
            return _generators[generatorIndex];
        }
    }
}
