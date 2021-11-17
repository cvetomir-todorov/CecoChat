using System;
using Refit;

namespace CecoChat.Contracts.Bff
{
    public sealed class ConnectRequest
    {
        [AliasAs("username")]
        public string Username { get; set; }

        [AliasAs("password")]
        public string Password { get; set; }
    }

    public sealed class ConnectResponse
    {
        [AliasAs("clientID")]
        public Guid ClientID { get; set; }

        [AliasAs("accessToken")]
        public string AccessToken { get; set; }

        [AliasAs("messagingServerAddress")]
        public string MessagingServerAddress { get; set; }
    }
}