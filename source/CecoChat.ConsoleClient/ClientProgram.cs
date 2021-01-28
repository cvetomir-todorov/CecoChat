using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Client.Shared;
using CecoChat.Contracts.Client;

namespace CecoChat.ConsoleClient
{
    public static class ClientProgram
    {
        public static async Task Main(string[] args)
        {
            Console.Write("Your ID: ");
            long userID = long.Parse(Console.ReadLine() ?? string.Empty);

            MessagingClient client = new(new MessageIDGenerator());
            client.Initialize(userID, "https://localhost:31001");
            client.MessageReceived += (_, message) => DisplayMessage(message);
            client.ExceptionOccurred += (_, exception) => Console.WriteLine(exception);

            client.ListenForMessages(CancellationToken.None);
            await ShowHistory(client);
            await Interact(client);

            client.Dispose();
            Console.WriteLine("Bye!");
        }

        private static async Task ShowHistory(MessagingClient client)
        {
            IList<Message> messageHistory = await client.GetUserHistory(DateTime.UtcNow);

            Console.WriteLine("{0} messages from history:", messageHistory.Count);
            foreach (Message message in messageHistory)
            {
                DisplayMessage(message);
            }
        }

        private static async Task Interact(MessagingClient client)
        {
            while (true)
            {
                Console.WriteLine("Receiver ID:");
                int receiverID = int.Parse(Console.ReadLine() ?? "0");
                if (receiverID <= 0)
                {
                    break;
                }

                Console.WriteLine("Message to {0}:", receiverID);
                string text = Console.ReadLine();

                try
                {
                    Message message = await client.SendPlainTextMessage(receiverID, text);
                    DisplayMessage(message);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        private static void DisplayMessage(Message message)
        {
            Console.WriteLine($"[{message.Timestamp.ToDateTime():F}] {message.SenderId}->{message.ReceiverId}: {message.PlainTextData.Text}");
        }
    }
}
