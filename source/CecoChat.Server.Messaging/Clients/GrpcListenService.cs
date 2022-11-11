using CecoChat.Contracts.Messaging;
using CecoChat.Server.Identity;
using CecoChat.Server.Messaging.Clients.Streaming;
using CecoChat.Server.Messaging.Telemetry;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.Messaging.Clients;

public sealed class GrpcListenService : Listen.ListenBase
{
    private readonly ILogger _logger;
    private readonly IClientContainer _clientContainer;
    private readonly IFactory<IGrpcListenStreamer> _streamerFactory;
    private readonly IMessagingTelemetry _messagingTelemetry;

    public GrpcListenService(
        ILogger<GrpcListenService> logger,
        IClientContainer clientContainer,
        IFactory<IGrpcListenStreamer> streamerFactory,
        IMessagingTelemetry messagingTelemetry)
    {
        _logger = logger;
        _clientContainer = clientContainer;
        _streamerFactory = streamerFactory;
        _messagingTelemetry = messagingTelemetry;
    }

    [Authorize(Roles = "user")]
    public override async Task Listen(ListenSubscription subscription, IServerStreamWriter<ListenNotification> notificationStream, ServerCallContext context)
    {
        string address = context.Peer;
        if (!context.GetHttpContext().User.TryGetUserClaims(out UserClaims? userClaims))
        {
            _logger.LogError("Client from {ClientAddress} was authorized but has no parseable access token", address);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Access token could not be parsed."));
        }

        _logger.LogInformation("{@User} from {Address} connected", userClaims, address);

        IGrpcListenStreamer streamer = _streamerFactory.Create();
        streamer.Initialize(userClaims!.ClientID, notificationStream);
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
                _messagingTelemetry.AddOnlineClient();
                await streamer.ProcessMessages(ct);
            }
            else
            {
                _logger.LogError("Failed to add {@User} from {Address}", userClaims, address);
            }
        }
        catch (OperationCanceledException operationCanceledException)
        {
            // thrown when client cancels the streaming call, when the deadline is exceeded or a network error
            if (operationCanceledException.InnerException != null)
            {
                _logger.LogError(operationCanceledException.InnerException, "Listen for {@User} from {Address} failed", userClaims, address);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Listen for {@User} from {Address} failed", userClaims, address);
        }
        finally
        {
            if (isClientAdded)
            {
                _clientContainer.RemoveClient(userClaims.UserID, streamer);
                _messagingTelemetry.RemoveOnlineClient();
                _logger.LogInformation("{@User} from {Address} disconnected", userClaims, address);
            }
            streamer.Dispose();
        }
    }
}
