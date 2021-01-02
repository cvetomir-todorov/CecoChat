using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts;
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

        public override async Task Listen(ListenRequest request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            IStreamingContext<Message> messageStream = new GrpcStreamingContext<Message>(_logger, responseStream);
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

        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            Message message = request.Message;
            IReadOnlyCollection<IStreamingContext<Message>> messageStreamList = _clientContainer.GetClients(message.ReceiverId);

            foreach (IStreamingContext<Message> messageStream in messageStreamList)
            {
                messageStream.AddMessage(message);
            }

            return Task.FromResult(new SendMessageResponse());
        }

        public override Task<AckMessageResponse> AckMessage(AckMessageRequest request, ServerCallContext context)
        {
            // TODO: add listening for ack
            return Task.FromResult(new AckMessageResponse());
        }
    }
}
