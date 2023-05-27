namespace CecoChat.Data.Config.Snowflake;

internal sealed class SnowflakeConfigValues
{
    public SnowflakeConfigValues()
    {
        GeneratorIds = new Dictionary<string, List<short>>();
    }

    public IDictionary<string, List<short>> GeneratorIds { get; }
}
