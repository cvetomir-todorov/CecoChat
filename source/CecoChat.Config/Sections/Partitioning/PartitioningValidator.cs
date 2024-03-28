using Common.Kafka;
using FluentValidation;

namespace CecoChat.Config.Sections.Partitioning;

internal sealed class PartitioningValidator : AbstractValidator<PartitioningValues>
{
    private const int PartitionCountMin = 2;
    private const int PartitionCountMax = 10000;

    public PartitioningValidator()
    {
        RuleFor(x => x.PartitionCount).InclusiveBetween(from: PartitionCountMin, to: PartitionCountMax);
        RuleFor(x => x.PartitionServerMap).Custom(ValidatePartitionServerMap);
        RuleFor(x => x.ServerPartitionMap).Custom(ValidateServerPartitionMap);
        RuleFor(x => x.ServerAddressMap).Custom(ValidateServerAddressMap);
    }

    private static void ValidatePartitionServerMap(IDictionary<int, string> partitionServerMap, ValidationContext<PartitioningValues> context)
    {
        PartitioningValues values = context.InstanceToValidate;
        if (!IsPartitionCountValid(values.PartitionCount))
        {
            return;
        }

        List<int> withWhitespaceServer = new();
        List<int> withMissingServer = new();

        for (int partition = 0; partition < values.PartitionCount; ++partition)
        {
            if (partitionServerMap.TryGetValue(partition, out string? server))
            {
                if (string.IsNullOrWhiteSpace(server))
                {
                    withWhitespaceServer.Add(partition);
                }
            }
            else
            {
                withMissingServer.Add(partition);
            }
        }

        if (withWhitespaceServer.Count > 0)
        {
            context.AddFailure($"Server is null or whitespace for partitions [{string.Join(',', withWhitespaceServer)}].");
        }
        if (withMissingServer.Count > 0)
        {
            context.AddFailure($"Server is missing for partitions [{string.Join(',', withMissingServer)}].");
        }
    }

    private static void ValidateServerPartitionMap(IDictionary<string, PartitionRange> serverPartitionRangeMap, ValidationContext<PartitioningValues> context)
    {
        PartitioningValues values = context.InstanceToValidate;
        if (!IsPartitionCountValid(values.PartitionCount))
        {
            return;
        }

        HashSet<int> uniquePartitions = new();

        foreach (KeyValuePair<string, PartitionRange> pair in serverPartitionRangeMap)
        {
            string server = pair.Key;
            PartitionRange partitions = pair.Value;
            ValidateSingleServerPartitions(server, partitions, values.PartitionCount, uniquePartitions, context);
        }
    }

    private static void ValidateSingleServerPartitions(
        string server, PartitionRange partitions,
        int partitionCount, HashSet<int> uniquePartitions,
        ValidationContext<PartitioningValues> context)
    {
        List<int> overlappingPartitions = new();
        List<int> invalidPartitions = new();

        if (string.IsNullOrWhiteSpace(server))
        {
            context.AddFailure($"Server for partitions {partitions} is null or whitespace.");
        }

        for (int partition = partitions.Lower; partition <= partitions.Upper; ++partition)
        {
            if (!uniquePartitions.Add(partition))
            {
                overlappingPartitions.Add(partition);
            }
            if (partition < 0 || partition > partitionCount - 1)
            {
                invalidPartitions.Add(partition);
            }
        }

        if (overlappingPartitions.Count > 0)
        {
            context.AddFailure($"Server {server} has partitions that overlap with existing ones [{string.Join(',', overlappingPartitions)}].");
        }
        if (invalidPartitions.Count > 0)
        {
            context.AddFailure($"Server {server} has invalid partitions [{string.Join(',', invalidPartitions)}].");
        }
    }

    private static void ValidateServerAddressMap(IDictionary<string, string> serverAddressMap, ValidationContext<PartitioningValues> context)
    {
        PartitioningValues values = context.InstanceToValidate;
        if (!IsPartitionCountValid(values.PartitionCount))
        {
            return;
        }

        foreach (KeyValuePair<string, string> pair in serverAddressMap)
        {
            string server = pair.Key;
            string address = pair.Value;

            if (string.IsNullOrWhiteSpace(server))
            {
                context.AddFailure($"Server for address {address} is null or whitespace.");
            }
            if (!Uri.TryCreate(address, UriKind.Absolute, out _))
            {
                context.AddFailure($"Address '{address}' for server {server} is not valid.");
            }
        }
    }

    private static bool IsPartitionCountValid(int partitionCount)
    {
        bool isValid = partitionCount >= PartitionCountMin && partitionCount <= PartitionCountMax;
        return isValid;
    }
}
