using System.Net;
using System.Net.Http.Headers;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace CecoChat.Polly;

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
                StatusCode? grpcStatus = GetGrpcStatusCode(response);

                bool isHttpError = grpcStatus == null && !response.IsSuccessStatusCode;
                bool isGrpcError = response.IsSuccessStatusCode && grpcStatus == StatusCode.OK;

                return !isHttpError && !isGrpcError;
            })
            .WaitAndRetryAsync(
                retryOptions.RetryCount,
                retryAttempt => SleepDurationProvider(retryAttempt, jitterGenerator, retryOptions),
                onRetry: (_, _, _, _) =>
                {
                    // if needed we can obtain new tokens and do other per-call stuff here
                });
    }

    private static StatusCode? GetGrpcStatusCode(HttpResponseMessage response)
    {
        HttpResponseHeaders headers = response.Headers;
        const string grpcStatusHeader = "grpc-status";

        if (!headers.Contains(grpcStatusHeader) && response.StatusCode == HttpStatusCode.OK)
        {
            return StatusCode.OK;
        }
        if (headers.TryGetValues(grpcStatusHeader, out IEnumerable<string>? values))
        {
            return (StatusCode)int.Parse(values.First());
        }

        return null;
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