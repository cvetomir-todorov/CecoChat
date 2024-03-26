using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Common.Http.Health;

public static class UriHealthCheckRegistrations
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public static IHealthChecksBuilder AddUri(
        this IHealthChecksBuilder builder,
        string name,
        Uri uri,
        Action<IServiceProvider, HttpClient>? configureHttpClient = null,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"Argument {nameof(name)} should not be null or whitespace.", nameof(name));
        }
        if (timeout == null)
        {
            timeout = DefaultTimeout;
        }

        RegisterConfigureHttpClient(builder, name, configureHttpClient);

        return builder.Add(new HealthCheckRegistration(
            name,
            serviceProvider => CreateHealthCheck(serviceProvider, name, uri, timeout.Value),
            failureStatus,
            tags,
            timeout));
    }

    private static void RegisterConfigureHttpClient(IHealthChecksBuilder builder, string registrationName, Action<IServiceProvider, HttpClient>? configureHttpClient)
    {
        builder.Services
            .AddHttpClient(registrationName)
            .ConfigureHttpClient(configureHttpClient ?? ((_, _) => { }));
    }

    private static IHealthCheck CreateHealthCheck(IServiceProvider serviceProvider, string registrationName, Uri uri, TimeSpan timeout)
    {
        return new UriHealthCheck(uri, timeout, () =>
        {
            IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            return httpClientFactory.CreateClient(registrationName);
        });
    }
}
