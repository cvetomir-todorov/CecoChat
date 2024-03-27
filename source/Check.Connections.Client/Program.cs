using System.Diagnostics;
using CecoChat.Messaging.Contracts;
using CommandLine;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

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
        Console.WriteLine("Created {0} clients with {1} messages each and server {2}.", clients.Count, commandLine.MessageCount, commandLine.ServerAddress);

        Console.WriteLine("Start connecting clients...");
        Stopwatch stopwatch = Stopwatch.StartNew();
        ConnectClients(clients);
        stopwatch.Stop();
        Console.WriteLine("{0} client(s) connected to server {1} for {2:0.####}s.", clients.Count, commandLine.ServerAddress, stopwatch.Elapsed.TotalSeconds);

        Console.WriteLine("Start sending messages...");
        stopwatch.Restart();
        RunAllClients(clients, commandLine);
        stopwatch.Stop();
        Console.WriteLine("Completed {0} out of {1} clients for {2:0.####}s.", _successCount, commandLine.ClientCount, stopwatch.Elapsed.TotalSeconds);
    }

    private static List<ClientData> CreateClients(CommandLine commandLine)
    {
        List<ClientData> clients = new(capacity: commandLine.ClientCount);

        for (int i = 0; i < commandLine.ClientCount; ++i)
        {
            UriBuilder addressBuilder = new(commandLine.ServerAddress);
            addressBuilder.Path = "/chat";

            HubConnection connection = new HubConnectionBuilder()
                .WithUrl(addressBuilder.Uri)
                .AddMessagePackProtocol()
                .Build();

            ClientData client = new(id: i, connection);
            clients.Add(client);
        }

        return clients;
    }

    private static void ConnectClients(List<ClientData> clients)
    {
        Task[] connectTasks = new Task[clients.Count];

        for (int i = 0; i < clients.Count; ++i)
        {
            connectTasks[i] = clients[i].Connection.StartAsync();
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
                SendPlainTextRequest request = new()
                {
                    ReceiverId = 2,
                    Text= "dummy"
                };

                await client.Connection.InvokeAsync(nameof(IChatHub.SendPlainText), request);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Interlocked.Increment(ref _successCount);
        }
        catch (Exception exception)
        {
            Console.WriteLine("Client {0} resulted in error: {1}", client.Id, exception.Message);
        }
        finally
        {
            await client.Connection.DisposeAsync();
        }
    }

    private sealed class ClientData
    {
        public ClientData(int id, HubConnection connection)
        {
            Id = id;
            Connection = connection;
        }

        public int Id { get; init; }
        public HubConnection Connection { get; init; }
    }
}
