using System;
using System.Collections.Generic;
using IdGen;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Identity.Server.Generation
{
    public interface IIdentityGenerator
    {
        long Generate(long originatorID);
    }

    public sealed class SnowflakeGenerator : IIdentityGenerator
    {
        private readonly ILogger _logger;
        private readonly List<IdGenerator> _generators;

        public SnowflakeGenerator(
            ILogger<SnowflakeGenerator> logger,
            IOptions<SnowflakeOptions> options)
        {
            _logger = logger;
            ISnowflakeOptions snowflakeOptions = options.Value;

            IdStructure idStructure = new(timestampBits: 41, generatorIdBits: 8, sequenceBits: 14);// IdGen doesn't use the sign bit
            DateTime epoch = snowflakeOptions.Epoch.ToUniversalTime();
            ITimeSource timeSource = new DefaultTimeSource(epoch);
            IdGeneratorOptions idGenOptions = new(idStructure, timeSource, SequenceOverflowStrategy.SpinWait);

            _generators = new List<IdGenerator>();
            foreach (short generatorID in snowflakeOptions.GeneratorIDs)
            {
                _generators.Add(new IdGenerator(generatorID, idGenOptions));
            }
        }

        public long Generate(long originatorID)
        {
            int index = (int) (originatorID % _generators.Count);
            long id = _generators[index].CreateId();
            _logger.LogTrace("Generated ID {0} for originator {1} using generator {2}.", id, originatorID, index);
            return id;
        }
    }
}
