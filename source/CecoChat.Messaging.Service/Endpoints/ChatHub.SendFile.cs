using CecoChat.Contracts.Backplane;
using CecoChat.Messaging.Contracts;
using CecoChat.Server.Identity;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Messaging.Service.Endpoints;

public partial class ChatHub
{
    [Authorize(Policy = "user")]
    public async Task<SendFileResponse> SendFile(SendFileRequest request)
    {
        ValidationResult result = _inputValidator.SendFileRequest.Validate(request);
        if (!result.IsValid)
        {
            throw new ValidationHubException(result.Errors);
        }

        UserClaims userClaims = Context.User!.GetUserClaimsSignalR(Context.ConnectionId, _logger);

        _messagingTelemetry.NotifyFileReceived();
        long messageId = await GetMessageId();
        _logger.LogTrace("User {UserId} with client {ClientId} and connection {ConnectionId} sent file message {MessageId} to user {ReceiverId}",
            userClaims.UserId, userClaims.ClientId, Context.ConnectionId, messageId, request.ReceiverId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, senderId: userClaims.UserId, initiatorConnection: Context.ConnectionId, messageId);
        _sendersProducer.ProduceMessage(backplaneMessage);

        ListenNotification notification = _mapper.CreateListenNotification(request, userClaims.UserId, messageId);
        await _clientContainer.NotifyInGroup(notification, userClaims.UserId, excluding: Context.ConnectionId);

        SendFileResponse response = new() { MessageId = messageId };
        return response;
    }
}
