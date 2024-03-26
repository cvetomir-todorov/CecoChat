using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace Common.AspNet.Health;

public static class HttpHealthEndpointsMapping
{
    public static void MapHttpHealthEndpoints(this IEndpointRouteBuilder builder, Action<HttpHealthEndpoints>? setup = null)
    {
        HttpHealthEndpoints options = new();
        setup?.Invoke(options);

        builder.MapHttpHealthEndpoint(options.Health);
        builder.MapHttpHealthEndpoint(options.Startup);
        builder.MapHttpHealthEndpoint(options.Live);
        builder.MapHttpHealthEndpoint(options.Ready);
    }

    private static void MapHttpHealthEndpoint(this IEndpointRouteBuilder builder, HttpHealthEndpoint endpoint)
    {
        if (!endpoint.Enabled)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(endpoint.Pattern))
        {
            throw new InvalidOperationException($"HTTP health endpoint {endpoint.Pattern} should be set.");
        }

        HealthCheckOptions healthCheckOptions = new()
        {
            Predicate = endpoint.Predicate,
            ResultStatusCodes = endpoint.ResultStatusCodes
        };
        if (endpoint.ResponseWriter != null)
        {
            healthCheckOptions.ResponseWriter = endpoint.ResponseWriter;
        }

        builder.MapHealthChecks(endpoint.Pattern, healthCheckOptions);
    }
}
