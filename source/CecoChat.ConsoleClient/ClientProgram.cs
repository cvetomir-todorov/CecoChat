using System;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace CecoChat.ConsoleClient
{
    public static class ClientProgram
    {
        public static async Task Main(string[] args)
        {
            Console.Write("Your ID: ");
            long userID = long.Parse(Console.ReadLine() ?? string.Empty);

            using GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:31001");
            Listen.ListenClient listenClient = new(channel);
            Send.SendClient sendClient = new(channel);
            History.HistoryClient historyClient = new(channel);

            AsyncServerStreamingCall<ListenResponse> serverStream = listenClient.Listen(new ListenRequest{UserId = userID});
            Task _ = Task.Run(async () => await ListenForNewMessages(serverStream));

            await ShowHistory(historyClient, userID);
            await Interact(userID, sendClient);

            await channel.ShutdownAsync();
            Console.WriteLine("Bye!");
        }

        private static async Task ListenForNewMessages(AsyncServerStreamingCall<ListenResponse> serverStream)
        {
            try
            {
                while (await serverStream.ResponseStream.MoveNext())
                {
                    Message message = serverStream.ResponseStream.Current.Message;
                    DisplayMessage(message);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static async Task ShowHistory(History.HistoryClient historyClient, long userID)
        {
            GetUserHistoryRequest request = new()
            {
                UserId = userID,
                OlderThan = Timestamp.FromDateTime(DateTime.UtcNow)
            };
            GetUserHistoryResponse response = await historyClient.GetUserHistoryAsync(request);

            Console.WriteLine("{0} messages from history:", response.Messages.Count);
            foreach (Message message in response.Messages)
            {
                DisplayMessage(message);
            }
        }

        private static void DisplayMessage(Message message)
        {
            Console.WriteLine($"[{message.Timestamp.ToDateTime():F}] {message.SenderId}: {message.PlainTextData.Text}");
        }

        private static async Task Interact(long userID, Send.SendClient sendClient)
        {
            CorrelationIDGenerator generator = new();

            while (true)
            {
                Console.WriteLine("Receiver ID:");
                int receiverId = int.Parse(Console.ReadLine() ?? "0");
                if (receiverId <= 0)
                {
                    break;
                }

                Console.WriteLine("Message to {0}:", receiverId);
                string text = Console.ReadLine();

                Message message = new Message
                {
                    MessageId = generator.GenerateCorrelationID(),
                    SenderId = userID,
                    ReceiverId = receiverId,
                    Type = MessageType.PlainText,
                    PlainTextData = new PlainTextData { Text = text }
                };

                try
                {
                    SendMessageResponse response = await sendClient.SendMessageAsync(new SendMessageRequest {Message = message});
                    message.Timestamp = response.MessageTimestamp;
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }
    }
}
