using System.Threading.Tasks;

namespace CecoChat.Data.Config.History;

public sealed class HistoryConfigUsage
{
    public bool UseMessageCount { get; init; }
}

public interface IHistoryConfig
{
    Task Initialize(HistoryConfigUsage usage);

    int ChatMessageCount { get; }
}