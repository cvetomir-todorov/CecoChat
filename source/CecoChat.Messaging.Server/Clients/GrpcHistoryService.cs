using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.Data.Messaging;
using CecoChat.Messaging.Server.Shared;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

            GetUserHistoryResponse response = new();
            foreach (BackendMessage backendMessage in backendMessages)
            {
                ClientMessage clientMessage = _clientBackendMapper.MapBackendToClientMessage(backendMessage);
                response.Messages.Add(clientMessage);
            }

            _logger.LogTrace("Responding with {0} messages for user {1} history.", response.Messages.Count, userID);
            return response;
        }

        public override async Task<GetDialogHistoryResponse> GetDialogHistory(GetDialogHistoryRequest request, ServerCallContext context)
        {
            // TODO: use user ID from auth token
            long userID = request.UserId;
            long otherUserID = request.OtherUserId;

            IReadOnlyCollection<BackendMessage> backendMessages = await _historyRepository
                .GetDialogHistory(userID, otherUserID, request.OlderThan.ToDateTime(), _clientOptions.MessageHistoryCountLimit);

            GetDialogHistoryResponse response = new();
            foreach (BackendMessage backendMessage in backendMessages)
            {
                ClientMessage clientMessage = _clientBackendMapper.MapBackendToClientMessage(backendMessage);
                response.Messages.Add(clientMessage);
            }

            _logger.LogTrace("Responding with {0} messages for dialog between [{1} <-> {2}].", response.Messages.Count, userID, otherUserID);
            return response;
        }
    }
}
