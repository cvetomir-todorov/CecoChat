using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Bff.Contracts.Profiles;

public sealed class ChangePasswordRequest
{
    [JsonPropertyName("newPassword")]
    [AliasAs("newPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }
}

public sealed class ChangePasswordResponse
{
    [JsonPropertyName("newVersion")]
    [AliasAs("newVersion")]
    public DateTime NewVersion { get; init; }
}

public sealed class EditProfileRequest
{
    [JsonPropertyName("displayName")]
    [AliasAs("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }
}

public sealed class EditProfileResponse
{
    [JsonPropertyName("newVersion")]
    [AliasAs("newVersion")]
    public DateTime NewVersion { get; init; }
}
