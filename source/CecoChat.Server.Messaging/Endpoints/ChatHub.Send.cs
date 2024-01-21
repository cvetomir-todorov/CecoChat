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
