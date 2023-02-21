using System.Diagnostics;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Server.Identity;
using CecoChat.Server.Messaging.Backplane;
using CecoChat.Server.Messaging.Clients.Streaming;
using CecoChat.Server.Messaging.Telemetry;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.Messaging.Clients;

public class ReactService : Reaction.ReactionBase
{
    private readonly ILogger _logger;
    private readonly ISendersProducer _sendersProducer;
    private readonly IClientContainer _clientContainer;
    private readonly IContractMapper _mapper;
    private readonly IMessagingTelemetry _messagingTelemetry;

    public ReactService(
        ILogger<ReactService> logger,
        ISendersProducer sendersProducer,
        IClientContainer clientContainer,
        IContractMapper mapper,
        IMessagingTelemetry messagingTelemetry)
    {
        _logger = logger;
        _sendersProducer = sendersProducer;
        _clientContainer = clientContainer;
        _mapper = mapper;
        _messagingTelemetry = messagingTelemetry;
    }

    [Authorize(Roles = "user")]
    public override Task<ReactResponse> React(ReactRequest request, ServerCallContext context)
    {
        if (!context.GetHttpContext().TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, string.Empty));
        }

        _messagingTelemetry.NotifyReactionReceived();
        _logger.LogTrace("User {UserId} with client {ClientId} reacted with {Reaction} to message {MessageId} sent by user {SenderId}",
            userClaims.UserId, userClaims.ClientId, request.Reaction, request.MessageId, request.SenderId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, userClaims.ClientId, userClaims.UserId);
        _sendersProducer.ProduceMessage(backplaneMessage);
        EnqueueReactionForReactorClients(
            userClaims.UserId, userClaims.ClientId, request.MessageId,
            () => _mapper.CreateListenNotification(request, userClaims.UserId));

        return Task.FromResult(new ReactResponse());
    }

    [Authorize(Roles = "user")]
    public override Task<UnReactResponse> UnReact(UnReactRequest request, ServerCallContext context)
    {
        if (!context.GetHttpContext().TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, string.Empty));
        }

        _messagingTelemetry.NotifyUnreactionReceived();
        _logger.LogTrace("User {UserId} with client {ClientId} un-reacted to message {MessageId} sent by user {SenderId}",
            userClaims.UserId, userClaims.ClientId, request.MessageId, request.SenderId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, userClaims.ClientId, userClaims.UserId);
        _sendersProducer.ProduceMessage(backplaneMessage);
        EnqueueReactionForReactorClients(
            userClaims.UserId, userClaims.ClientId, request.MessageId,
            () => _mapper.CreateListenNotification(request, userClaims.UserId));

        return Task.FromResult(new UnReactResponse());
    }

    private void EnqueueReactionForReactorClients(long reactorId, Guid reactorClientId, long messageId, Func<ListenNotification> notificationFactory)
    {
        // do not call clients.Count since it is expensive and uses locks
        int successCount = 0;
        int allCount = 0;

        IEnumerable<IStreamer<ListenNotification>> reactorClients = _clientContainer.EnumerateClients(reactorId);
        ListenNotification? notification = null;

        foreach (IStreamer<ListenNotification> reactorClient in reactorClients)
        {
            if (reactorClient.ClientId != reactorClientId)
            {
                notification ??= notificationFactory();

                if (reactorClient.EnqueueMessage(notification, parentActivity: Activity.Current))
                {
                    successCount++;
                }

                allCount++;
            }
        }

        LogLevel logLevel = successCount < allCount ? LogLevel.Warning : LogLevel.Trace;
        _logger.Log(logLevel, "Connected senders with ID {SenderId} ({SuccessCount} out of {AllCount}) were queued (un)reaction by {ReactorId} to {MessageId}",
            reactorId, successCount, allCount, reactorId, messageId);
    }
}
