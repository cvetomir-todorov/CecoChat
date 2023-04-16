namespace CecoChat.LoadTester;

public static class Initializer
{
    public static ClientContext[] Initialize(Args args)
    {
        Random random = new();

        Console.WriteLine("Initializing {0} contexts...", args.ClientCount);
        ClientContext[] contexts = new ClientContext[args.ClientCount];
        for (int i = 0; i < args.ClientCount; ++i)
        {
            long userId = i + 1;
            ChatClient client = new(args.BffAddress);
            long[] friends = GenerateFriends(userId, args.ClientCount, args.FriendCount, random);

            contexts[i] = new ClientContext(userId, friends, client, random);
        }
        Console.WriteLine("Initialized {0} contexts.", args.ClientCount);

        return contexts;
    }

    private static long[] GenerateFriends(long userId, int clientCount, int friendCount, Random random)
    {
        HashSet<long> friends = new(capacity: friendCount);
        while (friends.Count < friendCount)
        {
            // maxValue is exclusive, hence the +1, since user IDs start from 1
            long friend = random.Next(maxValue: clientCount) + 1;
            if (friend != userId)
            {
                friends.Add(friend);
            }
        }

        return friends.ToArray();
    }
}
