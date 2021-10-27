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
            if (_cache.TryGetUserChat(senderID, chatID, out ChatState chat))
            {
                chat.NewestMessage = messageID;
            }
            else
            {
                chat = _repo.GetChat(senderID, chatID);
                chat.NewestMessage = messageID;

                _cache.UpdateUserChat(senderID, chat);
            }

            _repo.UpdateChat(senderID, chat);
        }

        private void UpdateReceiverChat(long receiverID, string chatID, long messageID)
        {
            if (_cache.TryGetUserChat(receiverID, chatID, out ChatState chat))
            {
                chat.NewestMessage = messageID;
                chat.OtherUserDelivered = messageID;
                chat.OtherUserSeen = messageID;
            }
            else
            {
                chat = _repo.GetChat(receiverID, chatID);
                chat.NewestMessage = messageID;
                chat.OtherUserDelivered = messageID;
                chat.OtherUserSeen = messageID;

                _cache.UpdateUserChat(receiverID, chat);
            }

            _repo.UpdateChat(receiverID, chat);
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