using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Common.Http.Health;

public class UriHealthCheck : IHealthCheck
{
    private readonly Uri _uri;
    private readonly TimeSpan _timeout;
    private readonly Func<HttpClient> _httpClientFactory;

    public UriHealthCheck(Uri uri, TimeSpan timeout, Func<HttpClient> httpClientFactory)
    {
        _uri = uri;
        _timeout = timeout;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, description: $"{nameof(UriHealthCheck)} execution for {_uri} is cancelled.");
            }

            HttpClient httpClient = _httpClientFactory();
            using HttpRequestMessage requestMessage = new(HttpMethod.Get, _uri);
            requestMessage.Version = httpClient.DefaultRequestVersion;
            requestMessage.VersionPolicy = httpClient.DefaultVersionPolicy;

            using (CancellationTokenSource timeoutSource = new(_timeout))
            using (CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken))
            {
                using HttpResponseMessage response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, linkedSource.Token);
                if (!response.IsSuccessStatusCode)
                {
                    return new HealthCheckResult(context.Registration.FailureStatus, description: $"{nameof(UriHealthCheck)} for {_uri} failed with status code {response.StatusCode}.");
                }
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
