using System.Diagnostics;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Server.Identity;
using CecoChat.Server.Messaging.Backplane;
using CecoChat.Server.Messaging.Clients.Streaming;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.Messaging.Clients;

public class ReactService : Reaction.ReactionBase
{
    private readonly ILogger _logger;
    private readonly ISendersProducer _sendersProducer;
    private readonly IClientContainer _clientContainer;
    private readonly IContractDataMapper _mapper;

    public ReactService(
        ILogger<ReactService> logger,
        ISendersProducer sendersProducer,
        IClientContainer clientContainer,
        IContractDataMapper mapper)
    {
        _logger = logger;
        _sendersProducer = sendersProducer;
        _clientContainer = clientContainer;
        _mapper = mapper;
    }

    [Authorize(Roles = "user")]
    public override Task<ReactResponse> React(ReactRequest request, ServerCallContext context)
    {
        UserClaims userClaims = GetUserClaims(context);
        _logger.LogTrace("User {UserId} with client {ClientId} reacted with {Reaction} to message {MessageId} sent by user {SenderId}",
            userClaims.UserId, userClaims.ClientId, request.Reaction, request.MessageId, request.SenderId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, userClaims.ClientId, userClaims.UserId);
        _sendersProducer.ProduceMessage(backplaneMessage);

        ListenNotification notification = _mapper.CreateListenNotification(request, userClaims.UserId);
        EnqueueReactionForSenders(notification, userClaims.ClientId);

        return Task.FromResult(new ReactResponse());
    }

    [Authorize(Roles = "user")]
    public override Task<UnReactResponse> UnReact(UnReactRequest request, ServerCallContext context)
    {
        UserClaims userClaims = GetUserClaims(context);
        _logger.LogTrace("User {UserId} with client {ClientId} un-reacted to message {MessageId} sent by user {SenderId}",
            userClaims.UserId, userClaims.ClientId, request.MessageId, request.SenderId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, userClaims.ClientId, userClaims.UserId);
        _sendersProducer.ProduceMessage(backplaneMessage);

        ListenNotification notification = _mapper.CreateListenNotification(request, userClaims.UserId);
        EnqueueReactionForSenders(notification, userClaims.ClientId);

        return Task.FromResult(new UnReactResponse());
    }

    private UserClaims GetUserClaims(ServerCallContext context)
    {
        if (!context.GetHttpContext().User.TryGetUserClaims(out UserClaims? userClaims))
        {
            _logger.LogError("Client from {ClientAddress} was authorized but has no parseable access token", context.Peer);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Access token could not be parsed."));
        }

        Activity.Current?.SetTag("reactor.id", userClaims.UserId);
        return userClaims;
    }

    private void EnqueueReactionForSenders(ListenNotification notification, Guid senderClientId)
    {
        // do not call clients.Count since it is expensive and uses locks
        int successCount = 0;
        int allCount = 0;

        IEnumerable<IStreamer<ListenNotification>> senderClients = _clientContainer.EnumerateClients(notification.SenderId);

        foreach (IStreamer<ListenNotification> senderClient in senderClients)
        {
            if (senderClient.ClientId != senderClientId)
            {
                if (senderClient.EnqueueMessage(notification, parentActivity: Activity.Current))
                {
                    successCount++;
                }

                allCount++;
            }
        }

        LogLevel logLevel = successCount < allCount ? LogLevel.Warning : LogLevel.Trace;
        _logger.Log(logLevel, "Connected senders with ID {SenderId} ({SuccessCount} out of {AllCount}) were queued (un)reaction '{Reaction}' by {ReactorId} to {MessageId}",
            notification.SenderId, successCount, allCount, notification.Reaction.Reaction ?? string.Empty, notification.Reaction.ReactorId, notification.MessageId);
    }
}
