namespace CecoChat.Otel
{
    public record OtelServiceResource
    {
        public string Namespace { get; init; }

        public string Name { get; init; }

        public string Version { get; init; }
    }
}