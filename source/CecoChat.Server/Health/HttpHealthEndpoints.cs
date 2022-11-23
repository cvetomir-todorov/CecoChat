using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CecoChat.Server.Health;

public static class HttpHealthEndpoints
{
    public static IEndpointConventionBuilder MapHttpHealthEndpoint(
        this IEndpointRouteBuilder endpoints,
        string serviceName,
        string pattern = "/health",
        string targetTag = "health",
        Func<HealthCheckRegistration, bool>? predicate = default,
        Dictionary<HealthStatus, int>? resultStatusCodes = default)
    {
        predicate ??= healthCheck => healthCheck.Tags.Contains(targetTag);
        resultStatusCodes ??= new Dictionary<HealthStatus, int>
        {
            { HealthStatus.Healthy, (int)HttpStatusCode.OK },
            { HealthStatus.Degraded, (int)HttpStatusCode.OK },
            { HealthStatus.Unhealthy, (int)HttpStatusCode.ServiceUnavailable }
        };

        return endpoints.MapHealthChecks(pattern, new HealthCheckOptions
        {
            Predicate = predicate,
            ResultStatusCodes = resultStatusCodes,
            ResponseWriter = (context, report) => CustomHealth.Writer(serviceName, context, report)
        });
    }
}
