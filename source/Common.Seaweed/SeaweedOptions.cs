namespace Common.Seaweed;

public sealed class SeaweedOptions
{
    public Uri Endpoint { get; init; } = null!;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
}
