namespace CecoChat.Messaging.Server.Clients
{
    public interface IClientOptions
    {
        public int SendMessagesHighWatermark { get; }

        public int MessageHistoryCountLimit { get; }
    }

    public sealed class ClientOptions : IClientOptions
    {
        public int SendMessagesHighWatermark { get; set; }

        public int MessageHistoryCountLimit { get; set; }
    }
}
