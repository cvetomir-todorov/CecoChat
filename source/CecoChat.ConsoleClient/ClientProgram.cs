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
            Chat.ChatClient client = new(channel);
            History.HistoryClient history = new(channel);

            AsyncServerStreamingCall<ListenResponse> serverStream = client.Listen(new ListenRequest{UserId = userID});
            Task _ = Task.Run(async () => await ListenForNewMessages(serverStream));

            await ShowHistory(history, userID);
            await Interact(userID, client);

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

        private static async Task ShowHistory(History.HistoryClient client, long userID)
        {
            GetHistoryRequest request = new()
            {
                UserId = userID,
                NewerThan = Timestamp.FromDateTime(DateTime.UtcNow.AddYears(-1))
            };
            GetHistoryResponse response = await client.GetHistoryAsync(request);

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

        private static async Task Interact(long userID, Chat.ChatClient client)
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
                    SendMessageResponse response = await client.SendMessageAsync(new SendMessageRequest {Message = message});
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
