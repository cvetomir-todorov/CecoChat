using System;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using CecoChat.DependencyInjection;
using CecoChat.Messaging.Server.Backend.Production;
using CecoChat.Messaging.Server.Shared;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace CecoChat.Messaging.Server.Clients
{
    public sealed class GrpcChatService : Chat.ChatBase
    {
        private readonly ILogger _logger;
        private readonly IClock _clock;
        private readonly IClientContainer _clientContainer;
        private readonly IBackendProducer _backendProducer;
        private readonly IClientBackendMapper _mapper;
        private readonly IFactory<IGrpcStreamer<ListenResponse>> _streamerFactory;

        public GrpcChatService(
            ILogger<GrpcChatService> logger,
            IClock clock,
            IClientContainer clientContainer,
            IBackendProducer backendProducer,
            IClientBackendMapper mapper,
            IFactory<IGrpcStreamer<ListenResponse>> streamerFactory)
        {
            _logger = logger;
            _clock = clock;
            _clientContainer = clientContainer;
            _backendProducer = backendProducer;
            _mapper = mapper;
            _streamerFactory = streamerFactory;
        }

        public override async Task Listen(ListenRequest request, IServerStreamWriter<ListenResponse> responseStream, ServerCallContext context)
        {
            string clientID = context.Peer;
            _logger.LogTrace("Client {0} connected.", clientID);

            IGrpcStreamer<ListenResponse> streamer = _streamerFactory.Create();
            streamer.Initialize(responseStream, context);

            try
            {
                // TODO: use user ID from auth token
                _clientContainer.AddClient(request.UserId, streamer);
                await streamer.ProcessMessages(context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                _clientContainer.RemoveClient(request.UserId, streamer);
                _logger.LogTrace("Client {0} disconnected.", clientID);
            }
            finally
            {
                streamer.Dispose();
            }
        }

        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            Message clientMessage = request.Message;
            clientMessage.Timestamp = Timestamp.FromDateTime(_clock.GetNowUtc());
            _logger.LogTrace("Timestamped client message {0}.", clientMessage);

            Contracts.Backend.Message backendMessage = _mapper.MapClientToBackendMessage(clientMessage);
            _backendProducer.ProduceMessage(backendMessage);

            return Task.FromResult(new SendMessageResponse{MessageTimestamp = clientMessage.Timestamp});
        }
    }
}
