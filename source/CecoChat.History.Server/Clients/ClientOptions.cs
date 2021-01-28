namespace CecoChat.History.Server.Clients
{
    public interface IClientOptions
    {
        public int MessageHistoryCountLimit { get; }
    }

    public sealed class ClientOptions : IClientOptions
    {
        public int MessageHistoryCountLimit { get; set; }
    }
}
