using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class ProfileFull
{
    [AliasAs("user_name")]
    public string UserName { get; set; } = string.Empty;

    [AliasAs("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [AliasAs("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;

    [AliasAs("phone")]
    public string Phone { get; set; } = string.Empty;

    [AliasAs("email")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ProfilePublic
{
    [AliasAs("user_id")]
    public long UserId { get; set; }

    [AliasAs("user_name")]
    public string UserName { get; set; } = string.Empty;

    [AliasAs("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [AliasAs("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;
}

public sealed class GetPublicProfileResponse
{
    [AliasAs("profile")]
    public ProfilePublic Profile { get; set; } = null!;
}

public sealed class GetPublicProfilesResponse
{
    [AliasAs("profiles")]
    public ProfilePublic[] Profiles { get; set; } = Array.Empty<ProfilePublic>();
}
