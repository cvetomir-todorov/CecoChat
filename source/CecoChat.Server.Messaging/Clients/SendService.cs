﻿using System.Diagnostics;
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
        if (!context.GetHttpContext().TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, string.Empty));
        }

        _messagingTelemetry.NotifyPlainTextReceived();
        long messageId = await GetMessageId(userClaims, context);
        _logger.LogTrace("User {UserId} with client {ClientId} sent message {MessageId} with data {DataType} to user {ReceiverId}",
            userClaims.UserId, userClaims.ClientId, messageId, request.DataType, request.ReceiverId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, userClaims.UserId, userClaims.ClientId, messageId);
        _sendersProducer.ProduceMessage(backplaneMessage);
        EnqueueMessageForSenderClients(request, messageId, userClaims);

        SendMessageResponse response = new() { MessageId = messageId };
        return response;
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

    private void EnqueueMessageForSenderClients(SendMessageRequest request, long messageId, UserClaims userClaims)
    {
        // do not call clients.Count since it is expensive and uses locks
        int successCount = 0;
        int allCount = 0;

        IEnumerable<IStreamer<ListenNotification>> senderClients = _clientContainer.EnumerateClients(userClaims.UserId);
        ListenNotification? notification = null;

        foreach (IStreamer<ListenNotification> senderClient in senderClients)
        {
            if (senderClient.ClientId != userClaims.ClientId)
            {
                notification ??= _mapper.CreateListenNotification(request, userClaims.UserId, messageId);

                if (senderClient.EnqueueMessage(notification, parentActivity: Activity.Current))
                {
                    successCount++;
                }

                allCount++;
            }
        }

        LogLevel logLevel = successCount < allCount ? LogLevel.Warning : LogLevel.Trace;
        _logger.Log(logLevel, "Connected senders with ID {SenderId} ({SuccessCount} out of {AllCount}) were queued message {MessageId} with data {DataType} to user {ReceiverId}",
            userClaims.UserId, successCount, allCount, messageId, request.DataType, request.ReceiverId);
    }
}
