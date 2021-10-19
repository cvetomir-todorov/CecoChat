using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Messaging.Server.Backplane;
using CecoChat.Server;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CecoChat.Messaging.Server.Clients
{
    public class GrpcReactionService : Reaction.ReactionBase
    {
        private readonly ILogger _logger;
        private readonly ISendersProducer _sendersProducer;
        private readonly IContractDataMapper _mapper;

        public GrpcReactionService(
            ILogger<GrpcReactionService> logger,
            ISendersProducer sendersProducer,
            IContractDataMapper mapper)
        {
            _logger = logger;
            _sendersProducer = sendersProducer;
            _mapper = mapper;
        }

        [Authorize(Roles = "user")]
        public override Task<ReactResponse> React(ReactRequest request, ServerCallContext context)
        {
            UserClaims userClaims = GetUserClaims(context);
            _logger.LogTrace("User {0} reacted {1}.", userClaims, request);

            BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, userClaims.ClientID, userClaims.UserID);
            _sendersProducer.ProduceMessage(backplaneMessage);

            return Task.FromResult(new ReactResponse());
        }

        [Authorize(Roles = "user")]
        public override Task<UnReactResponse> UnReact(UnReactRequest request, ServerCallContext context)
        {
            UserClaims userClaims = GetUserClaims(context);
            _logger.LogTrace("User {0} un-reacted {1}.", userClaims, request);

            BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, userClaims.ClientID, userClaims.UserID);
            _sendersProducer.ProduceMessage(backplaneMessage);

            return Task.FromResult(new UnReactResponse());
        }

        private UserClaims GetUserClaims(ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserClaims(out UserClaims userClaims))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Access token could not be parsed."));
            }

            Activity.Current?.SetTag("reactor.id", userClaims.UserID);
            return userClaims;
        }
    }
}