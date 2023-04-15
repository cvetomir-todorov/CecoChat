using System.Diagnostics;
using Refit;

namespace CecoChat.LoadTester;

public static class Runner
{
    public static async Task Run(Args args, ClientContext[] contexts)
    {
        Console.WriteLine("Connecting {0} clients...", contexts.Length);
        Stopwatch stopwatch = Stopwatch.StartNew();
        await ConnectClients(contexts);
        stopwatch.Stop();
        Console.WriteLine("Connected {0} clients for {1:#.00}s.", contexts.Length, stopwatch.Elapsed.TotalSeconds);

        Console.WriteLine("Running {0} clients...", contexts.Length);
        stopwatch.Restart();
        Task[] tasks = new Task[contexts.Length];
        for (int i = 0; i < tasks.Length; ++i)
        {
            int local = i;
            tasks[i] = Task.Run(async () => await RunClient(args, contexts[local]));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();
        Console.WriteLine("Ran {0} clients to completion for {1:#.00}s.", tasks.Length, stopwatch.Elapsed.TotalSeconds);
    }

    private static Task ConnectClients(ClientContext[] contexts)
    {
        return Parallel.ForEachAsync(contexts, new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (context, ct) =>
        {
            try
            {
                await context.Client.Connect(username: $"user{context.UserId}", "not-empty", ct);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Failed ot connect client {0}: {1}", context.UserId, exception);
            }
        });
    }

    private static async Task RunClient(Args args, ClientContext context)
    {
        try
        {
            await DoRunClient(args, context);
            context.Success = true;
        }
        catch (ValidationApiException validationApiException)
        {
            Console.WriteLine("User {0} made an invalid API call against {1}: {2}", context.UserId, validationApiException.Uri, validationApiException.Content!.Title);
            foreach (KeyValuePair<string, string[]> error in validationApiException.Content!.Errors)
            {
                Console.WriteLine("  {0}: {1}", error.Key, string.Join(',', error.Value));
            }
        }
        catch (ApiException apiException)
        {
            Console.WriteLine("User {0} request against {1} resulted in an API error: {2}", context.UserId, apiException.Uri, apiException.Content);
        }
        catch (Exception exception)
        {
            Console.WriteLine("User {0} failed: {1}", context.UserId, exception);
        }
        finally
        {
            await context.Client.DisposeAsync();
        }
    }

    private static async Task DoRunClient(Args args, ClientContext context)
    {
        for (int i = 0; i < args.SessionCount; ++i)
        {
            await Delay(1000, 9000, context);
            await context.Client.OpenAllChats(context.Friends);

            await Delay(2000, 4000, context);
            int friendIndex = context.Random.Next(context.Friends.Length);
            long friendId = context.Friends[friendIndex];
            await context.Client.OpenOneChat(friendId);

            for (int j = 0; j < args.MessagesPerSessionCount; ++j)
            {
                await Delay(3000, 5000, context);
                await context.Client.SendPlainTextMessage(friendId, $"{DateTime.UtcNow}");
            }
        }
    }

    private static Task Delay(int delayMinMs, int delayMaxMs, ClientContext context)
    {
        int delayMs = context.Random.Next(delayMinMs, delayMaxMs);
        return Task.Delay(delayMs);
    }
}
