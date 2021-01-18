namespace CecoChat.Messaging.Server.Clients
{
    public interface IClientOptions
    {
        public int SendMessagesHighWatermark { get; }
    }

    public sealed class ClientOptions : IClientOptions
    {
        public int SendMessagesHighWatermark { get; set; }
    }
}
