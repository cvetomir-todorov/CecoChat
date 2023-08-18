using CecoChat.Contracts.History;
using CecoChat.Data;
using CecoChat.Data.Config.History;
using CecoChat.Data.History.Repos;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.History.Endpoints;

public sealed class HistoryService : Contracts.History.History.HistoryBase
{
    private readonly ILogger _logger;
    private readonly IHistoryConfig _historyConfig;
    private readonly IChatMessageRepo _messageRepo;

    public HistoryService(
        ILogger<HistoryService> logger,
        IHistoryConfig historyConfig,
        IChatMessageRepo messageRepo)
    {
        _logger = logger;
        _historyConfig = historyConfig;
        _messageRepo = messageRepo;
    }

    [Authorize(Policy = "user")]
    public override async Task<GetHistoryResponse> GetHistory(GetHistoryRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        string chatId = DataUtility.CreateChatId(userClaims.UserId, request.OtherUserId);
        DateTime olderThan = request.OlderThan.ToDateTime();
        IReadOnlyCollection<HistoryMessage> historyMessages = await _messageRepo.GetHistory(
            userClaims.UserId, chatId, olderThan, _historyConfig.MessageCount);

        GetHistoryResponse response = new();
        response.Messages.Add(historyMessages);

        _logger.LogTrace("Responding with {MessageCount} messages for chat {ChatId} which are older than {OlderThan}", response.Messages.Count, chatId, olderThan);
        return response;
    }
}
