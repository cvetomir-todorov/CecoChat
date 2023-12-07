namespace CecoChat.Data.Config.History;

public interface IHistoryConfig
{
    Task Initialize();

    int MessageCount { get; }
}
