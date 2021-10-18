using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts.Messaging;
using CecoChat.Data.History;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CecoChat.History.Server.Clients
{
    public class GrpcReactionService : Reaction.ReactionBase
    {
        private readonly ILogger _logger;
        private readonly IReactionRepository _reactionRepository;

        public GrpcReactionService(
            ILogger<GrpcReactionService> logger,
            IReactionRepository reactionRepository)
        {
            _logger = logger;
            _reactionRepository = reactionRepository;
        }

        [Authorize(Roles = "user")]
        public override async Task<ReactResponse> React(ReactRequest request, ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserID(out long userID))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                return new ReactResponse();
            }
            Activity.Current?.SetTag("reactor.id", userID);

            await _reactionRepository.AddReaction(request.MessageId, request.SenderId, request.ReceiverId, userID, request.Reaction);
            return new ReactResponse();
        }

        [Authorize(Roles = "user")]
        public override async Task<UnReactResponse> UnReact(UnReactRequest request, ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserID(out long userID))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                return new UnReactResponse();
            }
            Activity.Current?.SetTag("reactor.id", userID);

            await _reactionRepository.RemoveReaction(request.MessageId, request.SenderId, request.ReceiverId, userID);
            return new UnReactResponse();
        }
    }
}