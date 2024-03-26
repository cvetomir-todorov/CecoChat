namespace Common.AspNet.Health;

public sealed class HttpHealthEndpoints
{
    public HttpHealthEndpoint Health { get; } = new()
    {
        Pattern = HealthPaths.Health,
        Predicate = healthCheck => healthCheck.Tags.Contains(HealthTags.Health)
    };

    public HttpHealthEndpoint Startup { get; } = new()
    {
        Pattern = HealthPaths.Startup,
        Predicate = healthCheck => healthCheck.Tags.Contains(HealthTags.Startup)
    };

    public HttpHealthEndpoint Live { get; } = new()
    {
        Pattern = HealthPaths.Live,
        Predicate = healthCheck => healthCheck.Tags.Contains(HealthTags.Live)
    };

    public HttpHealthEndpoint Ready { get; } = new()
    {
        Pattern = HealthPaths.Ready,
        Predicate = healthCheck => healthCheck.Tags.Contains(HealthTags.Ready)
    };
}
