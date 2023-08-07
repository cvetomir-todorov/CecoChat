using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class ChangePasswordRequest
{
    [JsonPropertyName("newPassword")]
    [AliasAs("newPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public Guid Version { get; init; }
}

public sealed class ChangePasswordResponse
{
    [JsonPropertyName("newVersion")]
    [AliasAs("newVersion")]
    public Guid NewVersion { get; init; }
}

public sealed class EditProfileRequest
{
    [JsonPropertyName("displayName")]
    [AliasAs("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public Guid Version { get; init; }
}

public sealed class EditProfileResponse
{
    [JsonPropertyName("newVersion")]
    [AliasAs("newVersion")]
    public Guid NewVersion { get; init; }
}
