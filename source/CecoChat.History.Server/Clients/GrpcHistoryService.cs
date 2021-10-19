using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts.History;
using CecoChat.Data.Config.History;
using CecoChat.Data.History;
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

        public GrpcHistoryService(
            ILogger<GrpcHistoryService> logger,
            IHistoryConfig historyConfig,
            IHistoryRepository historyRepository)
        {
            _logger = logger;
            _historyConfig = historyConfig;
            _historyRepository = historyRepository;
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
            response.Messages.Add(historyMessages);

            _logger.LogTrace("Responding with {0} messages for user {1} history.", response.Messages.Count, userID);
            return response;
        }

        [Authorize(Roles = "user")]
        public override async Task<GetHistoryResponse> GetHistory(GetHistoryRequest request, ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserID(out long userID))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                return new GetHistoryResponse();
            }
            Activity.Current?.SetTag("user.id", userID);

            long otherUserID = request.OtherUserId;

            IReadOnlyCollection<HistoryMessage> historyMessages = await _historyRepository
                .GetHistory(userID, otherUserID, request.OlderThan.ToDateTime(), _historyConfig.DialogMessageCount);

            GetHistoryResponse response = new();
            response.Messages.Add(historyMessages);

            _logger.LogTrace("Responding with {0} messages for chat between [{1} <-> {2}].", response.Messages.Count, userID, otherUserID);
            return response;
        }
    }
}
