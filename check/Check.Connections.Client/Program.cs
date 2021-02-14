using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
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
        private static SemaphoreSlim _clientsConnected;

        public static void Main(string[] args)
        {
            Parser.Default
                .ParseArguments<CommandLine>(args)
                .WithParsed(Start);
        }

        private static void Start(CommandLine commandLine)
        {
            List<ClientData> clients = CreateClients(commandLine);

            Console.WriteLine("Create {0} clients with {1} messages each and server {2}.",
                commandLine.ClientCount, commandLine.MessageCount, commandLine.ServerAddress);

            _clientsConnected = new SemaphoreSlim(initialCount: 0, maxCount: commandLine.ClientCount);

            Stopwatch stopwatch = Stopwatch.StartNew();
            Task[] tasks = new Task[commandLine.ClientCount];
            for (int i = 0; i < commandLine.ClientCount; ++i)
            {
                tasks[i] = RunSingleClient(clients[i], commandLine);
            }
            stopwatch.Stop();
            Console.WriteLine("Clients connected for {0:0.####} ms.", stopwatch.Elapsed.TotalMilliseconds);

            stopwatch.Restart();
            _clientsConnected.Release(commandLine.ClientCount);
            Console.WriteLine("Start sending messages.");
            Task.WaitAll(tasks);

            stopwatch.Stop();
            Console.WriteLine("Completed {0} out of {1} clients for {2:0.####} ms.",
                _successCount, commandLine.ClientCount, stopwatch.Elapsed.TotalMilliseconds);
        }

        private static List<ClientData> CreateClients(CommandLine commandLine)
        {
            List<ClientData> clients = new(capacity: commandLine.ClientCount);

            for (int i = 0; i < commandLine.ClientCount; ++i)
            {
                Uri address = new Uri(commandLine.ServerAddress);
                EndPoint endPoint = new DnsEndPoint(address.Host, address.Port);
                PreConnectedHttpHandler preConnectedHttpHandler = new(endPoint);
                SocketsHttpHandler socketsHttpHandler = new()
                {
                    ConnectCallback = preConnectedHttpHandler.GetConnectedStream
                };

                GrpcChannel channel = GrpcChannel.ForAddress(commandLine.ServerAddress, new GrpcChannelOptions
                {
                    HttpHandler = socketsHttpHandler
                });
                Send.SendClient sendClient = new Send.SendClient(channel);

                ClientData client = new()
                {
                    ID = i,
                    HttpHandler = preConnectedHttpHandler,
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
                await client.HttpHandler.PreConnect();
                await _clientsConnected.WaitAsync();

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
            public PreConnectedHttpHandler HttpHandler { get; init; }
            public GrpcChannel Channel { get; init; }
            public Send.SendClient SendClient { get; init; }
        }
    }
}
