using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Contracts.Admin;

public sealed class GetConfigRequest
{ }

public sealed class GetConfigResponse
{
    public ConfigElement[] Elements { get; init; } = Array.Empty<ConfigElement>();
}

public sealed class UpdateConfigElementsRequest
{
    public ConfigElement[] ExistingElements { get; init; } = Array.Empty<ConfigElement>();

    public ConfigElement[] NewElements { get; init; } = Array.Empty<ConfigElement>();
}

public sealed class ConfigElement
{
    [JsonPropertyName("name")]
    [AliasAs("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    [AliasAs("value")]
    public string Value { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }
}
