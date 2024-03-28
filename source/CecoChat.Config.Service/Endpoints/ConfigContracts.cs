using System.Text.Json.Serialization;

namespace CecoChat.Config.Service.Endpoints;

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

    public ConfigElement[] DeletedElements { get; init; } = Array.Empty<ConfigElement>();
}

public sealed class ConfigElement
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public DateTime Version { get; init; }
}
