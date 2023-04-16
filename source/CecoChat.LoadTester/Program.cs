namespace CecoChat.LoadTester;

public static class Program
{
    public static async Task Main()
    {
        Args args = new();
        Console.WriteLine("Start load testing using BFF address {0}", args.BffAddress);
        Console.WriteLine("Clients = {0}, friends = {1}", args.ClientCount, args.FriendCount);
        Console.WriteLine("Sessions = {0}, messages per session = {1}", args.SessionCount, args.MessagesPerSessionCount);

        ClientContext[] contexts = Initializer.Initialize(args);
        await Runner.Run(args, contexts);
        await Analyzer.ShowResults(contexts, args);

        Console.WriteLine("Bye!");
    }
}

public sealed class Args
{
    public string BffAddress { get; init; } = "https://localhost:31000";
    public int ClientCount { get; init; } = 5000;
    public int FriendCount { get; init; } = 20;
    public int SessionCount { get; init; } = 3;
    public int MessagesPerSessionCount { get; init; } = 4;
    public int AnalysisWaitSeconds { get; init; } = 15;
}

public sealed class ClientContext
{
    public ClientContext(long userId, long[] friends, ChatClient client, Random random)
    {
        UserId = userId;
        Client = client;
        Friends = friends;
        Random = random;
    }

    public long UserId { get; }
    public ChatClient Client { get; }
    public long[] Friends { get; }
    public Random Random { get; }
    public bool Success { get; set; }
}
