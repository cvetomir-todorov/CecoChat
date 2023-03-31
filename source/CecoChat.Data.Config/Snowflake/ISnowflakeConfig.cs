namespace CecoChat.Data.Config.Snowflake;

public interface ISnowflakeConfig : IDisposable
{
    Task Initialize();

    IReadOnlyCollection<short> GetGeneratorIds(string server);
}