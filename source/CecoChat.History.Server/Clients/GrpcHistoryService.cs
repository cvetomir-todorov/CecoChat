using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.Data.Config.History;
using CecoChat.Data.History;
using CecoChat.Server;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CecoChat.History.Server.Clients
{
    public sealed class GrpcHistoryService : Contracts.Client.History.HistoryBase
    {
        private readonly ILogger _logger;
        private readonly IHistoryConfiguration _historyConfiguration;
        private readonly IHistoryRepository _historyRepository;
        private readonly IClientBackendMapper _clientBackendMapper;

        public GrpcHistoryService(
            ILogger<GrpcHistoryService> logger,
            IHistoryConfiguration historyConfiguration,
            IHistoryRepository historyRepository,
            IClientBackendMapper clientBackendMapper)
        {
            _logger = logger;
            _historyConfiguration = historyConfiguration;
            _historyRepository = historyRepository;
            _clientBackendMapper = clientBackendMapper;
        }

        [Authorize(Roles = "user")]
        public override async Task<GetUserHistoryResponse> GetUserHistory(GetUserHistoryRequest request, ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserID(out long userID))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                return new GetUserHistoryResponse();
            }
            Activity.Current?.SetTag("user.id", userID);

            IReadOnlyCollection<BackendMessage> backendMessages = await _historyRepository
                .GetUserHistory(userID, request.OlderThan.ToDateTime(), _historyConfiguration.UserMessageCount);

            GetUserHistoryResponse response = new();
            foreach (BackendMessage backendMessage in backendMessages)
            {
                ClientMessage clientMessage = _clientBackendMapper.MapBackendToClientMessage(backendMessage);
                response.Messages.Add(clientMessage);
            }

            _logger.LogTrace("Responding with {0} messages for user {1} history.", response.Messages.Count, userID);
            return response;
        }

        [Authorize(Roles = "user")]
        public override async Task<GetDialogHistoryResponse> GetDialogHistory(GetDialogHistoryRequest request, ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserID(out long userID))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                return new GetDialogHistoryResponse();
            }
            Activity.Current?.SetTag("user.id", userID);

            long otherUserID = request.OtherUserId;

            IReadOnlyCollection<BackendMessage> backendMessages = await _historyRepository
                .GetDialogHistory(userID, otherUserID, request.OlderThan.ToDateTime(), _historyConfiguration.DialogMessageCount);

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
