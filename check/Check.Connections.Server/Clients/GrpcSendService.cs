using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using Grpc.Core;

namespace Check.Connections.Server.Clients
{
    public sealed class GrpcSendService : Send.SendBase
    {
        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            SendMessageResponse response = new();
            return Task.FromResult(response);
        }
    }
}
