namespace CecoChat.Client.Shared
{
    public sealed class CreateSessionRequest
    {
        public long UserID { get; set; }
    }

    public sealed class CreateSessionResponse
    {
        public string MessagingServerAddress { get; set; }

        public string HistoryServerAddress { get; set; }
    }
}
