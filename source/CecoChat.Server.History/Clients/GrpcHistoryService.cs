using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts.History;
using CecoChat.Data.Config.History;
using CecoChat.Data.History.Repos;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CecoChat.Server.History.Clients
{
    public sealed class GrpcHistoryService : Contracts.History.History.HistoryBase
    {
        private readonly ILogger _logger;
        private readonly IHistoryConfig _historyConfig;
        private readonly IChatMessageRepo _messageRepo;

        public GrpcHistoryService(
            ILogger<GrpcHistoryService> logger,
            IHistoryConfig historyConfig,
            IChatMessageRepo messageRepo)
        {
            _logger = logger;
            _historyConfig = historyConfig;
            _messageRepo = messageRepo;
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

            IReadOnlyCollection<HistoryMessage> historyMessages = await _messageRepo
                .GetHistory(userID, request.OtherUserId, request.OlderThan.ToDateTime(), _historyConfig.DialogMessageCount);

            GetHistoryResponse response = new();
            response.Messages.Add(historyMessages);

            _logger.LogTrace("Responding with {0} messages for chat between [{1} <-> {2}].", response.Messages.Count, userID, request.OtherUserId);
            return response;
        }
    }
}
