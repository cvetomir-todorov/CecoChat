using FluentValidation;

namespace CecoChat.Config.Sections.Snowflake;

internal sealed class SnowflakeValidator : AbstractValidator<SnowflakeValues>
{
    private const int GeneratorIdMin = 0;
    private const int GeneratorIdMax = 255;

    public SnowflakeValidator()
    {
        RuleFor(x => x.GeneratorIds).Custom(ValidateServerGeneratorIDs);
    }

    private static void ValidateServerGeneratorIDs(IDictionary<string, List<short>> serverGeneratorIds, ValidationContext<SnowflakeValues> context)
    {
        HashSet<string> uniqueServers = new(capacity: serverGeneratorIds.Count);
        HashSet<short> allUniqueIds = new(capacity: serverGeneratorIds.Count);
        int allIdsCount = 0;
        HashSet<short> serverUniqueIds = new();

        if (serverGeneratorIds.Count == 0)
        {
            context.AddFailure("No snowflake servers configured.");
        }

        foreach (KeyValuePair<string, List<short>> pair in serverGeneratorIds)
        {
            string server = pair.Key;
            List<short> generatorIds = pair.Value;

            if (!uniqueServers.Add(server))
            {
                context.AddFailure($"Duplicate snowflake server {server}.");
            }
            if (generatorIds.Count == 0)
            {
                context.AddFailure($"No generator IDs configured for snowflake server {server}.");
            }

            serverUniqueIds.Clear();
            allIdsCount += generatorIds.Count;

            foreach (short generatorId in generatorIds)
            {
                if (generatorId < GeneratorIdMin || generatorId > GeneratorIdMax)
                {
                    context.AddFailure($"Generator ID {generatorId} should be within [{GeneratorIdMin}, {GeneratorIdMax}].");
                }

                serverUniqueIds.Add(generatorId);
                allUniqueIds.Add(generatorId);
            }

            if (serverUniqueIds.Count < generatorIds.Count)
            {
                context.AddFailure($"Duplicate generator IDs [{string.Join(",", generatorIds)}] for snowflake server {server}.");
            }
        }

        if (allUniqueIds.Count < allIdsCount)
        {
            context.AddFailure($"Duplicate generator IDs within individual or across all snowflake servers.");
        }
    }
}
