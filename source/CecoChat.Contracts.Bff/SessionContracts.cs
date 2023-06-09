using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class CreateSessionRequest
{
    [JsonPropertyName("username")]
    [AliasAs("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    [AliasAs("password")]
    public string Password { get; init; } = string.Empty;
}

public sealed class CreateSessionResponse
{
    [JsonPropertyName("clientId")]
    [AliasAs("clientId")]
    public Guid ClientId { get; init; }

    [JsonPropertyName("accessToken")]
    [AliasAs("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("profile")]
    [AliasAs("profile")]
    public ProfileFull Profile { get; init; } = null!;

    [JsonPropertyName("messagingServerAddress")]
    [AliasAs("messagingServerAddress")]
    public string MessagingServerAddress { get; init; } = string.Empty;
}
