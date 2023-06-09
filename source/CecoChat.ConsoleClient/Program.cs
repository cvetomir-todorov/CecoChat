using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.Interaction;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient;

public static class Program
{
    public static async Task Main()
    {
        string cluster = string.Empty;
        bool chosen = false;
        while (!chosen)
        {
            Console.WriteLine("Choose a cluster: local/Docker https://localhost:31000 (press '1') | Kubernetes https://bff.cecochat.com (press '2')");
            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
            switch (keyInfo.Key)
            {
                case ConsoleKey.D1:
                    cluster = "https://localhost:31000";
                    chosen = true;
                    break;
                case ConsoleKey.D2:
                    cluster = "https://bff.cecochat.com";
                    chosen = true;
                    break;
            }
        }

        Console.Write("Type a username: 'bob' (ID=1), 'alice' (ID=2), 'john' (ID=3), 'peter' (ID=1200): ");
        string username = Console.ReadLine() ?? string.Empty;

        ChatClient client = new(cluster);
        await client.CreateSession(username, password: "not-empty");
        MessageStorage messageStorage = new(client.UserId);
        ProfileStorage profileStorage = new();
        ChangeHandler changeHandler = new(messageStorage);

        client.Disconnected += (_, _) => Console.WriteLine("Disconnected.");
        client.MessageReceived += (_, notification) => changeHandler.AddReceivedMessage(notification);
        client.MessageDelivered += (_, notification) => changeHandler.UpdateDeliveryStatus(notification);
        client.ReactionReceived += (_, notification) => changeHandler.UpdateReaction(notification);
        await client.StartMessaging(CancellationToken.None);

        await RunStateMachine(client, messageStorage, profileStorage);

        await client.DisposeAsync();
        Console.WriteLine("Bye!");
    }

    private static async Task RunStateMachine(ChatClient client, MessageStorage messageStorage, ProfileStorage profileStorage)
    {
        StateContainer states = new(client, messageStorage, profileStorage);
        states.Context.ReloadData = true;
        State currentState = states.AllChats;

        while (currentState != states.Final)
        {
            currentState = await currentState.Execute();
        }
    }
}
