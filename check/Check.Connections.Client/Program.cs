using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using CommandLine;
using Grpc.Net.Client;

namespace Check.Connections.Client
{
    public static class Program
    {
        private static int _successCount;

        public static void Main(string[] args)
        {
            Parser.Default
                .ParseArguments<CommandLine>(args)
                .WithParsed(Start);
        }

        private static void Start(CommandLine commandLine)
        {
            List<ClientData> clients = CreateClients(commandLine);

            Console.WriteLine("Start {0} clients with {1} messages each and server {2}.",
                commandLine.ClientCount, commandLine.MessageCount, commandLine.ServerAddress);

            Stopwatch stopwatch = Stopwatch.StartNew();
            Task[] tasks = new Task[commandLine.ClientCount];

            for (int i = 0; i < commandLine.ClientCount; ++i)
            {
                // do not connect all clients immediately
                Thread.Sleep(TimeSpan.FromMilliseconds(0.5));
                tasks[i] = RunSingleClient(clients[i], commandLine);
            }
            Console.WriteLine("Clients connected.");

            Task.WaitAll(tasks);
            stopwatch.Stop();
            Console.WriteLine("Completed {0} out of {1} for {2:0.####} ms.",
                _successCount, commandLine.ClientCount, stopwatch.Elapsed.TotalMilliseconds);
        }

        private static List<ClientData> CreateClients(CommandLine commandLine)
        {
            List<ClientData> clients = new(capacity: commandLine.ClientCount);

            for (int i = 0; i < commandLine.ClientCount; ++i)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(commandLine.ServerAddress);
                Send.SendClient sendClient = new Send.SendClient(channel);

                ClientData client = new()
                {
                    ID = i,
                    Channel = channel,
                    SendClient = sendClient
                };

                clients.Add(client);
            }

            return clients;
        }

        private static async Task RunSingleClient(ClientData client, CommandLine commandLine)
        {
            try
            {
                for (int i = 0; i < commandLine.MessageCount; ++i)
                {
                    ClientMessage message = new()
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        SenderId = 1,
                        ReceiverId = 2,
                        Type = ClientMessageType.PlainText,
                        PlainTextData = new PlainTextData
                        {
                            Text = "dummy"
                        }
                    };
                    SendMessageRequest request = new()
                    {
                        Message = message
                    };

                    SendMessageResponse response = await client.SendClient.SendMessageAsync(request);
                    message.Timestamp = response.MessageTimestamp;

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                Interlocked.Increment(ref _successCount);
                await client.Channel.ShutdownAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Client {0} resulted in error: {1}", client.ID, exception.Message);
            }
        }

        private sealed class ClientData
        {
            public int ID { get; init; }
            public GrpcChannel Channel { get; init; }
            public Send.SendClient SendClient { get; init; }
        }
    }
}
