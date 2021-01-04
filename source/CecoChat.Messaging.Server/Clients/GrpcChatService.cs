using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.GrpcContracts;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace CecoChat.Messaging.Server.Clients
{
    public sealed class GrpcChatService : Chat.ChatBase
    {
        private readonly ILogger _logger;
        private readonly IClientContainer _clientContainer;

        public GrpcChatService(ILogger<GrpcChatService> logger, IClientContainer clientContainer)
        {
            _logger = logger;
            _clientContainer = clientContainer;
        }

        public override async Task Listen(GrpcListenRequest request, IServerStreamWriter<GrpcMessage> responseStream, ServerCallContext context)
        {
            IStreamingContext<GrpcMessage> messageStream = new GrpcStreamingContext<GrpcMessage>(_logger, responseStream);
            try
            {
                _clientContainer.AddClient(request.UserId, messageStream);
                await messageStream.ProcessMessages(CancellationToken.None);
            }
            finally
            {
                _clientContainer.RemoveClient(request.UserId, messageStream);
                messageStream.Dispose();
            }
        }

        public override Task<GrpcSendMessageResponse> SendMessage(GrpcSendMessageRequest request, ServerCallContext context)
        {
            GrpcMessage message = request.Message;
            IReadOnlyCollection<IStreamingContext<GrpcMessage>> messageStreamList = _clientContainer.GetClients(message.ReceiverId);

            foreach (IStreamingContext<GrpcMessage> messageStream in messageStreamList)
            {
                messageStream.AddMessage(message);
            }

            return Task.FromResult(new GrpcSendMessageResponse());
        }

        public override Task<GrpcAckMessageResponse> AckMessage(GrpcAckMessageRequest request, ServerCallContext context)
        {
            // TODO: add listening for ack
            return Task.FromResult(new GrpcAckMessageResponse());
        }
    }
}
