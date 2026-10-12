using System.Text.Json.Serialization;

namespace CecoChat.Config.Service.Endpoints;

public sealed class GetConfigRequest
{ }

public sealed class GetConfigResponse
{
    public ConfigElement[] Elements { get; init; } = [];
}

public sealed class UpdateConfigElementsRequest
{
    public ConfigElement[] ExistingElements { get; init; } = [];

    public ConfigElement[] NewElements { get; init; } = [];

    public ConfigElement[] DeletedElements { get; init; } = [];
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
