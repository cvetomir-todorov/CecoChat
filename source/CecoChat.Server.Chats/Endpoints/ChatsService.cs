using CecoChat.Chats.Contracts;
using CecoChat.Chats.Data.Entities.ChatMessages;
using CecoChat.Chats.Data.Entities.UserChats;
using CecoChat.Data;
using CecoChat.DynamicConfig.Sections.History;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.Chats.Endpoints;

public sealed class ChatsService : CecoChat.Chats.Contracts.Chats.ChatsBase
{
    private readonly ILogger _logger;
    private readonly IHistoryConfig _historyConfig;
    private readonly IChatMessageRepo _chatMessageRepo;
    private readonly IUserChatsRepo _userChatsRepo;

    public ChatsService(
        ILogger<ChatsService> logger,
        IHistoryConfig historyConfig,
        IChatMessageRepo chatMessageRepo,
        IUserChatsRepo userChatsRepo)
    {
        _logger = logger;
        _historyConfig = historyConfig;
        _chatMessageRepo = chatMessageRepo;
        _userChatsRepo = userChatsRepo;
    }

    [Authorize(Policy = "user")]
    public override async Task<GetChatHistoryResponse> GetChatHistory(GetChatHistoryRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        string chatId = DataUtility.CreateChatId(userClaims.UserId, request.OtherUserId);
        DateTime olderThan = request.OlderThan.ToDateTime();
        IReadOnlyCollection<HistoryMessage> historyMessages = await _chatMessageRepo.GetHistory(
            userClaims.UserId, chatId, olderThan, _historyConfig.MessageCount);

        GetChatHistoryResponse response = new();
        response.Messages.Add(historyMessages);

        _logger.LogTrace("Responding with {MessageCount} messages for chat {ChatId} which are older than {OlderThan}", response.Messages.Count, chatId, olderThan);
        return response;
    }

    [Authorize(Policy = "user")]
    public override async Task<GetUserChatsResponse> GetUserChats(GetUserChatsRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        DateTime newerThan = request.NewerThan.ToDateTime();
        IReadOnlyCollection<ChatState> chats = await _userChatsRepo.GetUserChats(userClaims.UserId, newerThan);

        GetUserChatsResponse response = new();
        response.Chats.Add(chats);

        _logger.LogTrace("Responding with {ChatCount} chats for user {UserId} which are newer than {NewerThan}", chats.Count, userClaims.UserId, newerThan);
        return response;
    }
}
