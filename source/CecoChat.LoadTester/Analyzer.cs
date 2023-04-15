namespace CecoChat.LoadTester;

public static class Analyzer
{
    public static async Task ShowResults(ClientContext[] contexts, Args args)
    {
        TimeSpan waitInterval = TimeSpan.FromSeconds(args.AnalysisWaitSeconds);
        Console.WriteLine("Waiting {0:#.00}s before analysis...", waitInterval.TotalSeconds);
        await Task.Delay(waitInterval);

        int succeeded = 0;
        int msgSent = 0;
        int msgProcessed = 0;
        int msgReceived = 0;

        foreach (ClientContext context in contexts)
        {
            if (context.Success)
            {
                succeeded++;
            }

            msgSent += context.Client.MessagesSent;
            msgProcessed += context.Client.MessagesProcessed;
            msgReceived += context.Client.MessagesReceived;
        }

        Console.WriteLine("Client succeeded: {0}, failed: {1}", succeeded, contexts.Length - succeeded);
        long msgTotal = args.ClientCount * args.SessionCount * args.MessagesPerSessionCount;
        Console.WriteLine("Messages sent: {0}, processed: {1}, received: {2}, total: {3}", msgSent, msgProcessed, msgReceived, msgTotal);
    }
}
