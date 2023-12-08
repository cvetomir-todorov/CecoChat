namespace CecoChat.Data.Config.Snowflake;

public interface ISnowflakeConfig
{
    Task<bool> Initialize();

    IReadOnlyCollection<short> GetGeneratorIds(string server);
}
