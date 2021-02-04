using System;
using System.Collections.Generic;
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
        private readonly MessageIDGenerator _messageIDGenerator;
        private readonly HttpClient _httpClient;
        private long _userID;
        private string _messagingServer;
        private string _historyServer;
        private GrpcChannel _messagingChannel;
        private GrpcChannel _historyChannel;
        private Listen.ListenClient _listenClient;
        private Send.SendClient _sendClient;
        private History.HistoryClient _historyClient;

        public MessagingClient(MessageIDGenerator messageIDGenerator)
        {
            _messageIDGenerator = messageIDGenerator;
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

        public async Task Initialize(long userID, string connectServer)
        {
            _userID = userID;
            CreateSessionRequest request = new() {UserID = userID};
            CreateSessionResponse response = await CreateSession(request, connectServer);

            _messagingServer = response.MessagingServerAddress;
            _historyServer = response.HistoryServerAddress;

            _messagingChannel = GrpcChannel.ForAddress(_messagingServer);
            _historyChannel = GrpcChannel.ForAddress(_historyServer);

            _listenClient = new Listen.ListenClient(_messagingChannel);
            _sendClient = new Send.SendClient(_messagingChannel);
            _historyClient = new History.HistoryClient(_historyChannel);
        }

        private async Task<CreateSessionResponse> CreateSession(CreateSessionRequest request, string connectServer)
        {
            UriBuilder builder = new(uri: connectServer);
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

        public void ListenForMessages(CancellationToken ct)
        {
            AsyncServerStreamingCall<ListenResponse> serverStream = _listenClient.Listen(new ListenRequest {UserId = _userID});
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
                    ClientMessage message = serverStream.ResponseStream.Current.Message;
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception exception)
            {
                ExceptionOccurred?.Invoke(this, exception);
            }
        }

        public event EventHandler<ClientMessage> MessageReceived;

        public event EventHandler<Exception> ExceptionOccurred;

        public async Task<IList<ClientMessage>> GetUserHistory(DateTime olderThan)
        {
            GetUserHistoryRequest request = new()
            {
                UserId = _userID,
                OlderThan = Timestamp.FromDateTime(olderThan)
            };
            GetUserHistoryResponse response = await _historyClient.GetUserHistoryAsync(request);
            return response.Messages;
        }

        public async Task<IList<ClientMessage>> GetDialogHistory(long otherUserID, DateTime olderThan)
        {
            GetDialogHistoryRequest request = new()
            {
                UserId = _userID,
                OtherUserId = otherUserID,
                OlderThan = Timestamp.FromDateTime(olderThan)
            };
            GetDialogHistoryResponse response = await _historyClient.GetDialogHistoryAsync(request);
            return response.Messages;
        }

        public async Task<ClientMessage> SendPlainTextMessage(long receiverID, string text)
        {
            string messageID = _messageIDGenerator.GenerateMessageID();
            ClientMessage message = new()
            {
                MessageId = messageID,
                SenderId = _userID,
                ReceiverId = receiverID,
                Type = ClientMessageType.PlainText,
                PlainTextData = new PlainTextData
                {
                    Text = text
                }
            };

            SendMessageResponse response = await _sendClient.SendMessageAsync(new SendMessageRequest {Message = message});
            message.Timestamp = response.MessageTimestamp;
            return message;
        }
    }
}
