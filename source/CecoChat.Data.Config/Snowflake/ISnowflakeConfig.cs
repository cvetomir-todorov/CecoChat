namespace CecoChat.Data.Config.Snowflake;

public interface ISnowflakeConfig : IDisposable
{
    Task<bool> Initialize();

    IReadOnlyCollection<short> GetGeneratorIds(string server);
}
