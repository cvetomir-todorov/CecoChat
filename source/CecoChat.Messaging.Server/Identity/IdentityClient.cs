using System;
using System.Threading.Tasks;
using CecoChat.Contracts.Identity;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Identity
{
    public interface IIdentityClient : IDisposable
    {
        Task<long> GenerateIdentity(long userID);
    }

    public sealed class IdentityClient : IIdentityClient
    {
        private readonly GrpcChannel _channel;
        private readonly Contracts.Identity.Identity.IdentityClient _client;

        public IdentityClient(
            IOptions<IdentityOptions> options)
        {
            _channel = GrpcChannel.ForAddress(options.Value.Address);
            _client = new(_channel);
        }

        public void Dispose()
        {
            _channel.ShutdownAsync().Wait();
            _channel.Dispose();
        }

        public async Task<long> GenerateIdentity(long userID)
        {
            GenerateIdentityRequest request = new() {OriginatorId = userID};
            GenerateIdentityResponse response = await _client.GenerateIdentityAsync(request);
            return response.Id;
        }
    }
}
