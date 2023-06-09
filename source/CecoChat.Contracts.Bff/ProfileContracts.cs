using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class ProfileFull
{
    [JsonPropertyName("userName")]
    [AliasAs("userName")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    [AliasAs("displayName")]
    public string DisplayName { get; init; } = string.Empty;

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

public sealed class ProfilePublic
{
    [JsonPropertyName("userId")]
    [AliasAs("userId")]
    public long UserId { get; init; }

    [JsonPropertyName("userName")]
    [AliasAs("userName")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    [AliasAs("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("avatarUrl")]
    [AliasAs("avatarUrl")]
    public string AvatarUrl { get; init; } = string.Empty;
}

public sealed class GetPublicProfileResponse
{
    [JsonPropertyName("profile")]
    [AliasAs("profile")]
    public ProfilePublic Profile { get; init; } = null!;
}

public sealed class GetPublicProfilesResponse
{
    [JsonPropertyName("profiles")]
    [AliasAs("profiles")]
    public ProfilePublic[] Profiles { get; init; } = Array.Empty<ProfilePublic>();
}
