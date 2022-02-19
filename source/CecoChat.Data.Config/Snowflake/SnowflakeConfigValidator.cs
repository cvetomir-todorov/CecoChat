using System.Collections.Generic;
using FluentValidation;

namespace CecoChat.Data.Config.Snowflake
{
    internal sealed class SnowflakeConfigValidator : AbstractValidator<SnowflakeConfigValues>
    {
        public SnowflakeConfigValidator()
        {
            RuleFor(x => x.ServerGeneratorIDs).Custom(ValidateServerGeneratorIDs);
        }

        private static void ValidateServerGeneratorIDs(IDictionary<string, List<short>> serverGeneratorIDs, ValidationContext<SnowflakeConfigValues> context)
        {
            HashSet<string> uniqueServers = new(capacity: serverGeneratorIDs.Count);
            HashSet<short> allUniqueIDs = new(capacity: serverGeneratorIDs.Count);
            int allIDsCount = 0;
            HashSet<short> serverUniqueIDs = new();

            if (serverGeneratorIDs.Count == 0)
            {
                context.AddFailure("No snowflake servers configured.");
            }

            foreach (KeyValuePair<string, List<short>> pair in serverGeneratorIDs)
            {
                string server = pair.Key;
                List<short> generatorIDs = pair.Value;

                if (!uniqueServers.Add(server))
                {
                    context.AddFailure($"Duplicate snowflake server {server}.");
                }
                if (generatorIDs.Count == 0)
                {
                    context.AddFailure($"No generator IDs configured for snowflake server {server}.");
                }

                serverUniqueIDs.Clear();
                allIDsCount += generatorIDs.Count;

                foreach (short generatorID in generatorIDs)
                {
                    serverUniqueIDs.Add(generatorID);
                    allUniqueIDs.Add(generatorID);
                }

                if (serverUniqueIDs.Count < generatorIDs.Count)
                {
                    context.AddFailure($"Duplicate generator IDs [{string.Join(",", generatorIDs)}] for snowflake server {server}.");
                }
            }

            if (allUniqueIDs.Count < allIDsCount)
            {
                context.AddFailure($"Duplicate generator IDs within individual or across all snowflake servers.");
            }
        }
    }
}