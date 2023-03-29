using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Server.Identity;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CecoChat.Server.Messaging.Endpoints;

public partial class ChatHub
{
    [Authorize(Roles = "user")]
    public async Task<ReactResponse> React(ReactRequest request)
    {
        ValidationResult result = _inputValidator.ReactRequest.Validate(request);
        if (!result.IsValid)
        {
            throw new ValidationHubException(result.Errors);
        }
        if (!Context.User!.TryGetUserClaims(Context.ConnectionId, _logger, out UserClaims? userClaims))
        {
            throw new HubException(MissingUserDataExMsg);
        }

        _messagingTelemetry.NotifyReactionReceived();
        _logger.LogTrace("User {UserId} with client {ClientId} and connection {ConnectionId} reacted with {Reaction} to message {MessageId} sent by user {SenderId}",
            userClaims.UserId, userClaims.ClientId, Context.ConnectionId, request.Reaction, request.MessageId, request.SenderId);

        BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, initiatorConnection: Context.ConnectionId, reactorId: userClaims.UserId);
        _sendersProducer.ProduceMessage(backplaneMessage);

        ListenNotification notification = _mapper.CreateListenNotification(request, userClaims.UserId);
        await _clientContainer.NotifyInGroup(notification, userClaims.UserId, excluding: Context.ConnectionId);

        return new ReactResponse();
    }

    [Authorize(Roles = "user")]
    public async Task<UnReactResponse> UnReact(UnReactRequest request)
    {
        ValidationResult result = _inputValidator.UnReactRequest.Validate(request);
        if (!result.IsValid)
        {
            throw new ValidationHubException(result.Errors);
        }
        if (!Context.User!.TryGetUserClaims(Context.ConnectionId, _logger, out UserClaims? userClaims))
        {
            throw new HubException(MissingUserDataExMsg);
        }

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
