using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Client.Console.Interaction;
using CecoChat.Contracts.Messaging;

namespace CecoChat.Client.Console
{
    public static class Program
    {
        public static async Task Main()
        {
            MessagingClient client = new();
            await LogIn(client, profileServer: "https://localhost:31005", connectServer: "https://localhost:31000");
            MessageStorage storage = new(client.UserID);

            client.ExceptionOccurred += ShowException;
            client.MessageReceived += (_, response) => AddReceivedMessage(response, storage);
            client.MessageDelivered += (_, response) => UpdateMessageDeliveryStatus(response, storage);
            client.ListenForMessages(CancellationToken.None);

            await RunStateMachine(client, storage);

            client.ExceptionOccurred -= ShowException;
            client.Dispose();
            System.Console.WriteLine("Bye!");
        }

        private static async Task LogIn(MessagingClient client, string profileServer, string connectServer)
        {
            System.Console.Write("Username bob (ID=1), alice (ID=2), peter (ID=1200): ");
            string username = System.Console.ReadLine() ?? string.Empty;
            await client.Initialize(username, password: "not-empty", profileServer, connectServer);
        }

        private static void ShowException(object _, Exception exception)
        {
            System.Console.WriteLine(exception);
        }

        private static void AddReceivedMessage(ListenResponse response, MessageStorage storage)
        {
            if (response.Type != MessageType.Data)
            {
                throw new InvalidOperationException($"Response {response} should have type {MessageType.Data}.");
            }

            Message message = new()
            {
                MessageID = response.MessageId,
                SenderID = response.SenderId,
                ReceiverID = response.ReceiverId,
                SequenceNumber = response.SequenceNumber,
                Data = response.MessageData.Data,
                Status = DeliveryStatus.Processed
            };

            switch (response.MessageData.Type)
            {
                case Contracts.Messaging.DataType.PlainText:
                    message.DataType = DataType.PlainText;
                    break;
                default:
                    throw new EnumValueNotSupportedException(response.MessageData.Type);
            }

            storage.AddMessage(message);
        }

        private static void UpdateMessageDeliveryStatus(ListenResponse response, MessageStorage storage)
        {
            if (response.Type != MessageType.Delivery)
            {
                throw new InvalidOperationException($"Response {response} should have type {MessageType.Delivery}.");
            }

            Message message = new()
            {
                MessageID = response.MessageId,
                SenderID = response.SenderId,
                ReceiverID = response.ReceiverId,
                SequenceNumber = response.SequenceNumber,
            };

            switch (response.Status)
            {
                case Contracts.Messaging.DeliveryStatus.Lost:
                    message.Status = DeliveryStatus.Lost;
                    break;
                case Contracts.Messaging.DeliveryStatus.Processed:
                    message.Status = DeliveryStatus.Processed;
                    break;
                case Contracts.Messaging.DeliveryStatus.Delivered:
                    message.Status = DeliveryStatus.Delivered;
                    break;
                case Contracts.Messaging.DeliveryStatus.Seen:
                    message.Status = DeliveryStatus.Seen;
                    break;
                default:
                    throw new EnumValueNotSupportedException(response.Status);
            }

            storage.AcknowledgeMessage(message);
        }

        private static async Task RunStateMachine(MessagingClient client, MessageStorage storage)
        {
            StateContainer states = new(client, storage);
            states.Context.ReloadData = true;
            State currentState = states.Users;

            while (currentState != states.Final)
            {
                currentState = await currentState.Execute();
            }
        }
    }
}
