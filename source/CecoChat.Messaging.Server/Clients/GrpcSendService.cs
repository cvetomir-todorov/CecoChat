using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Contracts.Backend;
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
        private readonly IClientContainer _clientContainer;

        public GrpcSendService(
            ILogger<GrpcListenService> logger,
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

        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            ClientMessage clientMessage = request.Message;
            clientMessage.Timestamp = Timestamp.FromDateTime(_clock.GetNowUtc());
            _logger.LogTrace("Timestamped client message {0}.", clientMessage);

            BackendMessage backendMessage = _mapper.MapClientToBackendMessage(clientMessage);
            _backendProducer.ProduceMessage(backendMessage);

            SendToClientsFromSameSender(clientMessage, context);

            return Task.FromResult(new SendMessageResponse{MessageTimestamp = clientMessage.Timestamp});
        }

        private void SendToClientsFromSameSender(ClientMessage clientMessage, ServerCallContext context)
        {
            IEnumerable<IStreamer<ListenResponse>> clients = _clientContainer.GetClients(clientMessage.SenderId);
            // TODO: use client ID from metadata or auth token
            string currentClientID = context.Peer;
            ListenResponse response = new()
            {
                Message = clientMessage
            };

            // do not call clients.Count since it is expensive and uses locks
            int successCount = 0;
            int allCount = 0;

            foreach (IStreamer<ListenResponse> client in clients)
            {
                if (client.ClientID != currentClientID)
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
