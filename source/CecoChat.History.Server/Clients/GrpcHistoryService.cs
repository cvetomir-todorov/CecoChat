using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts.History;
using CecoChat.Data.Config.History;
using CecoChat.Data.History;
using CecoChat.Server;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CecoChat.History.Server.Clients
{
    public sealed class GrpcHistoryService : Contracts.History.History.HistoryBase
    {
        private readonly ILogger _logger;
        private readonly IHistoryConfig _historyConfig;
        private readonly IHistoryRepository _historyRepository;
        private readonly IContractDataMapper _mapper;

        public GrpcHistoryService(
            ILogger<GrpcHistoryService> logger,
            IHistoryConfig historyConfig,
            IHistoryRepository historyRepository,
            IContractDataMapper mapper)
        {
            _logger = logger;
            _historyConfig = historyConfig;
            _historyRepository = historyRepository;
            _mapper = mapper;
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

            IReadOnlyCollection<HistoryMessage> historyMessages = await _historyRepository
                .GetUserHistory(userID, request.OlderThan.ToDateTime(), _historyConfig.UserMessageCount);

            GetUserHistoryResponse response = new();
            foreach (HistoryMessage historyMessage in historyMessages)
            {
                HistoryItem historyItem = _mapper.CreateHistoryItem(historyMessage);
                response.Messages.Add(historyItem);
            }

            _logger.LogTrace("Responding with {0} messages for user {1} history.", response.Messages.Count, userID);
            return response;
        }

        [Authorize(Roles = "user")]
        public override async Task<GetChatHistoryResponse> GetChatHistory(GetChatHistoryRequest request, ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserID(out long userID))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                return new GetChatHistoryResponse();
            }
            Activity.Current?.SetTag("user.id", userID);

            long otherUserID = request.OtherUserId;

            IReadOnlyCollection<HistoryMessage> historyMessages = await _historyRepository
                .GetDialogHistory(userID, otherUserID, request.OlderThan.ToDateTime(), _historyConfig.DialogMessageCount);

            GetChatHistoryResponse response = new();
            foreach (HistoryMessage historyMessage in historyMessages)
            {
                HistoryItem historyItem = _mapper.CreateHistoryItem(historyMessage);
                response.Messages.Add(historyItem);
            }

            _logger.LogTrace("Responding with {0} messages for chat between [{1} <-> {2}].", response.Messages.Count, userID, otherUserID);
            return response;
        }
    }
}
