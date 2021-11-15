using System;

namespace CecoChat.Contracts.Bff
{
    public sealed class ConnectRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public sealed class ConnectResponse
    {
        public Guid ClientID { get; set; }
        public string AccessToken { get; set; }
        public string MessagingServerAddress { get; set; }
    }
}