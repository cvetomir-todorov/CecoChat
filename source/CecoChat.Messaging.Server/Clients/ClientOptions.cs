namespace CecoChat.Messaging.Server.Clients
{
    public interface IClientOptions
    {
        public int MaxClients { get; }

        public int SendMessagesHighWatermark { get; }
    }

    public sealed class ClientOptions : IClientOptions
    {
        public int MaxClients { get; set; }

        public int SendMessagesHighWatermark { get; set; }
    }
}
