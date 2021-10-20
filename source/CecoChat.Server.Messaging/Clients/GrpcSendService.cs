using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Data.IDGen;
using CecoChat.Server.Identity;
using CecoChat.Server.Messaging.Backplane;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CecoChat.Server.Messaging.Clients
{
    public sealed class GrpcSendService : Send.SendBase
    {
        private readonly ILogger _logger;
        private readonly IIDGenClient _idGenClient;
        private readonly ISendersProducer _sendersProducer;
        private readonly IClientContainer _clientContainer;
        private readonly IContractDataMapper _mapper;

        public GrpcSendService(
            ILogger<GrpcSendService> logger,
            IIDGenClient idGenClient,
            ISendersProducer sendersProducer,
            IClientContainer clientContainer,
            IContractDataMapper mapper)
        {
            _logger = logger;
            _idGenClient = idGenClient;
            _sendersProducer = sendersProducer;
            _clientContainer = clientContainer;
            _mapper = mapper;
        }

        [Authorize(Roles = "user")]
        public override async Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
        {
            UserClaims userClaims = GetUserClaims(context);
            long messageID = await GetMessageID(userClaims, context);

            _logger.LogTrace("User {0} sent message with generated ID {1}: {2}.", userClaims, messageID, request);

            BackplaneMessage backplaneMessage = _mapper.CreateBackplaneMessage(request, userClaims.ClientID, messageID);
            _sendersProducer.ProduceMessage(backplaneMessage);

            (int successCount, int allCount) = EnqueueMessagesForSenders(request, messageID, userClaims.ClientID);
            LogResults(request, messageID, successCount, allCount);

            SendMessageResponse response = new() {MessageId = messageID};
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
            GetIDResult result = await _idGenClient.GetID(userClaims.UserID, context.CancellationToken);
            if (!result.Success)
            {
                Metadata metadata = new();
                metadata.Add("UserID", userClaims.UserID.ToString());
                throw new RpcException(new Status(StatusCode.Unavailable, "Failed to get a message ID."), metadata);
            }

            return result.ID;
        }

        private (int successCount, int allCount) EnqueueMessagesForSenders(SendMessageRequest request, long messageID, Guid senderClientID)
        {
            // do not call clients.Count since it is expensive and uses locks
            int successCount = 0;
            int allCount = 0;

            IEnumerable<IStreamer<ListenNotification>> senderClients = _clientContainer.EnumerateClients(request.SenderId);
            ListenNotification notification = _mapper.CreateListenNotification(request, messageID);

            foreach (IStreamer<ListenNotification> senderClient in senderClients)
            {
                if (senderClient.ClientID != senderClientID)
                {
                    if (senderClient.EnqueueMessage(notification, parentActivity: Activity.Current))
                    {
                        successCount++;
                    }

                    allCount++;
                }
            }

            return (successCount, allCount);
        }

        private void LogResults(SendMessageRequest request, long messageID, int successCount, int allCount)
        {
            if (successCount < allCount)
            {
                _logger.LogWarning("Connected senders with ID {0} ({1} out of {2}) were queued message {3}.",
                    request.SenderId, successCount, allCount, messageID);
            }
            else if (allCount > 0)
            {
                _logger.LogTrace("Connected senders with ID {0} (all {1}) were queued message {2}.",
                    request.SenderId, successCount, messageID);
            }
        }
    }
}
