namespace CecoChat.Data.Config.Snowflake;

internal sealed class SnowflakeValues
{
    public SnowflakeValues()
    {
        GeneratorIds = new Dictionary<string, List<short>>();
    }

    public IDictionary<string, List<short>> GeneratorIds { get; }
}