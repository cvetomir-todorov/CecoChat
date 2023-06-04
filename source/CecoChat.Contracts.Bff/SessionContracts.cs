using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class CreateSessionRequest
{
    [AliasAs("username")]
    public string Username { get; init; } = string.Empty;

    [AliasAs("password")]
    public string Password { get; init; } = string.Empty;
}

public sealed class CreateSessionResponse
{
    [AliasAs("clientID")]
    public Guid ClientId { get; init; }

    [AliasAs("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [AliasAs("profile")]
    public ProfileFull Profile { get; init; } = null!;

    [AliasAs("messagingServerAddress")]
    public string MessagingServerAddress { get; init; } = string.Empty;
}
