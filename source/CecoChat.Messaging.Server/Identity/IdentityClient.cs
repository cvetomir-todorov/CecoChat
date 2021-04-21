using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Identity;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Identity
{
    public interface IIdentityClient : IDisposable
    {
        Task<GenerateIdentityResult> GenerateIdentity(long userID);
    }

    public struct GenerateIdentityResult
    {
        public bool Success { get; set; }
        public long ID { get; set; }
    }

    public sealed class IdentityClient : IIdentityClient
    {
        private readonly ILogger _logger;
        private readonly IIdentityClientOptions _options;
        private readonly GrpcChannel _channel;
        private readonly Contracts.Identity.Identity.IdentityClient _client;

        public IdentityClient(
            ILogger<IdentityClient> logger,
            IOptions<IdentityClientOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _channel = CreateChannel(_options);
            _client = new(_channel);
        }

        private static GrpcChannel CreateChannel(IIdentityClientOptions options)
        {
            MethodConfig defaultMethodConfig = new()
            {
                Names = {MethodName.Default},
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = options.RetryTimes,
                    InitialBackoff = options.InitialBackOff,
                    MaxBackoff = options.MaxBackOff,
                    BackoffMultiplier = options.BackOffMultiplier,
                    RetryableStatusCodes = {StatusCode.Unavailable, StatusCode.DeadlineExceeded}
                }
            };
            ServiceConfig serviceConfig = new()
            {
                MethodConfigs = {defaultMethodConfig}
            };
            SocketsHttpHandler httpHandler = new()
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            };

            return GrpcChannel.ForAddress(options.Address, new GrpcChannelOptions
            {
                ServiceConfig = serviceConfig,
                HttpHandler = httpHandler
            });
        }

        public void Dispose()
        {
            _channel.ShutdownAsync().Wait();
            _channel.Dispose();
        }

        public async Task<GenerateIdentityResult> GenerateIdentity(long userID)
        {
            GenerateIdentityRequest request = new() {OriginatorId = userID};
            DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);

            try
            {
                GenerateIdentityResponse response = await _client.GenerateIdentityAsync(request, deadline: deadline);
                return new GenerateIdentityResult
                {
                    Success = true,
                    ID = response.Id
                };
            }
            catch (RpcException rpcException)
            {
                _logger.LogError(rpcException, "Failed to generate ID for user {0} due to error {1}.", userID, rpcException.Status);
                return new GenerateIdentityResult();
            }
        }
    }
}
