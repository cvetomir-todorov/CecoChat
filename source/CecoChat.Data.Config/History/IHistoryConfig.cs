using System.Threading.Tasks;

namespace CecoChat.Data.Config.History
{
    public sealed class HistoryConfigUsage
    {
        public bool UseServerAddress { get; init; }

        public bool UseMessageCount { get; init; }
    }

    public interface IHistoryConfig
    {
        Task Initialize(HistoryConfigUsage usage);

        string ServerAddress { get; }

        int UserMessageCount { get; }

        int DialogMessageCount { get; }
    }
}