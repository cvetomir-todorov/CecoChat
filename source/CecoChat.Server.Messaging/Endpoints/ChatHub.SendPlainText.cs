using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Server.Identity;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.Messaging.Endpoints;

public partial class ChatHub
{
    [Authorize(Policy = "user")]
    public async Task<SendPlainTextResponse> SendPlainText(SendPlainTextRequest request)
    {
        ValidationResult result = _inputValidator.SendPlainTextRequest.Validate(request);
        if (!result.IsValid)
        {
            throw new ValidationHubException(result.Errors);
        }

        UserClaims userClaims = Context.User!.GetUserClaimsSignalR(Context.ConnectionId, _logger);

        _messagingTelemetry.NotifyPlainTextReceived();
        long messageId = await GetMessageId();
        _logger.LogTrace("User {UserId} with client {ClientId} and connection {ConnectionId} sent plain text message {MessageId} to user {ReceiverId}",
            userClaims.UserId, userClaims.ClientId, Context.ConnectionId, messageId, request.ReceiverId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, senderId: userClaims.UserId, initiatorConnection: Context.ConnectionId, messageId);
        _sendersProducer.ProduceMessage(backplaneMessage);

        ListenNotification notification = _mapper.CreateListenNotification(request, userClaims.UserId, messageId);
        await _clientContainer.NotifyInGroup(notification, userClaims.UserId, excluding: Context.ConnectionId);

        SendPlainTextResponse response = new() { MessageId = messageId };
        return response;
    }
}
