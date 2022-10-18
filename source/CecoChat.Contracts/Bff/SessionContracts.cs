using System;
using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class CreateSessionRequest
{
    [AliasAs("username")]
    public string Username { get; set; }

    [AliasAs("password")]
    public string Password { get; set; }
}

public sealed class CreateSessionResponse
{
    [AliasAs("clientID")]
    public Guid ClientID { get; set; }

    [AliasAs("accessToken")]
    public string AccessToken { get; set; }

    [AliasAs("messagingServerAddress")]
    public string MessagingServerAddress { get; set; }
}