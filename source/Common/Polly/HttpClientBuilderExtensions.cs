using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Common.Polly;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddGrpcRetryPolicy(this IHttpClientBuilder builder, RetryOptions retryOptions)
    {
        return builder.AddPolicyHandler(_ => HandleFailure(retryOptions));
    }

    private static IAsyncPolicy<HttpResponseMessage> HandleFailure(RetryOptions retryOptions)
    {
        Random jitterGenerator = new();

        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(response =>
            {
                // ignore redirects 3xx and client errors 4xx
                bool isHttpError = (int)response.StatusCode >= 500;
                return isHttpError;
            })
            .WaitAndRetryAsync(
                retryOptions.RetryCount,
                retryAttempt => SleepDurationProvider(retryAttempt, jitterGenerator, retryOptions),
                onRetry: (_, _, _, _) =>
                {
                    // if needed we can obtain new tokens and do other per-call stuff here
                });
    }

    private static TimeSpan SleepDurationProvider(int retryAttempt, Random jitterGenerator, RetryOptions retryOptions)
    {
        TimeSpan sleepDuration;
        if (retryAttempt == 1)
        {
            sleepDuration = retryOptions.InitialBackOff;
        }
        else
        {
            // exponential delay
            sleepDuration = TimeSpan.FromSeconds(Math.Pow(retryOptions.BackOffMultiplier, retryAttempt));
            if (sleepDuration > retryOptions.MaxBackOff)
            {
                sleepDuration = retryOptions.MaxBackOff;
            }
        }

        TimeSpan jitter = TimeSpan.FromMilliseconds(jitterGenerator.Next(0, retryOptions.MaxJitterMs));
        sleepDuration += jitter;

        return sleepDuration;
    }
}
