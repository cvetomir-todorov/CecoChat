using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class ProfileFull
{
    [AliasAs("user_name")]
    public string UserName { get; init; } = string.Empty;

    [AliasAs("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    [AliasAs("avatar_url")]
    public string AvatarUrl { get; init; } = string.Empty;

    [AliasAs("phone")]
    public string Phone { get; init; } = string.Empty;

    [AliasAs("email")]
    public string Email { get; init; } = string.Empty;
}

public sealed class ProfilePublic
{
    [AliasAs("user_id")]
    public long UserId { get; init; }

    [AliasAs("user_name")]
    public string UserName { get; init; } = string.Empty;

    [AliasAs("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    [AliasAs("avatar_url")]
    public string AvatarUrl { get; init; } = string.Empty;
}

public sealed class GetPublicProfileResponse
{
    [AliasAs("profile")]
    public ProfilePublic Profile { get; init; } = null!;
}

public sealed class GetPublicProfilesResponse
{
    [AliasAs("profiles")]
    public ProfilePublic[] Profiles { get; init; } = Array.Empty<ProfilePublic>();
}
