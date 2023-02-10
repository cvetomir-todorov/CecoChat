using CecoChat.Contracts.Messaging;
using CecoChat.Server.Identity;
using CecoChat.Server.Messaging.Clients.Streaming;
using CecoChat.Server.Messaging.Telemetry;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.Messaging.Clients;

public sealed class ListenService : Listen.ListenBase
{
    private readonly ILogger _logger;
    private readonly IClientContainer _clientContainer;
    private readonly IFactory<IListenStreamer> _streamerFactory;
    private readonly IMessagingTelemetry _messagingTelemetry;

    public ListenService(
        ILogger<ListenService> logger,
        IClientContainer clientContainer,
        IFactory<IListenStreamer> streamerFactory,
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
        if (!context.GetHttpContext().TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, string.Empty));
        }

        string address = context.Peer;
        _logger.LogInformation("Connect for user {UserId} with client {ClientId} from {Address}", userClaims.UserId, userClaims.ClientId, address);

        IListenStreamer streamer = _streamerFactory.Create();
        streamer.Initialize(userClaims.ClientId, notificationStream);
        await ProcessMessages(streamer, userClaims, address, context.CancellationToken);
    }

    private async Task ProcessMessages(IListenStreamer streamer, UserClaims userClaims, string address, CancellationToken ct)
    {
        bool isClientAdded = false;

        try
        {
            isClientAdded = _clientContainer.AddClient(userClaims.UserId, streamer);
            if (isClientAdded)
            {
                _messagingTelemetry.AddOnlineClient();
                await streamer.ProcessMessages(ct);
            }
            else
            {
                _logger.LogError("Failure to add user {UserId} with client {ClientId} from {Address}", userClaims.UserId, userClaims.ClientId, address);
            }
        }
        catch (OperationCanceledException operationCanceledException)
        {
            // thrown when client cancels the streaming call, when the deadline is exceeded or a network error
            if (operationCanceledException.InnerException != null)
            {
                _logger.LogError(operationCanceledException.InnerException, "Failure to listen to user {UserId} with client {ClientId} from {Address}", userClaims.UserId, userClaims.ClientId, address);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failure to listen to user {UserId} with client {ClientId} from {Address}", userClaims.UserId, userClaims.ClientId, address);
        }
        finally
        {
            if (isClientAdded)
            {
                _clientContainer.RemoveClient(userClaims.UserId, streamer);
                _messagingTelemetry.RemoveOnlineClient();
                _logger.LogInformation("Disconnect for user {UserId} with client {ClientId} from {Address}", userClaims.UserId, userClaims.ClientId, address);
            }
            streamer.Dispose();
        }
    }
}
