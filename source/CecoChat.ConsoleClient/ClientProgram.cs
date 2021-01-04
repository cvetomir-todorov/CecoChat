using System;
using System.Threading.Tasks;
using CecoChat.GrpcContracts;
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

            AsyncServerStreamingCall<GrpcMessage> serverStream = client.Listen(new GrpcListenRequest{UserId = userID});
            Task _ = Task.Run(async () => await Listen(serverStream));

            while (true)
            {
                Console.Write("Receiver ID: ");
                int receiverId = int.Parse(Console.ReadLine() ?? "0");
                if (receiverId <= 0)
                {
                    break;
                }

                Console.Write("Message: ");
                string text = Console.ReadLine();

                GrpcMessage message = new GrpcMessage
                {
                    SenderId = userID,
                    ReceiverId = receiverId,
                    PlainTextData = new GrpcPlainTextData {Text = text}
                };

                try
                {
                    await client.SendMessageAsync(new GrpcSendMessageRequest {Message = message});
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            await channel.ShutdownAsync();
            Console.WriteLine("Bye!");
        }

        private static async Task Listen(AsyncServerStreamingCall<GrpcMessage> serverStream)
        {
            try
            {
                while (await serverStream.ResponseStream.MoveNext())
                {
                    GrpcMessage message = serverStream.ResponseStream.Current;
                    Console.WriteLine($"[{message.SenderId}] {message.PlainTextData.Text}");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
