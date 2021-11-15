namespace CecoChat.ConsoleClient.Api
{
    internal sealed class CreateSessionRequest
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }

    internal sealed class CreateSessionResponse
    {
        public string AccessToken { get; set; }
    }

    internal sealed class ConnectResponse
    {
        public string MessagingServerAddress { get; set; }

        public string HistoryServerAddress { get; set; }
    }
}
