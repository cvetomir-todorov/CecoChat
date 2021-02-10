using System;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using CecoChat.DependencyInjection;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CecoChat.Messaging.Server.Clients
{
    public sealed class GrpcListenService : Listen.ListenBase
    {
        private readonly ILogger _logger;
        private readonly IClientContainer _clientContainer;
        private readonly IFactory<IGrpcStreamer<ListenResponse>> _streamerFactory;

        public GrpcListenService(
            ILogger<GrpcListenService> logger,
            IClientContainer clientContainer,
            IFactory<IGrpcStreamer<ListenResponse>> streamerFactory)
        {
            _logger = logger;
            _clientContainer = clientContainer;
            _streamerFactory = streamerFactory;
        }

        [Authorize(Roles = "user")]
        public override async Task Listen(ListenRequest request, IServerStreamWriter<ListenResponse> responseStream, ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserClaims(out UserClaims userClaims))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                return;
            }

            long userID = userClaims.UserID;
            Guid clientID = userClaims.ClientID;
            _logger.LogTrace("{0} connected.", userClaims);

            IGrpcStreamer<ListenResponse> streamer = _streamerFactory.Create();
            streamer.SetFinalMessagePredicate(IsFinalMessage);
            streamer.Initialize(clientID, responseStream);

            try
            {
                _clientContainer.AddClient(userID, streamer);
                await streamer.ProcessMessages(context.CancellationToken);

                RemoveClient(userClaims, streamer);
            }
            catch (OperationCanceledException)
            {
                RemoveClient(userClaims, streamer);
            }
            finally
            {
                streamer.Dispose();
            }
        }

        private void RemoveClient(UserClaims userClaims, IGrpcStreamer<ListenResponse> streamer)
        {
            _clientContainer.RemoveClient(userClaims.UserID, streamer);
            _logger.LogTrace("{0} disconnected.", userClaims);
        }

        private bool IsFinalMessage(ListenResponse response)
        {
            return response.Message.Type == ClientMessageType.Disconnect;
        }
    }
}
