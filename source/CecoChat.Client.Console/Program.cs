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
            client.MessageReceived += (_, notification) => AddReceivedMessage(notification, storage);
            client.MessageDelivered += (_, notification) => UpdateMessageDeliveryStatus(notification, storage);
            client.ReactionReceived += (_, notification) => UpdateReaction(notification, storage);
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

        private static void AddReceivedMessage(ListenNotification notification, MessageStorage storage)
        {
            if (notification.Type != MessageType.Data)
            {
                throw new InvalidOperationException($"Notification {notification} should have type {MessageType.Data}.");
            }

            Message message = new()
            {
                MessageID = notification.MessageId,
                SenderID = notification.SenderId,
                ReceiverID = notification.ReceiverId,
                SequenceNumber = notification.SequenceNumber,
                Status = DeliveryStatus.Processed
            };

            switch (notification.Data.Type)
            {
                case Contracts.Messaging.DataType.PlainText:
                    message.DataType = DataType.PlainText;
                    message.Data = notification.Data.Data;
                    break;
                default:
                    throw new EnumValueNotSupportedException(notification.Data.Type);
            }

            storage.AddMessage(message);
        }

        private static void UpdateMessageDeliveryStatus(ListenNotification notification, MessageStorage storage)
        {
            if (notification.Type != MessageType.Delivery)
            {
                throw new InvalidOperationException($"Notification {notification} should have type {MessageType.Delivery}.");
            }

            Message message = new()
            {
                MessageID = notification.MessageId,
                SenderID = notification.SenderId,
                ReceiverID = notification.ReceiverId,
                SequenceNumber = notification.SequenceNumber,
            };

            switch (notification.Status)
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
                    throw new EnumValueNotSupportedException(notification.Status);
            }

            storage.AcknowledgeMessage(message);
        }

        private static void UpdateReaction(ListenNotification notification, MessageStorage storage)
        {
            if (notification.Type != MessageType.Reaction)
            {
                throw new InvalidOperationException($"Notification {notification} should have type {MessageType.Reaction}.");
            }

            if (!storage.TryGetMessage(notification.SenderId, notification.ReceiverId, notification.MessageId, out Message message))
            {
                // the message is not in the local history
                return;
            }

            if (string.IsNullOrWhiteSpace(notification.Reaction.Reaction))
            {
                if (message.Reactions.ContainsKey(notification.Reaction.ReactorId))
                {
                    message.Reactions.Remove(notification.Reaction.ReactorId);
                }
            }
            else
            {
                message.Reactions.Add(notification.Reaction.ReactorId, notification.Reaction.Reaction);
            }
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
