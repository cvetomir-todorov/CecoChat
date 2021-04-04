using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
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
            string address = context.Peer;
            if (!context.GetHttpContext().User.TryGetUserClaims(out UserClaims userClaims))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", address);
                return;
            }

            _logger.LogTrace("{0} from {1} connected.", userClaims, address);

            IGrpcStreamer<ListenResponse> streamer = _streamerFactory.Create();
            streamer.SetFinalMessagePredicate(IsFinalMessage);
            streamer.Initialize(userClaims.ClientID, responseStream);
            await ProcessMessages(streamer, userClaims, address, context.CancellationToken);
        }

        private async Task ProcessMessages(IGrpcStreamer<ListenResponse> streamer, UserClaims userClaims, string address, CancellationToken ct)
        {
            bool isClientAdded = false;

            try
            {
                isClientAdded = _clientContainer.AddClient(userClaims.UserID, streamer);
                if (isClientAdded)
                {
                    await streamer.ProcessMessages(ct);
                }
                else
                {
                    _logger.LogError("Failed to add {0} from {1}.", userClaims, address);
                }
            }
            catch (OperationCanceledException operationCanceledException)
            {
                // thrown when client cancels the streaming call, when the deadline is exceeded or a network error
                if (operationCanceledException.InnerException != null)
                {
                    _logger.LogError(operationCanceledException.InnerException, "Listen for {0} from {1} failed.", userClaims, address);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Listen for {0} from {1} failed.", userClaims, address);
            }
            finally
            {
                if (isClientAdded)
                {
                    RemoveClient(userClaims, address, streamer);
                }
                streamer.Dispose();
            }
        }

        private void RemoveClient(UserClaims userClaims, string address, IGrpcStreamer<ListenResponse> streamer)
        {
            _clientContainer.RemoveClient(userClaims.UserID, streamer);
            _logger.LogTrace("{0} from {1} disconnected.", userClaims, address);
        }

        private bool IsFinalMessage(ListenResponse response)
        {
            return response.Message.Type == ClientMessageType.Disconnect;
        }
    }
}
