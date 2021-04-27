using System;
using System.Collections.Generic;
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
        private readonly IFactory<IGrpcListenStreamer> _streamerFactory;

        public GrpcListenService(
            ILogger<GrpcListenService> logger,
            IClientContainer clientContainer,
            IFactory<IGrpcListenStreamer> streamerFactory)
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

            _logger.LogInformation("{0} from {1} connected.", userClaims, address);

            IGrpcListenStreamer streamer = _streamerFactory.Create();
            streamer.Initialize(userClaims.ClientID, responseStream, StaticStreamingStrategy.Instance);
            await ProcessMessages(streamer, userClaims, address, context.CancellationToken);
        }

        private async Task ProcessMessages(IGrpcListenStreamer streamer, UserClaims userClaims, string address, CancellationToken ct)
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
                    _clientContainer.RemoveClient(userClaims.UserID, streamer);
                    _logger.LogInformation("{0} from {1} disconnected.", userClaims, address);
                }
                streamer.Dispose();
            }
        }

        private sealed class StaticStreamingStrategy : IStreamingStrategy
        {
            public static readonly StaticStreamingStrategy Instance = new();

            private static readonly HashSet<ClientMessageType> _blacklistedTypes = new()
            {
                ClientMessageType.Disconnect, ClientMessageType.Unknown
            };

            private StaticStreamingStrategy()
            {}

            public bool IsFinal(ListenResponse response)
            {
                return response.Message.Type == ClientMessageType.Disconnect;
            }

            public bool AffectsSequencing(ListenResponse response)
            {
                return !_blacklistedTypes.Contains(response.Message.Type);
            }
        }
    }
}
