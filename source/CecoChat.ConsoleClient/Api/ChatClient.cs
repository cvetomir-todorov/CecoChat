using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Bff;
using CecoChat.Contracts.Messaging;
using CecoChat.Data;
using Grpc.Core;
using Grpc.Net.Client;
using Refit;

namespace CecoChat.ConsoleClient.Api
{
    public sealed class ChatClient : IDisposable
    {
        private long _userID;
        private string _accessToken;
        private string _messagingServerAddress;
        private IBffClient _bffClient;
        private Metadata _grpcMetadata;
        private GrpcChannel _messagingChannel;
        private Listen.ListenClient _listenClient;
        private Send.SendClient _sendClient;
        private Reaction.ReactionClient _reactionClient;

        public void Dispose()
        {
            _bffClient?.Dispose();
            _messagingChannel?.ShutdownAsync().Wait();
            _messagingChannel?.Dispose();
        }

        public long UserID => _userID;

        public async Task CreateSession(string username, string password, string bffAddress)
        {
            _bffClient?.Dispose();
            _bffClient = RestService.For<IBffClient>(bffAddress);

            CreateSessionRequest request = new()
            {
                Username = username,
                Password = password
            };
            CreateSessionResponse response = await _bffClient.CreateSession(request);
            ProcessAccessToken(response.AccessToken);
            _messagingServerAddress = response.MessagingServerAddress;
        }

        private void ProcessAccessToken(string accessToken)
        {
            _accessToken = accessToken;

            _grpcMetadata = new();
            _grpcMetadata.Add("Authorization", $"Bearer {accessToken}");

            JwtSecurityToken jwt = new(accessToken);
            _userID = long.Parse(jwt.Subject);
        }

        public void StartMessaging(CancellationToken ct)
        {
            _messagingChannel?.Dispose();
            _messagingChannel = GrpcChannel.ForAddress(_messagingServerAddress);

            _listenClient = new Listen.ListenClient(_messagingChannel);
            _sendClient = new Send.SendClient(_messagingChannel);
            _reactionClient = new Reaction.ReactionClient(_messagingChannel);

            ListenSubscription subscription = new();
            AsyncServerStreamingCall<ListenNotification> serverStream = _listenClient.Listen(subscription, _grpcMetadata, cancellationToken: ct);
            Task.Factory.StartNew(
                async () => await ListenForNewMessages(serverStream, ct),
                TaskCreationOptions.LongRunning);
        }

        private async Task ListenForNewMessages(AsyncServerStreamingCall<ListenNotification> serverStream, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && await serverStream.ResponseStream.MoveNext())
                {
                    ListenNotification notification = serverStream.ResponseStream.Current;
                    switch (notification.Type)
                    {
                        case MessageType.Data:
                            MessageReceived?.Invoke(this, notification);
                            break;
                        case MessageType.Disconnect:
                            Disconnected?.Invoke(this, EventArgs.Empty);
                            break;
                        case MessageType.Delivery:
                            MessageDelivered?.Invoke(this, notification);
                            break;
                        case MessageType.Reaction:
                            ReactionReceived?.Invoke(this, notification);
                            break;
                        default:
                            throw new EnumValueNotSupportedException(notification.Type);
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionOccurred?.Invoke(this, exception);
            }
        }

        public event EventHandler<ListenNotification> MessageReceived;

        public event EventHandler<ListenNotification> ReactionReceived; 

        public event EventHandler<ListenNotification> MessageDelivered;

        public event EventHandler Disconnected;

        public event EventHandler<Exception> ExceptionOccurred;

        public async Task<IList<LocalStorage.Chat>> GetChats(DateTime newerThan)
        {
            GetChatsRequest request = new()
            {
                NewerThan = newerThan
            };
            GetChatsResponse response = await _bffClient.GetStateChats(request, _accessToken);

            List<LocalStorage.Chat> chats = new(capacity: response.Chats.Count);
            foreach (ChatState bffChat in response.Chats)
            {
                long otherUserID = DataUtility.GetOtherUsedID(bffChat.ChatID, UserID);
                LocalStorage.Chat chat = Map.BffChat(bffChat, otherUserID);
                chats.Add(chat);
            }

            return chats;
        }

        public async Task<IList<LocalStorage.Message>> GetHistory(long otherUserID, DateTime olderThan)
        {
            GetHistoryRequest request = new()
            {
                OtherUserID = otherUserID,
                OlderThan = olderThan
            };
            GetHistoryResponse response = await _bffClient.GetHistoryMessages(request, _accessToken);

            List<LocalStorage.Message> messages = new(response.Messages.Count);
            foreach (HistoryMessage bffMessage in response.Messages)
            {
                LocalStorage.Message message = Map.BffMessage(bffMessage);
                messages.Add(message);
            }

            return messages;
        }

        public async Task<long> SendPlainTextMessage(long receiverID, string text)
        {
            SendMessageRequest request = new()
            {
                SenderId = _userID,
                ReceiverId = receiverID,
                DataType = Contracts.Messaging.DataType.PlainText,
                Data = text
            };

            SendMessageResponse response = await _sendClient.SendMessageAsync(request, _grpcMetadata);
            return response.MessageId;
        }

        public async Task React(long messageID, long otherUserID, string reaction)
        {
            ReactRequest request = new()
            {
                MessageId = messageID,
                SenderId = _userID,
                ReceiverId = otherUserID,
                Reaction = reaction
            };
            await _reactionClient.ReactAsync(request, _grpcMetadata);
        }

        public async Task UnReact(long messageID, long otherUserID)
        {
            UnReactRequest request = new()
            {
                MessageId = messageID,
                SenderId = _userID,
                ReceiverId = otherUserID
            };
            await _reactionClient.UnReactAsync(request, _grpcMetadata);
        }
    }
}
