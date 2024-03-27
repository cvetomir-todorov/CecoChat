using CecoChat.Contracts.Backplane;
using CecoChat.Messaging.Contracts;
using CecoChat.Server.Identity;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Messaging.Service.Endpoints;

public partial class ChatHub
{
    [Authorize(Policy = "user")]
    public async Task<ReactResponse> React(ReactRequest request)
    {
        ValidationResult result = _inputValidator.ReactRequest.Validate(request);
        if (!result.IsValid)
        {
            throw new ValidationHubException(result.Errors);
        }

        UserClaims userClaims = Context.User!.GetUserClaimsSignalR(Context.ConnectionId, _logger);

        _messagingTelemetry.NotifyReactionReceived();
        _logger.LogTrace("User {UserId} with client {ClientId} and connection {ConnectionId} reacted with {Reaction} to message {MessageId} sent by user {SenderId}",
            userClaims.UserId, userClaims.ClientId, Context.ConnectionId, request.Reaction, request.MessageId, request.SenderId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, initiatorConnection: Context.ConnectionId, reactorId: userClaims.UserId);
        _sendersProducer.ProduceMessage(backplaneMessage);

        ListenNotification notification = _mapper.CreateListenNotification(request, userClaims.UserId);
        await _clientContainer.NotifyInGroup(notification, userClaims.UserId, excluding: Context.ConnectionId);

        return new ReactResponse();
    }

    [Authorize(Policy = "user")]
    public async Task<UnReactResponse> UnReact(UnReactRequest request)
    {
        ValidationResult result = _inputValidator.UnReactRequest.Validate(request);
        if (!result.IsValid)
        {
            throw new ValidationHubException(result.Errors);
        }

        UserClaims userClaims = Context.User!.GetUserClaimsSignalR(Context.ConnectionId, _logger);

        _messagingTelemetry.NotifyUnreactionReceived();
        _logger.LogTrace("User {UserId} with client {ClientId} and connection {ConnectionId} un-reacted to message {MessageId} sent by user {SenderId}",
            userClaims.UserId, userClaims.ClientId, Context.ConnectionId, request.MessageId, request.SenderId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, initiatorConnection: Context.ConnectionId, reactorId: userClaims.UserId);
        _sendersProducer.ProduceMessage(backplaneMessage);

        ListenNotification notification = _mapper.CreateListenNotification(request, userClaims.UserId);
        await _clientContainer.NotifyInGroup(notification, userClaims.UserId, excluding: Context.ConnectionId);

        return new UnReactResponse();
    }
}
