using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace CecoChat.Client.Shared
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
            ListenRequest request = new();
            AsyncServerStreamingCall<ListenResponse> serverStream = _listenClient.Listen(request, _grpcMetadata, cancellationToken: ct);
            Task.Factory.StartNew(
                async () => await ListenForNewMessages(serverStream, ct),
                TaskCreationOptions.LongRunning);
        }

        private async Task ListenForNewMessages(AsyncServerStreamingCall<ListenResponse> serverStream, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && await serverStream.ResponseStream.MoveNext())
                {
                    ListenResponse response = serverStream.ResponseStream.Current;
                    if (response.Message.Type == ClientMessageType.Disconnect)
                    {
                        Disconnected?.Invoke(this, EventArgs.Empty);
                    }
                    else if (response.Message.Type == ClientMessageType.Ack)
                    {
                        MessageAcknowledged?.Invoke(this, response);
                    }
                    else
                    {
                        MessageReceived?.Invoke(this, response);
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionOccurred?.Invoke(this, exception);
            }
        }

        public event EventHandler<ListenResponse> MessageReceived;

        public event EventHandler<ListenResponse> MessageAcknowledged;

        public event EventHandler Disconnected;

        public event EventHandler<Exception> ExceptionOccurred;

        public async Task<IList<ClientMessage>> GetUserHistory(DateTime olderThan)
        {
            GetUserHistoryRequest request = new()
            {
                OlderThan = Timestamp.FromDateTime(olderThan)
            };
            GetUserHistoryResponse response = await _historyClient.GetUserHistoryAsync(request, _grpcMetadata);
            return response.Messages;
        }

        public async Task<IList<ClientMessage>> GetDialogHistory(long otherUserID, DateTime olderThan)
        {
            GetDialogHistoryRequest request = new()
            {
                OtherUserId = otherUserID,
                OlderThan = Timestamp.FromDateTime(olderThan)
            };
            GetDialogHistoryResponse response = await _historyClient.GetDialogHistoryAsync(request, _grpcMetadata);
            return response.Messages;
        }

        public async Task<ClientMessage> SendPlainTextMessage(long receiverID, string text)
        {
            ClientMessage message = new()
            {
                SenderId = _userID,
                ReceiverId = receiverID,
                Type = ClientMessageType.PlainText,
                Text = text
            };

            SendMessageRequest request = new()
            {
                Message = message
            };
            SendMessageResponse response = await _sendClient.SendMessageAsync(request, _grpcMetadata);

            message.MessageId = response.MessageId;
            return message;
        }
    }
}
