using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using CecoChat.Messaging.Server.Backend.Production;
using CecoChat.Messaging.Server.Shared;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace CecoChat.Messaging.Server.Clients
{
    public sealed class GrpcSendService : Send.SendBase
    {
        private readonly ILogger _logger;
        private readonly IClock _clock;
        private readonly IBackendProducer _backendProducer;
        private readonly IClientBackendMapper _mapper;

        public GrpcSendService(
            ILogger<GrpcListenService> logger,
            IClock clock,
            IBackendProducer backendProducer,
            IClientBackendMapper mapper)
        {
            _logger = logger;
            _clock = clock;
            _backendProducer = backendProducer;
            _mapper = mapper;
        }

        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            Message clientMessage = request.Message;
            clientMessage.Timestamp = Timestamp.FromDateTime(_clock.GetNowUtc());
            _logger.LogTrace("Timestamped client message {0}.", clientMessage);

            Contracts.Backend.Message backendMessage = _mapper.MapClientToBackendMessage(clientMessage);
            _backendProducer.ProduceMessage(backendMessage);

            // TODO: send to the same user connected with multiple clients

            return Task.FromResult(new SendMessageResponse{MessageTimestamp = clientMessage.Timestamp});
        }
    }
}
