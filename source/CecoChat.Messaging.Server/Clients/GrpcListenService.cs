using System;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using CecoChat.DependencyInjection;
using Grpc.Core;
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

        public override async Task Listen(ListenRequest request, IServerStreamWriter<ListenResponse> responseStream, ServerCallContext context)
        {
            // TODO: use client ID from metadata or auth token
            string clientID = context.Peer;
            _logger.LogTrace("Client {0} connected.", clientID);

            IGrpcStreamer<ListenResponse> streamer = _streamerFactory.Create();
            streamer.Initialize(responseStream, context);

            try
            {
                // TODO: use user ID from auth token
                _clientContainer.AddClient(request.UserId, streamer);
                await streamer.ProcessMessages(context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                _clientContainer.RemoveClient(request.UserId, streamer);
                _logger.LogTrace("Client {0} disconnected.", clientID);
            }
            finally
            {
                streamer.Dispose();
            }
        }
    }
}
