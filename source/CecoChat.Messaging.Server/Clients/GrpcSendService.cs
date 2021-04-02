using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts;
using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.Messaging.Server.Backend;
using CecoChat.Server;
using CecoChat.Server.Identity;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CecoChat.Messaging.Server.Clients
{
    public sealed class GrpcSendService : Send.SendBase
    {
        private readonly ILogger _logger;
        private readonly IClock _clock;
        private readonly IMessagesToBackendProducer _messagesToBackendProducer;
        private readonly IClientBackendMapper _mapper;

        public GrpcSendService(
            ILogger<GrpcSendService> logger,
            IClock clock,
            IMessagesToBackendProducer messagesToBackendProducer,
            IClientBackendMapper mapper)
        {
            _logger = logger;
            _clock = clock;
            _messagesToBackendProducer = messagesToBackendProducer;
            _mapper = mapper;
        }

        [Authorize(Roles = "user")]
        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserClaims(out UserClaims userClaims))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                return Task.FromResult(new SendMessageResponse());
            }
            Activity.Current?.SetTag("user.id", userClaims.UserID);

            ClientMessage clientMessage = request.Message;
            clientMessage.Timestamp = Timestamp.FromDateTime(_clock.GetNowUtc());
            _logger.LogTrace("Message for {0} processed {1}.", userClaims, clientMessage);

            BackendMessage backendMessage = _mapper.MapClientToBackendMessage(clientMessage);
            backendMessage.TargetId = backendMessage.ReceiverId;
            backendMessage.ClientId = userClaims.ClientID.ToUuid();
            _messagesToBackendProducer.ProduceMessage(backendMessage);

            SendMessageResponse response = new()
            {
                MessageTimestamp = clientMessage.Timestamp
            };
            return Task.FromResult(response);
        }
    }
}
