using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.History;
using CecoChat.Contracts.Messaging;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace CecoChat.Client
{
    public sealed class MessagingClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private long _userID;
        private Metadata _grpcMetadata;
        private GrpcChannel _messagingChannel;
        private GrpcChannel _historyChannel;
        private Listen.ListenClient _listenClient;
        private Send.SendClient _sendClient;
        private History.HistoryClient _historyClient;
        private Reaction.ReactionClient _reactionClient;

        public MessagingClient()
        {
            _httpClient = new HttpClient();
        }

        public void Dispose()
        {
            _httpClient.Dispose();

            _messagingChannel?.ShutdownAsync().Wait();
            _messagingChannel?.Dispose();

            _historyChannel?.ShutdownAsync().Wait();
            _historyChannel?.Dispose();
        }

        public long UserID => _userID;

        public async Task Initialize(string username, string password, string profileServer, string connectServer)
        {
            CreateSessionRequest createSessionRequest = new()
            {
                Username = username,
                Password = password
            };
            CreateSessionResponse createSessionResponse = await CreateSession(createSessionRequest, profileServer);
            ConnectResponse connectResponse = await GetConnectInfo(createSessionResponse.AccessToken, connectServer);
            ProcessAccessToken(createSessionResponse.AccessToken);

            _messagingChannel?.Dispose();
            _messagingChannel = GrpcChannel.ForAddress(connectResponse.MessagingServerAddress);
            _historyChannel?.Dispose();
            _historyChannel = GrpcChannel.ForAddress(connectResponse.HistoryServerAddress);

            _listenClient = new Listen.ListenClient(_messagingChannel);
            _sendClient = new Send.SendClient(_messagingChannel);
            _historyClient = new History.HistoryClient(_historyChannel);
            _reactionClient = new Reaction.ReactionClient(_messagingChannel);
        }

        private async Task<CreateSessionResponse> CreateSession(CreateSessionRequest request, string profileServer)
        {
            UriBuilder builder = new(uri: profileServer);
            builder.Path = "api/session";

            HttpRequestMessage requestMessage = new(HttpMethod.Post, builder.Uri);
            requestMessage.Version = HttpVersion.Version20;
            string requestString = JsonSerializer.Serialize(request);
            requestMessage.Content = new StringContent(requestString);
            requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            responseMessage.EnsureSuccessStatusCode();

            string responseString = await responseMessage.Content.ReadAsStringAsync();
            CreateSessionResponse response = JsonSerializer.Deserialize<CreateSessionResponse>(responseString,
                new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            return response;
        }

        private async Task<ConnectResponse> GetConnectInfo(string accessToken, string connectServer)
        {
            UriBuilder builder = new(uri: connectServer);
            builder.Path = "api/connect";

            HttpRequestMessage requestMessage = new(HttpMethod.Get, builder.Uri);
            requestMessage.Version = HttpVersion.Version20;
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
            responseMessage.EnsureSuccessStatusCode();

            string responseString = await responseMessage.Content.ReadAsStringAsync();
            ConnectResponse response = JsonSerializer.Deserialize<ConnectResponse>(responseString,
                new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            return response;
        }

        private void ProcessAccessToken(string accessToken)
        {
            _grpcMetadata = new();
            _grpcMetadata.Add("Authorization", $"Bearer {accessToken}");

            JwtSecurityToken jwt = new(accessToken);
            _userID = long.Parse(jwt.Subject);
        }

        public void ListenForMessages(CancellationToken ct)
        {
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

        public async Task<IList<HistoryMessage>> GetUserHistory(DateTime olderThan)
        {
            GetUserHistoryRequest request = new()
            {
                OlderThan = Timestamp.FromDateTime(olderThan)
            };
            GetUserHistoryResponse response = await _historyClient.GetUserHistoryAsync(request, _grpcMetadata);
            return response.Messages;
        }

        public async Task<IList<HistoryMessage>> GetHistory(long otherUserID, DateTime olderThan)
        {
            GetHistoryRequest request = new()
            {
                OtherUserId = otherUserID,
                OlderThan = Timestamp.FromDateTime(olderThan)
            };
            GetHistoryResponse response = await _historyClient.GetHistoryAsync(request, _grpcMetadata);
            return response.Messages;
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

        public async Task React(long messageID, long senderID, long receiverID, string reaction)
        {
            ReactRequest request = new()
            {
                MessageId = messageID,
                SenderId = senderID,
                ReceiverId = receiverID,
                Reaction = reaction
            };
            await _reactionClient.ReactAsync(request, _grpcMetadata);
        }

        public async Task UnReact(long messageID, long senderID, long receiverID)
        {
            UnReactRequest request = new()
            {
                MessageId = messageID,
                SenderId = senderID,
                ReceiverId = receiverID
            };
            await _reactionClient.UnReactAsync(request, _grpcMetadata);
        }
    }
}
