using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Client.Shared;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Console
{
    public static class Program
    {
        public static async Task Main()
        {
            System.Console.Write("Username: ");
            string username = System.Console.ReadLine() ?? string.Empty;

            System.Console.Write("Password: ");
            string password = System.Console.ReadLine() ?? string.Empty;

            MessagingClient client = new();
            await client.Initialize(username, password, profileServer: "https://localhost:31005", connectServer: "https://localhost:31000");
            client.MessageReceived += (_, response) => DisplayMessage(response.Message, response.SequenceNumber);
            client.MessageAcknowledged += (_, response) => DisplayAck(response);
            client.ExceptionOccurred += (_, exception) => System.Console.WriteLine(exception);

            client.ListenForMessages(CancellationToken.None);
            await ShowHistory(client);
            await Interact(client);

            client.Dispose();
            System.Console.WriteLine("Bye!");
        }

        private static async Task ShowHistory(MessagingClient client)
        {
            IList<ClientMessage> messageHistory = await client.GetUserHistory(DateTime.UtcNow);

            System.Console.WriteLine("{0} messages from history:", messageHistory.Count);
            foreach (ClientMessage message in messageHistory)
            {
                DisplayMessage(message);
            }
        }

        private static async Task Interact(MessagingClient client)
        {
            while (true)
            {
                System.Console.WriteLine("Receiver ID:");
                int receiverID = int.Parse(System.Console.ReadLine() ?? "0");
                if (receiverID <= 0)
                {
                    break;
                }

                System.Console.WriteLine("Message to {0}:", receiverID);
                string text = System.Console.ReadLine();

                try
                {
                    ClientMessage message = await client.SendPlainTextMessage(receiverID, text);
                    DisplayMessage(message);
                }
                catch (Exception exception)
                {
                    System.Console.WriteLine(exception);
                }
            }
        }

        private static void DisplayMessage(ClientMessage message, int? sequenceNumber = null)
        {
            string sequenceString = sequenceNumber.HasValue ? $"[{sequenceNumber}]" : string.Empty;
            System.Console.WriteLine("{0}[{1:F}] {2}->{3}: {4}",
                sequenceString, message.Timestamp.ToDateTime(), message.SenderId, message.ReceiverId, message.Text);
        }

        private static void DisplayAck(ListenResponse response)
        {
            ClientMessage message = response.Message;
            System.Console.WriteLine("[{0}][{1:F}] {2}->{3}: {4}",
                response.SequenceNumber, message.Timestamp.ToDateTime(), message.SenderId, message.ReceiverId, message.AckType);
        }
    }
}
