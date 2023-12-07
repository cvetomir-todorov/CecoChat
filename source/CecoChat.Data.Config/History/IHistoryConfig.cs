namespace CecoChat.Data.Config.History;

public interface IHistoryConfig
{
    Task<bool> Initialize();

    int MessageCount { get; }
}
