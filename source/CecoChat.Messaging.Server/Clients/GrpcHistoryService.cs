using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using CecoChat.Data.Messaging;
using CecoChat.Messaging.Server.Shared;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ClientMessage = CecoChat.Contracts.Client.Message;
using BackendMessage = CecoChat.Contracts.Backend.Message;

namespace CecoChat.Messaging.Server.Clients
{
    public sealed class GrpcHistoryService : History.HistoryBase
    {
        private readonly ILogger _logger;
        private readonly IMessagingRepository _messagingRepository;
        private readonly IClientBackendMapper _clientBackendMapper;

        public GrpcHistoryService(
            ILogger<GrpcHistoryService> logger,
            IMessagingRepository messagingRepository,
            IClientBackendMapper clientBackendMapper)
        {
            _logger = logger;
            _messagingRepository = messagingRepository;
            _clientBackendMapper = clientBackendMapper;
        }

        public override async Task<GetHistoryResponse> GetHistory(GetHistoryRequest request, ServerCallContext context)
        {
            // TODO: use user ID from auth token
            long userID = request.UserId;
            IReadOnlyCollection<BackendMessage> backendMessages = await _messagingRepository
                .SelectNewerMessagesForReceiver(userID, request.NewerThan.ToDateTime());
            _logger.LogTrace("Responding with {0} message history to user {1}.", backendMessages.Count, userID);

            GetHistoryResponse response = new GetHistoryResponse();
            foreach (BackendMessage backendMessage in backendMessages)
            {
                ClientMessage clientMessage = _clientBackendMapper.MapBackendToClientMessage(backendMessage);
                response.Messages.Add(clientMessage);
            }

            return response;
        }
    }
}
