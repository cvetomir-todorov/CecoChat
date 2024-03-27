using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Bff.Contracts.Auth;

public sealed class RegisterRequest
{
    [JsonPropertyName("userName")]
    [AliasAs("userName")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    [AliasAs("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    [AliasAs("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("phone")]
    [AliasAs("phone")]
    public string Phone { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    [AliasAs("email")]
    public string Email { get; init; } = string.Empty;
}
