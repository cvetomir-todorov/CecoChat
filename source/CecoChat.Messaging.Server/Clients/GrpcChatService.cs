using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
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

        public override async Task Listen(ListenRequest request, IServerStreamWriter<ListenResponse> responseStream, ServerCallContext context)
        {
            IStreamer<ListenResponse> streamer = new GrpcStreamer<ListenResponse>(_logger, responseStream);
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

        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            Message message = request.Message;
            IReadOnlyCollection<IStreamer<ListenResponse>> streamerList = _clientContainer.GetClients(message.ReceiverId);

            if (streamerList.Count > 0)
            {
                ListenResponse response = new ListenResponse
                {
                    Message = message
                };
                foreach (IStreamer<ListenResponse> streamer in streamerList)
                {
                    streamer.AddMessage(response);
                }
            }

            return Task.FromResult(new SendMessageResponse());
        }
    }
}
