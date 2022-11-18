using System.Diagnostics;
using CecoChat.Contracts.History;
using CecoChat.Data;
using CecoChat.Data.Config.History;
using CecoChat.Data.History.Repos;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.History.Clients;

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
        if (!context.GetHttpContext().User.TryGetUserId(out long userId))
        {
            _logger.LogError("Client from {ClientAddress} was authorized but has no parseable access token", context.Peer);
            return new GetHistoryResponse();
        }
        Activity.Current?.SetTag("user.id", userId);

        string chatId = DataUtility.CreateChatID(userId, request.OtherUserId);
        DateTime olderThan = request.OlderThan.ToDateTime();
        IReadOnlyCollection<HistoryMessage> historyMessages = await _messageRepo
            .GetHistory(userId, chatId, olderThan, _historyConfig.ChatMessageCount);

        GetHistoryResponse response = new();
        response.Messages.Add(historyMessages);

        _logger.LogTrace("Responding with {MessageCount} messages for chat {ChatId} which are older than {OlderThan}", response.Messages.Count, chatId, olderThan);
        return response;
    }
}
