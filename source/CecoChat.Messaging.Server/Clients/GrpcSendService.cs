using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly IBackendProducer _backendProducer;
        private readonly IClientBackendMapper _mapper;
        private readonly IClientContainer _clientContainer;

        public GrpcSendService(
            ILogger<GrpcSendService> logger,
            IClock clock,
            IBackendProducer backendProducer,
            IClientBackendMapper mapper,
            IClientContainer clientContainer)
        {
            _logger = logger;
            _clock = clock;
            _backendProducer = backendProducer;
            _mapper = mapper;
            _clientContainer = clientContainer;
        }

        [Authorize(Roles = "user")]
        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserClaims(out UserClaims userClaims))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                return Task.FromResult(new SendMessageResponse());
            }

            ClientMessage clientMessage = request.Message;
            clientMessage.Timestamp = Timestamp.FromDateTime(_clock.GetNowUtc());
            _logger.LogTrace("Message for {0} processed {1}.", userClaims, clientMessage);

            BackendMessage backendMessage = _mapper.MapClientToBackendMessage(clientMessage);
            _backendProducer.ProduceMessage(backendMessage);

            SendToClientsFromSameSender(clientMessage, userClaims);

            SendMessageResponse response = new()
            {
                MessageTimestamp = clientMessage.Timestamp
            };
            return Task.FromResult(response);
        }

        private void SendToClientsFromSameSender(ClientMessage clientMessage, UserClaims currentUserClaims)
        {
            IEnumerable<IStreamer<ListenResponse>> clients = _clientContainer.EnumerateClients(clientMessage.SenderId);
            ListenResponse response = new()
            {
                Message = clientMessage
            };

            // do not call clients.Count since it is expensive and uses locks
            int successCount = 0;
            int allCount = 0;

            foreach (IStreamer<ListenResponse> client in clients)
            {
                if (client.ClientID != currentUserClaims.ClientID)
                {
                    if (client.AddMessage(response))
                    {
                        successCount++;
                    }

                    allCount++;
                }
            }

            if (successCount < allCount)
            {
                _logger.LogWarning("Connected other senders ({0} out of {1}) were sent message {2}.", successCount, allCount, clientMessage);
            }
            else
            {
                _logger.LogTrace("Connected other senders (all {0}) were sent message {1}.", successCount, clientMessage);
            }
        }
    }
}
