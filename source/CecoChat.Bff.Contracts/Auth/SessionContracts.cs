using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Bff.Contracts.Auth;

public sealed class ProfileFull
{
    [JsonPropertyName("userId")]
    [AliasAs("userId")]
    public long UserId { get; init; }

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; set; }

    [JsonPropertyName("userName")]
    [AliasAs("userName")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    [AliasAs("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("avatarUrl")]
    [AliasAs("avatarUrl")]
    public string AvatarUrl { get; init; } = string.Empty;

    [JsonPropertyName("phone")]
    [AliasAs("phone")]
    public string Phone { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    [AliasAs("email")]
    public string Email { get; init; } = string.Empty;
}

public sealed class CreateSessionRequest
{
    [JsonPropertyName("userName")]
    [AliasAs("userName")]
    public string UserName { get; init; } = string.Empty;

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
