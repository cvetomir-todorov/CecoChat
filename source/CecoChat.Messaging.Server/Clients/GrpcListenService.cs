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
            if (!context.GetHttpContext().User.TryGetUserID(out long userID))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                return;
            }

            // TODO: use client ID from metadata or auth token
            string clientID = context.Peer;
            _logger.LogTrace("Client {0} connected.", clientID);

            IGrpcStreamer<ListenResponse> streamer = _streamerFactory.Create();
            streamer.SetFinalMessagePredicate(IsFinalMessage);
            streamer.Initialize(responseStream, context);

            try
            {
                _clientContainer.AddClient(userID, streamer);
                await streamer.ProcessMessages(context.CancellationToken);

                RemoveClient(userID, clientID, streamer);
            }
            catch (OperationCanceledException)
            {
                RemoveClient(userID, clientID, streamer);
            }
            finally
            {
                streamer.Dispose();
            }
        }

        private void RemoveClient(long userID, string clientID, IGrpcStreamer<ListenResponse> streamer)
        {
            _clientContainer.RemoveClient(userID, streamer);
            _logger.LogTrace("Client {0} disconnected.", clientID);
        }

        private bool IsFinalMessage(ListenResponse response)
        {
            return response.Message.Type == ClientMessageType.Disconnect;
        }
    }
}
