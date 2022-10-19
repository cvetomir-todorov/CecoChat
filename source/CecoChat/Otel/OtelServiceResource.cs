namespace CecoChat.Otel;

public record OtelServiceResource
{
    public string Namespace { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;
}