using System;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using Grpc.Core;
using Grpc.Net.Client;

namespace CecoChat.ConsoleClient
{
    public static class ClientProgram
    {
        public static async Task Main(string[] args)
        {
            Console.Write("Your ID: ");
            int userID = int.Parse(Console.ReadLine() ?? string.Empty);

            using GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:5001");
            Chat.ChatClient client = new Chat.ChatClient(channel);

            AsyncServerStreamingCall<ListenResponse> serverStream = client.Listen(new ListenRequest{UserId = userID});
            Task _ = Task.Run(async () => await Listen(serverStream));

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
                    SenderId = userID,
                    ReceiverId = receiverId,
                    Type = MessageType.PlainText,
                    PlainTextData = new PlainTextData {Text = text}
                };

                try
                {
                    await client.SendMessageAsync(new SendMessageRequest {Message = message});
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            await channel.ShutdownAsync();
            Console.WriteLine("Bye!");
        }

        private static async Task Listen(AsyncServerStreamingCall<ListenResponse> serverStream)
        {
            try
            {
                while (await serverStream.ResponseStream.MoveNext())
                {
                    Message message = serverStream.ResponseStream.Current.Message;
                    Console.WriteLine($"[{message.Timestamp.ToDateTime():F}] {message.SenderId}: {message.PlainTextData.Text}");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
