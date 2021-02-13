using System;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Check.Connections.Server.Clients
{
    public sealed class GrpcSendService : Send.SendBase
    {
        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            SendMessageResponse response = new()
            {
                MessageTimestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
            return Task.FromResult(response);
        }
    }
}
