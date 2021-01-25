using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using CecoChat.Data.Messaging;
using CecoChat.Messaging.Server.Shared;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ClientMessage = CecoChat.Contracts.Client.Message;
using BackendMessage = CecoChat.Contracts.Backend.Message;

namespace CecoChat.Messaging.Server.Clients
{
    public sealed class GrpcHistoryService : History.HistoryBase
    {
        private readonly ILogger _logger;
        private readonly IClientOptions _clientOptions;
        private readonly IHistoryRepository _historyRepository;
        private readonly IClientBackendMapper _clientBackendMapper;

        public GrpcHistoryService(
            ILogger<GrpcHistoryService> logger,
            IOptions<ClientOptions> clientOptions,
            IHistoryRepository historyRepository,
            IClientBackendMapper clientBackendMapper)
        {
            _logger = logger;
            _clientOptions = clientOptions.Value;
            _historyRepository = historyRepository;
            _clientBackendMapper = clientBackendMapper;
        }

        public override async Task<GetUserHistoryResponse> GetUserHistory(GetUserHistoryRequest request, ServerCallContext context)
        {
            // TODO: use user ID from auth token
            long userID = request.UserId;
            IReadOnlyCollection<BackendMessage> backendMessages = await _historyRepository
                .GetUserHistory(userID, request.OlderThan.ToDateTime(), _clientOptions.MessageHistoryCountLimit);
            _logger.LogTrace("Responding with {0} message history to user {1}.", backendMessages.Count, userID);

            GetUserHistoryResponse response = new();
            foreach (BackendMessage backendMessage in backendMessages)
            {
                ClientMessage clientMessage = _clientBackendMapper.MapBackendToClientMessage(backendMessage);
                response.Messages.Add(clientMessage);
            }

            return response;
        }
    }
}
