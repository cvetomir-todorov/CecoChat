using CecoChat.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Config.Partitioning;

internal interface IPartitioningRepo
{
    Task<PartitioningValues> GetValues();
}

internal sealed class PartitioningRepo : IPartitioningRepo
{
    private readonly ILogger _logger;
    private readonly ConfigDbContext _dbContext;

    public PartitioningRepo(
        ILogger<PartitioningRepo> logger,
        ConfigDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<PartitioningValues> GetValues()
    {
        List<ElementEntity> elements = await _dbContext.Elements
            .Where(e => e.Name.StartsWith(ConfigKeys.Partitioning.Section))
            .AsNoTracking()
            .ToListAsync();

        PartitioningValues values = new();

        ElementEntity? partitionCount = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.Partitioning.Count, StringComparison.InvariantCultureIgnoreCase));
        if (partitionCount != null)
        {
            values.PartitionCount = ParsePartitionCount(partitionCount);
        }

        ElementEntity? partitions = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.Partitioning.Partitions, StringComparison.InvariantCultureIgnoreCase));
        if (partitions != null)
        {
            ParsePartitions(partitions, values);
        }

        ElementEntity? addresses = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.Partitioning.Addresses, StringComparison.InvariantCultureIgnoreCase));
        if (addresses != null)
        {
            ParseAddresses(addresses, values);
        }

        return values;
    }

    private int ParsePartitionCount(ElementEntity element)
    {
        if (!int.TryParse(element.Value, out int partitionCount))
        {
            _logger.LogError("Config {ConfigName} has invalid value '{ConfigValue}'", element.Name, element.Value);
        }

        return partitionCount;
    }

    private void ParsePartitions(ElementEntity element, PartitioningValues values)
    {
        string[] pairs = element.Value.Split(separator: ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string pair in pairs)
        {
            ParsePartitionsValue(element, pair, values);
        }
    }

    private void ParsePartitionsValue(ElementEntity element, string pair, PartitioningValues values)
    {
        string[] parts = pair.Split(separator: '=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            _logger.LogError("Config {ConfigName} has an invalid pair '{Pair}'", element.Name, pair);
            return;
        }

        string server = parts[0];
        string partitionsValue = parts[1];

        if (PartitionRange.TryParse(partitionsValue, separator: '-', out PartitionRange partitions))
        {
            for (int partition = partitions.Lower; partition <= partitions.Upper; ++partition)
            {
                values.PartitionServerMap[partition] = server;
            }
        }
        else
        {
            _logger.LogError("Config {ConfigName} has an invalid partition range in '{Pair}'", element.Name, pair);
            return;
        }

        values.ServerPartitionMap[server] = partitions;
    }

    private void ParseAddresses(ElementEntity element, PartitioningValues values)
    {
        string[] pairs = element.Value.Split(separator: ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string pair in pairs)
        {
            string[] parts = pair.Split(separator: '=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                _logger.LogError("Config {ConfigName} has an invalid pair '{Pair}'", element.Name, pair);
            }

            string server = parts[0];
            string address = parts[1];

            values.ServerAddressMap[server] = address;
        }
    }
}
