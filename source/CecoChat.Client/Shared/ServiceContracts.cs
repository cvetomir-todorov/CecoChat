namespace CecoChat.Client.Shared
{
    public sealed class CreateSessionRequest
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }

    public sealed class CreateSessionResponse
    {
        public string AccessToken { get; set; }
    }

    public sealed class ConnectResponse
    {
        public string MessagingServerAddress { get; set; }

        public string HistoryServerAddress { get; set; }
    }
}
