namespace Common.AspNet.Health;

public static class HealthTags
{
    public static readonly string Health = "health";
    public static readonly string Startup = "startup";
    public static readonly string Live = "live";
    public static readonly string Ready = "ready";
}

public static class HealthPaths
{
    public static readonly string Health = "/healthz";
    public static readonly string Startup = "/startupz";
    public static readonly string Live = "/livez";
    public static readonly string Ready = "/readyz";
}
