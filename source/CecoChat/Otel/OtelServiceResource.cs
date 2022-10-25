namespace CecoChat.Otel;

public sealed class OtelServiceResource
{
    public string Namespace { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;
}