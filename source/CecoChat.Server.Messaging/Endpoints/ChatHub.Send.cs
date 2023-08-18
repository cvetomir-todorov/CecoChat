using CecoChat.Client.IdGen;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Server.Identity;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CecoChat.Server.Messaging.Endpoints;

public partial class ChatHub
{
    [Authorize(Policy = "user")]
    public async Task<SendMessageResponse> SendMessage(SendMessageRequest request)
    {
        ValidationResult result = _inputValidator.SendMessageRequest.Validate(request);
        if (!result.IsValid)
        {
            throw new ValidationHubException(result.Errors);
        }

        UserClaims userClaims = Context.User!.GetUserClaimsSignalR(Context.ConnectionId, _logger);

        _messagingTelemetry.NotifyPlainTextReceived();
        long messageId = await GetMessageId();
        _logger.LogTrace("User {UserId} with client {ClientId} and connection {ConnectionId} sent message {MessageId} with data {DataType} to user {ReceiverId}",
            userClaims.UserId, userClaims.ClientId, Context.ConnectionId, messageId, request.DataType, request.ReceiverId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, senderId: userClaims.UserId, initiatorConnection: Context.ConnectionId, messageId);
        _sendersProducer.ProduceMessage(backplaneMessage);

        ListenNotification notification = _mapper.CreateListenNotification(request, userClaims.UserId, messageId);
        await _clientContainer.NotifyInGroup(notification, userClaims.UserId, excluding: Context.ConnectionId);

        SendMessageResponse response = new() { MessageId = messageId };
        return response;
    }

    private async Task<long> GetMessageId()
    {
        // SignalR doesn't support yet CancellationToken in the method signature so we have none
        GetIdResult result = await _idGenClient.GetId(CancellationToken.None);
        if (!result.Success)
        {
            throw new HubException("Failed to obtain a message ID.");
        }

        return result.Id;
    }
}
