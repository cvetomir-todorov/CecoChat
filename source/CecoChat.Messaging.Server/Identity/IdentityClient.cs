using System;
using System.Threading.Tasks;
using CecoChat.Contracts.Identity;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Identity
{
    public interface IIdentityClient
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
        private readonly Contracts.Identity.Identity.IdentityClient _client;

        public IdentityClient(
            ILogger<IdentityClient> logger,
            IOptions<IdentityClientOptions> options,
            Contracts.Identity.Identity.IdentityClient client)
        {
            _logger = logger;
            _options = options.Value;
            _client = client;
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
