using System.Collections.Generic;

namespace CecoChat.Data.Config.Snowflake;

internal sealed class SnowflakeConfigValues
{
    public SnowflakeConfigValues()
    {
        ServerGeneratorIDs = new Dictionary<string, List<short>>();
    }

    public IDictionary<string, List<short>> ServerGeneratorIDs { get; }
}