using System.Diagnostics;
using CecoChat.Client.IDGen;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Server.Identity;
using CecoChat.Server.Messaging.Backplane;
using CecoChat.Server.Messaging.Clients.Streaming;
using CecoChat.Server.Messaging.Telemetry;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.Messaging.Clients;

public sealed class SendService : Send.SendBase
{
    private readonly ILogger _logger;
    private readonly IIDGenClient _idGenClient;
    private readonly ISendersProducer _sendersProducer;
    private readonly IClientContainer _clientContainer;
    private readonly IContractDataMapper _mapper;
    private readonly IMessagingTelemetry _messagingTelemetry;

    public SendService(
        ILogger<SendService> logger,
        IIDGenClient idGenClient,
        ISendersProducer sendersProducer,
        IClientContainer clientContainer,
        IContractDataMapper mapper,
        IMessagingTelemetry messagingTelemetry)
    {
        _logger = logger;
        _idGenClient = idGenClient;
        _sendersProducer = sendersProducer;
        _clientContainer = clientContainer;
        _mapper = mapper;
        _messagingTelemetry = messagingTelemetry;
    }

    [Authorize(Roles = "user")]
    public override async Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
    {
        UserClaims userClaims = GetUserClaims(context);
        long messageId = await GetMessageId(userClaims, context);

        _messagingTelemetry.NotifyMessageReceived();
        _logger.LogTrace("User {UserId} with client {ClientId} sent message {MessageId} with data {DataType} to user {ReceiverId}",
            userClaims.UserId, userClaims.ClientId, messageId, request.DataType, request.ReceiverId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, userClaims.ClientId, messageId);
        _sendersProducer.ProduceMessage(backplaneMessage);
        EnqueueMessagesForSenders(request, messageId, userClaims.ClientId);

        SendMessageResponse response = new() { MessageId = messageId };
        return response;
    }

    private UserClaims GetUserClaims(ServerCallContext context)
    {
        if (!context.GetHttpContext().User.TryGetUserClaims(out UserClaims? userClaims))
        {
            _logger.LogError("Client from {Address} was authorized but has no parseable access token", context.Peer);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Access token could not be parsed."));
        }

        Activity.Current?.SetTag("user.id", userClaims.UserId);
        return userClaims;
    }

    private async Task<long> GetMessageId(UserClaims userClaims, ServerCallContext context)
    {
        GetIDResult result = await _idGenClient.GetID(context.CancellationToken);
        if (!result.Success)
        {
            Metadata metadata = new();
            metadata.Add("UserId", userClaims.UserId.ToString());
            throw new RpcException(new Status(StatusCode.Unavailable, "Failed to get a message ID."), metadata);
        }

        return result.ID;
    }

    private void EnqueueMessagesForSenders(SendMessageRequest request, long messageId, Guid senderClientId)
    {
        // do not call clients.Count since it is expensive and uses locks
        int successCount = 0;
        int allCount = 0;

        IEnumerable<IStreamer<ListenNotification>> senderClients = _clientContainer.EnumerateClients(request.SenderId);
        ListenNotification notification = _mapper.CreateListenNotification(request, messageId);

        foreach (IStreamer<ListenNotification> senderClient in senderClients)
        {
            if (senderClient.ClientID != senderClientId)
            {
                if (senderClient.EnqueueMessage(notification, parentActivity: Activity.Current))
                {
                    successCount++;
                }

                allCount++;
            }
        }

        LogLevel logLevel = successCount < allCount ? LogLevel.Warning : LogLevel.Trace;
        _logger.Log(logLevel, "Connected senders with ID {SenderId} ({SuccessCount} out of {AllCount}) were queued message {MessageId} with data {DataType} to user {ReceiverId}",
            request.SenderId, successCount, allCount, messageId, request.DataType, request.ReceiverId);
    }
}
