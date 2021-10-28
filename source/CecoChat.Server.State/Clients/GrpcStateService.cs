using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts.State;
using CecoChat.Data;
using CecoChat.Data.State.Repos;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CecoChat.Server.State.Clients
{
    public class GrpcStateService : Contracts.State.State.StateBase
    {
        private readonly ILogger _logger;
        private readonly IChatStateRepo _repo;
        private readonly IStateCache _cache;

        public GrpcStateService(
            ILogger<GrpcStateService> logger,
            IChatStateRepo repo,
            IStateCache cache)
        {
            _logger = logger;
            _repo = repo;
            _cache = cache;
        }

        [Authorize(Roles = "user")]
        public override async Task<GetChatsResponse> GetChats(GetChatsRequest request, ServerCallContext context)
        {
            long userID = GetUserID(context);
            DateTime newerThan = request.NewerThan.ToDateTime();
            IReadOnlyCollection<ChatState> chatsFromDB = await _repo.GetChats(userID, newerThan);
            _cache.UpdateUserChats(userID, chatsFromDB);
            IReadOnlyCollection<ChatState> chats = _cache.GetUserChats(userID);

            GetChatsResponse response = new();
            response.Chats.Add(chats);

            _logger.LogTrace("Responding with {0} chats for user {1}.", chats.Count, userID);
            return response;
        }

        [Authorize(Roles = "user")]
        public override Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            long userID = GetUserID(context);
            string chatID = DataUtility.CreateChatID(userID, request.ReceiverId);
            UpdateSenderChat(userID, chatID, request.MessageId);
            UpdateReceiverChat(request.ReceiverId, chatID, request.MessageId);

            return Task.FromResult(new SendMessageResponse());
        }

        private void UpdateSenderChat(long senderID, string chatID, long messageID)
        {
            ChatState chat = GetChatFromDBIntoCache(senderID, chatID);
            chat.NewestMessage = Math.Max(chat.NewestMessage, messageID);

            _repo.UpdateChat(senderID, chat);
        }

        private void UpdateReceiverChat(long receiverID, string chatID, long messageID)
        {
            ChatState chat = GetChatFromDBIntoCache(receiverID, chatID);
            chat.NewestMessage = Math.Max(chat.NewestMessage, messageID);
            chat.OtherUserDelivered = Math.Max(chat.OtherUserDelivered, messageID);
            chat.OtherUserSeen = Math.Max(chat.OtherUserSeen, messageID);

            _repo.UpdateChat(receiverID, chat);
        }

        private ChatState GetChatFromDBIntoCache(long userID, string chatID)
        {
            if (!_cache.TryGetUserChat(userID, chatID, out ChatState chat))
            {
                chat = _repo.GetChat(userID, chatID);
                chat ??= new ChatState
                {
                    ChatId = chatID
                };

                _cache.UpdateUserChat(userID, chat);
            }

            return chat;
        }

        private static long GetUserID(ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserID(out long userID))
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Client has no parseable access token."));
            }
            Activity.Current?.SetTag("user.id", userID);
            return userID;
        }
    }
}