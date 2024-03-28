using CecoChat.Config.Sections.Snowflake;
using Common;
using IdGen;
using Microsoft.Extensions.Options;

namespace CecoChat.IdGen.Service.Endpoints;

public interface IIdentityGenerator
{
    long GenerateOne();

    IEnumerable<long> GenerateMany(int count);
}

public sealed class SnowflakeGenerator : IIdentityGenerator
{
    private readonly ILogger _logger;
    private readonly List<IdGenerator> _generators;
    private readonly Random _random;

    public SnowflakeGenerator(
        ILogger<SnowflakeGenerator> logger,
        IOptions<ConfigOptions> configOptions,
        ISnowflakeConfig snowflakeConfig)
    {
        _logger = logger;
        _random = new();

        // IdGen doesn't use the sign bit so the sum of bits is 63
        // we support:
        // 2^8  = 256 generators
        // 2^14 = 16384 IDs per tick per generator
        IdStructure idStructure = new(timestampBits: 41, generatorIdBits: 8, sequenceBits: 14);
        ITimeSource timeSource = new DefaultTimeSource(Snowflake.Epoch);
        IdGeneratorOptions idGenOptions = new(idStructure, timeSource, SequenceOverflowStrategy.SpinWait);

        _generators = new List<IdGenerator>();
        IReadOnlyCollection<short> generatorIds = snowflakeConfig.GetGeneratorIds(configOptions.Value.ServerId);
        foreach (short generatorId in generatorIds)
        {
            _generators.Add(new IdGenerator(generatorId, idGenOptions));
        }
    }

    public long GenerateOne()
    {
        long id = ChooseGenerator(out int generatorIndex).CreateId();
        _logger.LogTrace("Generated ID {Id} using generator {GeneratorIndex}", id, generatorIndex);
        return id;
    }

    public IEnumerable<long> GenerateMany(int count)
    {
        IEnumerable<long> ids = ChooseGenerator(out int generatorIndex).Take(count);
        _logger.LogTrace("Generated {IdCount} IDs using generator {GeneratorIndex}", count, generatorIndex);
        return ids;
    }

    private IdGenerator ChooseGenerator(out int generatorIndex)
    {
        if (_generators.Count == 1)
        {
            generatorIndex = 0;
        }
        else
        {
            // min value is inclusive
            // max value is exclusive
            generatorIndex = _random.Next(minValue: 0, maxValue: _generators.Count);
        }

        return _generators[generatorIndex];
    }
}
