using CecoChat.Data.Config.Snowflake;
using CecoChat.Server.IdGen.HostedServices;
using IdGen;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.IdGen.Endpoints;

public interface IIdentityGenerator
{
    long GenerateOne(long originatorId);

    IEnumerable<long> GenerateMany(long originatorId, int count);
}

public sealed class SnowflakeGenerator : IIdentityGenerator
{
    private readonly ILogger _logger;
    private readonly INonCryptoHash _hashFunction;
    private readonly List<IdGenerator> _generators;

    public SnowflakeGenerator(
        ILogger<SnowflakeGenerator> logger,
        IOptions<ConfigOptions> configOptions,
        ISnowflakeConfig snowflakeConfig,
        INonCryptoHash hashFunction)
    {
        _logger = logger;
        _hashFunction = hashFunction;

        // IdGen doesn't use the sign bit so the sum of bits is 63
        IdStructure idStructure = new(timestampBits: 41, generatorIdBits: 8, sequenceBits: 14);
        ITimeSource timeSource = new DefaultTimeSource(Snowflake.Epoch);
        IdGeneratorOptions idGenOptions = new(idStructure, timeSource, SequenceOverflowStrategy.SpinWait);

        _generators = new List<IdGenerator>();
        IReadOnlyCollection<short> generatorIds = snowflakeConfig.GetGeneratorIds(configOptions.Value.ServerID);
        foreach (short generatorId in generatorIds)
        {
            _generators.Add(new IdGenerator(generatorId, idGenOptions));
        }
    }

    public long GenerateOne(long originatorId)
    {
        long id = ChooseGenerator(originatorId, out int generatorIndex).CreateId();
        _logger.LogTrace("Generated ID {Id} for originator {OriginatorId} using generator {GeneratorIndex}", id, originatorId, generatorIndex);
        return id;
    }

    public IEnumerable<long> GenerateMany(long originatorId, int count)
    {
        IEnumerable<long> ids = ChooseGenerator(originatorId, out int generatorIndex).Take(count);
        _logger.LogTrace("Generated {IdCount} IDs for originator {OriginatorId} using generator {GeneratorIndex}", count, originatorId, generatorIndex);
        return ids;
    }

    private IdGenerator ChooseGenerator(long originatorId, out int generatorIndex)
    {
        if (_generators.Count == 1)
        {
            generatorIndex = 0;
        }
        else
        {
            int hash = _hashFunction.Compute(originatorId);
            generatorIndex = Math.Abs(hash) % _generators.Count;
        }

        return _generators[generatorIndex];
    }
}
