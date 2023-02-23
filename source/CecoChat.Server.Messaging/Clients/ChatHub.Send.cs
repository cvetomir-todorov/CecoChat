using CecoChat.Client.IDGen;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Server.Identity;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CecoChat.Server.Messaging.Clients;

public partial class ChatHub
{
    [Authorize(Roles = "user")]
    public async Task<SendMessageResponse> SendMessage(SendMessageRequest request)
    {
        ValidationResult result = _inputValidator.SendMessageRequest.Validate(request);
        if (!result.IsValid)
        {
            throw new ValidationHubException(result.Errors);
        }
        if (!Context.User!.TryGetUserClaims(out UserClaims? userClaims))
        {
            throw new HubException(MissingUserDataExMsg);
        }

        _messagingTelemetry.NotifyPlainTextReceived();
        long messageId = await GetMessageId();
        _logger.LogTrace("User {UserId} with client {ClientId} and connection {ConnectionId} sent message {MessageId} with data {DataType} to user {ReceiverId}",
            userClaims.UserId, userClaims.ClientId, Context.ConnectionId, messageId, request.DataType, request.ReceiverId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, senderId: userClaims.UserId, senderConnectionId: Context.ConnectionId, messageId);
        _sendersProducer.ProduceMessage(backplaneMessage);

        ListenNotification notification = _mapper.CreateListenNotification(request, userClaims.UserId, messageId);
        await _clientContainer.NotifyInGroup(notification, userClaims.UserId, excluding: Context.ConnectionId);

        SendMessageResponse response = new() { MessageId = messageId };
        return response;
    }

    private async Task<long> GetMessageId()
    {
        // SignalR doesn't support yet CancellationToken in the method signature so we have none
        GetIDResult result = await _idGenClient.GetID(CancellationToken.None);
        if (!result.Success)
        {
            throw new HubException("Failed to obtain a message ID.");
        }

        return result.ID;
    }
}
