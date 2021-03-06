﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace CecoChat.Messaging.Server.Identity
{
    public static class IdentityRegistrations
    {
        public static void AddIdentityClient(this IServiceCollection services, IIdentityOptions options)
        {
            services
                .AddGrpcClient<Contracts.Identity.Identity.IdentityClient>(grpc =>
                {
                    grpc.Address = options.Communication.Address;
                })
                .ConfigurePrimaryHttpMessageHandler(() => CreateMessageHandler(options))
                .AddPolicyHandler(_ => HandleFailure(options));
        }

        private static HttpMessageHandler CreateMessageHandler(IIdentityOptions options)
        {
            return new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = options.Communication.KeepAlivePingDelay,
                KeepAlivePingTimeout = options.Communication.KeepAlivePingTimeout,
                EnableMultipleHttp2Connections = true
            };
        }

        private static IAsyncPolicy<HttpResponseMessage> HandleFailure(IIdentityOptions options)
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
                    options.Retry.RetryCount,
                    retryAttempt => SleepDurationProvider(retryAttempt, jitterGenerator, options),
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
            if (headers.TryGetValues(grpcStatusHeader, out IEnumerable<string> values))
            {
                return (StatusCode)int.Parse(values.First());
            }

            return null;
        }

        private static TimeSpan SleepDurationProvider(int retryAttempt, Random jitterGenerator, IIdentityOptions options)
        {
            TimeSpan sleepDuration;
            if (retryAttempt == 1)
            {
                sleepDuration = options.Retry.InitialBackOff;
            }
            else
            {
                // exponential delay
                sleepDuration = TimeSpan.FromSeconds(Math.Pow(options.Retry.BackOffMultiplier, retryAttempt));
                if (sleepDuration > options.Retry.MaxBackOff)
                {
                    sleepDuration = options.Retry.MaxBackOff;
                }
            }

            TimeSpan jitter = TimeSpan.FromMilliseconds(jitterGenerator.Next(0, options.Retry.MaxJitterMs));
            sleepDuration += jitter;

            return sleepDuration;
        }
    }
}
