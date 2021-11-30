using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts.State;
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
            IReadOnlyCollection<ChatState> chats = await _repo.GetChats(userID, newerThan);
            foreach (ChatState chat in chats)
            {
                _cache.UpdateUserChat(userID, chat);
            }

            GetChatsResponse response = new();
            response.Chats.Add(chats);

            _logger.LogTrace("Responding with {0} chats for user {1}.", chats.Count, userID);
            return response;
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