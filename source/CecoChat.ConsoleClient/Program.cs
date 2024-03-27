using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.Interaction;
using CecoChat.ConsoleClient.LocalStorage;
using CecoChat.Messaging.Client;

namespace CecoChat.ConsoleClient;

public static class Program
{
    public static async Task Main()
    {
        string cluster = ChooseCluster();
        ChatClient client = new(cluster);

        Credentials credentials = await RegisterOrChooseCredentials(client);
        await Authenticate(client, credentials);

        MessageStorage messageStorage = new(client.UserId);
        ConnectionStorage connectionStorage = new();
        ChangeHandler changeHandler = new(client.UserId, messageStorage, connectionStorage);
        IMessagingClient messagingClient = new MessagingClient(client.AccessToken, client.MessagingServerAddress);

        messagingClient.Disconnected += (_, _) => Console.WriteLine("Disconnected.");
        messagingClient.PlainTextReceived += (_, notification) => changeHandler.HandlePlainTextMessage(notification);
        messagingClient.FileReceived += (_, notification) => changeHandler.HandleFileMessage(notification);
        messagingClient.MessageDelivered += (_, notification) => changeHandler.UpdateDeliveryStatus(notification);
        messagingClient.ReactionReceived += (_, notification) => changeHandler.HandleReaction(notification);
        messagingClient.ConnectionNotificationReceived += (_, notification) => changeHandler.HandleConnectionChange(notification);
        await messagingClient.Connect(CancellationToken.None);

        await RunStateMachine(client, messagingClient, messageStorage, connectionStorage);

        await messagingClient.DisposeAsync();
        client.Dispose();
        Console.WriteLine("Bye!");
    }

    private static string ChooseCluster()
    {
        string cluster = string.Empty;
        bool chosen = false;
        while (!chosen)
        {
            Console.WriteLine("Choose a cluster: local/Docker https://localhost:31003 (press '1') | Kubernetes https://bff.cecochat.com (press '2')");
            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
            switch (keyInfo.Key)
            {
                case ConsoleKey.D1:
                    cluster = "https://localhost:31003";
                    chosen = true;
                    break;
                case ConsoleKey.D2:
                    cluster = "https://bff.cecochat.com";
                    chosen = true;
                    break;
            }
        }

        return cluster;
    }

    private static async Task<Credentials> RegisterOrChooseCredentials(ChatClient client)
    {
        Credentials credentials = new(string.Empty, string.Empty);
        bool chosen = false;
        while (!chosen)
        {
            Console.WriteLine("Choose: authenticate (press '1') | register (press '2')");
            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
            switch (keyInfo.Key)
            {
                case ConsoleKey.D1:
                    credentials = ChooseCredentials();
                    chosen = true;
                    break;
                case ConsoleKey.D2:
                    credentials = await Register(client);
                    chosen = true;
                    break;
            }
        }

        return credentials;
    }

    private static Credentials ChooseCredentials()
    {
        Console.WriteLine("Console users (if seeded): 'bobby' (ID=1), 'alice' (ID=2), 'john' (ID=3), 'peter' (ID=1200)");

        Console.Write("User: ");
        string userName = Console.ReadLine() ?? string.Empty;

        Console.Write("Pass: ");
        string password = Console.ReadLine() ?? string.Empty;
        if (password.Length == 0)
        {
            password = "secret12";
        }

        return new Credentials(userName, password);
    }

    private static async Task<Credentials> Register(ChatClient client)
    {
        Console.Write("Type a unique user name: ");
        string userName = Console.ReadLine() ?? string.Empty;

        Console.Write("Type a password: ");
        string password = Console.ReadLine() ?? string.Empty;

        Console.Write("Type a display name: ");
        string displayName = Console.ReadLine() ?? string.Empty;

        Console.Write("Type a phone: ");
        string phone = Console.ReadLine() ?? string.Empty;

        Console.Write("Type an email: ");
        string email = Console.ReadLine() ?? string.Empty;

        ClientResponse response = await client.Register(userName, password, displayName, phone, email);
        if (response.Success)
        {
            return new Credentials(userName, password);
        }
        else
        {
            foreach (string error in response.Errors)
            {
                Console.WriteLine(error);
            }

            throw new InvalidOperationException("Unsuccessful registration.");
        }
    }

    private static async Task Authenticate(ChatClient client, Credentials credentials)
    {
        ClientResponse response = await client.CreateSession(credentials.UserName, credentials.Password);
        if (!response.Success)
        {
            foreach (string error in response.Errors)
            {
                Console.WriteLine(error);
            }
            throw new InvalidOperationException("Unsuccessful authentication.");
        }
    }

    private sealed record Credentials(string UserName, string Password);

    private static async Task RunStateMachine(ChatClient client, IMessagingClient messagingClient, MessageStorage messageStorage, ConnectionStorage connectionStorage)
    {
        ProfileStorage profileStorage = new();
        FileStorage fileStorage = new();
        StateContainer states = new(client, messagingClient, messageStorage, connectionStorage, profileStorage, fileStorage);

        states.Context.ReloadData = true;
        State currentState = states.AllChats;

        while (currentState != states.Final)
        {
            currentState = await currentState.Execute();
        }
    }
}
