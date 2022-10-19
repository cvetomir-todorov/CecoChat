using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class CreateSessionRequest
{
    [AliasAs("username")]
    public string Username { get; set; } = string.Empty;

    [AliasAs("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class CreateSessionResponse
{
    [AliasAs("clientID")]
    public Guid ClientID { get; set; }

    [AliasAs("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [AliasAs("messagingServerAddress")]
    public string MessagingServerAddress { get; set; } = string.Empty;
}