﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts;
using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.Messaging.Server.Backend;
using CecoChat.Messaging.Server.Identity;
using CecoChat.Server;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CecoChat.Messaging.Server.Clients
{
    public sealed class GrpcSendService : Send.SendBase
    {
        private readonly ILogger _logger;
        private readonly IIdentityClient _identityClient;
        private readonly ISendProducer _sendProducer;
        private readonly IClientContainer _clientContainer;
        private readonly IClientBackendMapper _mapper;

        public GrpcSendService(
            ILogger<GrpcSendService> logger,
            IIdentityClient identityClient,
            ISendProducer sendProducer,
            IClientContainer clientContainer,
            IClientBackendMapper mapper)
        {
            _logger = logger;
            _identityClient = identityClient;
            _sendProducer = sendProducer;
            _clientContainer = clientContainer;
            _mapper = mapper;
        }

        [Authorize(Roles = "user")]
        public override async Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            UserClaims userClaims = GetUserClaims(context);
            long messageID = await GetMessageID(userClaims, context);

            ClientMessage clientMessage = request.Message;
            clientMessage.MessageId = messageID;
            _logger.LogTrace("Message for {0} received {1}.", userClaims, clientMessage);

            BackendMessage backendMessage = _mapper.MapClientToBackendMessage(clientMessage);
            backendMessage.ClientId = userClaims.ClientID.ToUuid();
            _sendProducer.ProduceMessage(backendMessage);

            EnqueueMessagesForSenders(clientMessage, userClaims.ClientID, out int successCount, out int allCount);
            LogResults(clientMessage, successCount, allCount);

            SendMessageResponse response = new() {MessageId = clientMessage.MessageId};
            return response;
        }

        private UserClaims GetUserClaims(ServerCallContext context)
        {
            if (!context.GetHttpContext().User.TryGetUserClaims(out UserClaims userClaims))
            {
                _logger.LogError("Client from {0} was authorized but has no parseable access token.", context.Peer);
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Access token could not be parsed."));
            }

            Activity.Current?.SetTag("user.id", userClaims.UserID);
            return userClaims;
        }

        private async Task<long> GetMessageID(UserClaims userClaims, ServerCallContext context)
        {
            GetIdentityResult result = await _identityClient.GetIdentity(userClaims.UserID, context.CancellationToken);
            if (!result.Success)
            {
                Metadata metadata = new();
                metadata.Add("UserID", userClaims.UserID.ToString());
                throw new RpcException(new Status(StatusCode.Unavailable, "Failed to get a message ID."), metadata);
            }

            return result.ID;
        }

        private void EnqueueMessagesForSenders(ClientMessage clientMessage, Guid senderID, out int successCount, out int allCount)
        {
            // do not call clients.Count since it is expensive and uses locks
            successCount = 0;
            allCount = 0;

            IEnumerable<IStreamer<ListenResponse>> senderClients = _clientContainer.EnumerateClients(clientMessage.SenderId);
            ListenResponse response = new() {Message = clientMessage};

            foreach (IStreamer<ListenResponse> senderClient in senderClients)
            {
                if (senderClient.ClientID != senderID)
                {
                    if (senderClient.EnqueueMessage(response, parentActivity: Activity.Current))
                    {
                        successCount++;
                    }

                    allCount++;
                }
            }
        }

        private void LogResults(ClientMessage clientMessage, int successCount, int allCount)
        {
            if (successCount < allCount)
            {
                _logger.LogWarning("Connected senders with ID {0} ({1} out of {2}) were queued message {3}.",
                    clientMessage.SenderId, successCount, allCount, clientMessage.MessageId);
            }
            else if (allCount > 0)
            {
                _logger.LogTrace("Connected senders with ID {0} (all {1}) were queued message {2}.",
                    clientMessage.SenderId, successCount, clientMessage.MessageId);
            }
        }
    }
}
