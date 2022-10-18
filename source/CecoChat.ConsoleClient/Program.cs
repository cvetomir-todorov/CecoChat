using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.Interaction;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient;

public static class Program
{
    public static async Task Main()
    {
        Console.Write("Username bob (ID=1), alice (ID=2), john (ID=3), peter (ID=1200): ");
        string username = Console.ReadLine() ?? string.Empty;
        ChatClient client = new("https://localhost:31000");
        await client.CreateSession(username, password: "not-empty");
        MessageStorage storage = new(client.UserID);
        ChangeHandler changeHandler = new(storage);

        client.ExceptionOccurred += ShowException;
        client.MessageReceived += (_, notification) => changeHandler.AddReceivedMessage(notification);
        client.MessageDelivered += (_, notification) => changeHandler.UpdateDeliveryStatus(notification);
        client.ReactionReceived += (_, notification) => changeHandler.UpdateReaction(notification);
        client.StartMessaging(CancellationToken.None);

        await RunStateMachine(client, storage);

        client.ExceptionOccurred -= ShowException;
        client.Dispose();
        Console.WriteLine("Bye!");
    }

    private static void ShowException(object _, Exception exception)
    {
        Console.WriteLine(exception);
    }

    private static async Task RunStateMachine(ChatClient client, MessageStorage storage)
    {
        StateContainer states = new(client, storage);
        states.Context.ReloadData = true;
        State currentState = states.AllChats;

        while (currentState != states.Final)
        {
            currentState = await currentState.Execute();
        }
    }
}