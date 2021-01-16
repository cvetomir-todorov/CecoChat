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
            IStreamer<GrpcMessage> streamer = new GrpcStreamer<GrpcMessage>(_logger, responseStream);
            try
            {
                _clientContainer.AddClient(request.UserId, streamer);
                await streamer.ProcessMessages(CancellationToken.None);
            }
            finally
            {
                _clientContainer.RemoveClient(request.UserId, streamer);
                streamer.Dispose();
            }
        }

        public override Task<GrpcSendMessageResponse> SendMessage(GrpcSendMessageRequest request, ServerCallContext context)
        {
            GrpcMessage message = request.Message;
            IReadOnlyCollection<IStreamer<GrpcMessage>> streamerList = _clientContainer.GetClients(message.ReceiverId);

            foreach (IStreamer<GrpcMessage> streamer in streamerList)
            {
                streamer.AddMessage(message);
            }

            return Task.FromResult(new GrpcSendMessageResponse());
        }
    }
}
