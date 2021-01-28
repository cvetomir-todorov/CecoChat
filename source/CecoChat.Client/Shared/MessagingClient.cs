using System;
using System.Collections.Generic;
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
        private long _userID;
        private string _server;
        private GrpcChannel _channel;
        private Listen.ListenClient _listenClient;
        private History.HistoryClient _historyClient;
        private Send.SendClient _sendClient;

        public MessagingClient(MessageIDGenerator messageIDGenerator)
        {
            _messageIDGenerator = messageIDGenerator;
        }

        public void Dispose()
        {
            _channel?.ShutdownAsync().Wait();
            _channel?.Dispose();
        }

        public long UserID => _userID;

        public string Server => _server;

        public void Initialize(long userID, string server)
        {
            _userID = userID;
            _server = server;

            _channel = GrpcChannel.ForAddress(server);

            _listenClient = new Listen.ListenClient(_channel);
            _historyClient = new History.HistoryClient(_channel);
            _sendClient = new Send.SendClient(_channel);
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
                    Message message = serverStream.ResponseStream.Current.Message;
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception exception)
            {
                ExceptionOccurred?.Invoke(this, exception);
            }
        }

        public event EventHandler<Message> MessageReceived;

        public event EventHandler<Exception> ExceptionOccurred;

        public async Task<IList<Message>> GetUserHistory(DateTime olderThan)
        {
            GetUserHistoryRequest request = new()
            {
                UserId = _userID,
                OlderThan = Timestamp.FromDateTime(olderThan)
            };
            GetUserHistoryResponse response = await _historyClient.GetUserHistoryAsync(request);
            return response.Messages;
        }

        public async Task<IList<Message>> GetDialogHistory(long otherUserID, DateTime olderThan)
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

        public async Task<Message> SendPlainTextMessage(long receiverID, string text)
        {
            string messageID = _messageIDGenerator.GenerateMessageID();
            Message message = new()
            {
                MessageId = messageID,
                SenderId = _userID,
                ReceiverId = receiverID,
                Type = MessageType.PlainText,
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
