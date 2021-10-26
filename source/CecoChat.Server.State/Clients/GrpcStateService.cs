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
        private readonly IChatStateRepo _chatStateRepo;

        public GrpcStateService(
            ILogger<GrpcStateService> logger,
            IChatStateRepo chatStateRepo)
        {
            _logger = logger;
            _chatStateRepo = chatStateRepo;
        }

        [Authorize(Roles = "user")]
        public override async Task<GetChatsResponse> GetChats(GetChatsRequest request, ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserID(out long userID))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Client has no parseable access token."));
            }
            Activity.Current?.SetTag("user.id", userID);

            DateTime newerThan = request.NewerThan.ToDateTime();
            IReadOnlyCollection<ChatState> chats = await _chatStateRepo.GetChats(userID, newerThan);

            GetChatsResponse response = new();
            response.Chats.Add(chats);

            _logger.LogTrace("Responding with {0} chats for user {1}.", chats.Count, userID);
            return response;
        }
    }
}