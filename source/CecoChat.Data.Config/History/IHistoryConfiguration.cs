using System.Threading.Tasks;

namespace CecoChat.Data.Config.History
{
    public sealed class HistoryConfigurationUsage
    {
        public bool UseServerAddress { get; init; }

        public bool UseMessageCount { get; init; }
    }

    public interface IHistoryConfiguration
    {
        Task Initialize(HistoryConfigurationUsage usage);

        string ServerAddress { get; }

        int UserMessageCount { get; }

        int DialogMessageCount { get; }
    }
}