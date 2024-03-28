using CecoChat.Client.Config;
using CecoChat.Config.Contracts;
using Common.Kafka;
using Microsoft.Extensions.Logging;

namespace CecoChat.DynamicConfig.Sections.Partitioning;

internal sealed class PartitioningRepo : IRepo<PartitioningValues>
{
    private readonly ILogger _logger;
    private readonly IConfigClient _configClient;

    public PartitioningRepo(
        ILogger<PartitioningRepo> logger,
        IConfigClient configClient)
    {
        _logger = logger;
        _configClient = configClient;
    }

    public async Task<PartitioningValues> Load(CancellationToken ct)
    {
        IReadOnlyCollection<ConfigElement> elements = await _configClient.GetConfigElements(ConfigKeys.Partitioning.Section, ct);
        PartitioningValues values = new();

        ConfigElement? partitionCount = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.Partitioning.Count, StringComparison.InvariantCultureIgnoreCase));
        if (partitionCount != null)
        {
            values.PartitionCount = ParsePartitionCount(partitionCount);
        }

        ConfigElement? partitions = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.Partitioning.Partitions, StringComparison.InvariantCultureIgnoreCase));
        if (partitions != null)
        {
            ParsePartitions(partitions, values);
        }

        ConfigElement? addresses = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.Partitioning.Addresses, StringComparison.InvariantCultureIgnoreCase));
        if (addresses != null)
        {
            ParseAddresses(addresses, values);
        }

        return values;
    }

    private int ParsePartitionCount(ConfigElement element)
    {
        if (!int.TryParse(element.Value, out int partitionCount))
        {
            _logger.LogError("Config {ConfigName} has invalid value '{ConfigValue}'", element.Name, element.Value);
        }

        return partitionCount;
    }

    private void ParsePartitions(ConfigElement element, PartitioningValues values)
    {
        string[] pairs = element.Value.Split(separator: ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string pair in pairs)
        {
            ParsePartitionsValue(element, pair, values);
        }
    }

    private void ParsePartitionsValue(ConfigElement element, string pair, PartitioningValues values)
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

    private void ParseAddresses(ConfigElement element, PartitioningValues values)
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
