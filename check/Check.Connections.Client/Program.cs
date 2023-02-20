using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Messaging;
using CommandLine;
using Grpc.Net.Client;

namespace Check.Connections.Client;

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
        Console.WriteLine("Create {0} clients with {1} messages each and server {2}.",
            commandLine.ClientCount, commandLine.MessageCount, commandLine.ServerAddress);

        Stopwatch stopwatch = Stopwatch.StartNew();
        ConnectClients(clients);
        stopwatch.Stop();
        Console.WriteLine("Clients connected for {0:0.####} ms.", stopwatch.Elapsed.TotalMilliseconds);

        stopwatch.Restart();
        Console.WriteLine("Start sending messages.");
        RunAllClients(clients, commandLine);
        stopwatch.Stop();
        Console.WriteLine("Completed {0} out of {1} clients for {2:0.####} ms.",
            _successCount, commandLine.ClientCount, stopwatch.Elapsed.TotalMilliseconds);
    }

    private static List<ClientData> CreateClients(CommandLine commandLine)
    {
        List<ClientData> clients = new(capacity: commandLine.ClientCount);

        for (int i = 0; i < commandLine.ClientCount; ++i)
        {
            Uri address = new(commandLine.ServerAddress);
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
            Send.SendClient sendClient = new(channel);

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

    private static void ConnectClients(List<ClientData> clients)
    {
        Task[] connectTasks = new Task[clients.Count];

        for (int i = 0; i < clients.Count; ++i)
        {
            connectTasks[i] = clients[i].HttpHandler.PreConnect();
        }

        Task.WaitAll(connectTasks);
    }

    private static void RunAllClients(List<ClientData> clients, CommandLine commandLine)
    {
        Task[] runTasks = new Task[clients.Count];
        for (int i = 0; i < clients.Count; ++i)
        {
            runTasks[i] = RunSingleClient(clients[i], commandLine);
        }

        Task.WaitAll(runTasks);
    }

    private static async Task RunSingleClient(ClientData client, CommandLine commandLine)
    {
        try
        {
            for (int i = 0; i < commandLine.MessageCount; ++i)
            {
                SendMessageRequest request = new()
                {
                    ReceiverId = 2,
                    DataType = DataType.PlainText,
                    Data = "dummy"
                };

                await client.SendClient.SendMessageAsync(request);
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