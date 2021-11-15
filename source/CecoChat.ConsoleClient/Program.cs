using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Client;
using CecoChat.ConsoleClient.Interaction;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient
{
    public static class Program
    {
        public static async Task Main()
        {
            MessagingClient client = new();
            await LogIn(client, profileServer: "https://localhost:31005", connectServer: "https://localhost:31000");
            MessageStorage storage = new(client.UserID);
            ChangeHandler changeHandler = new(storage);

            client.ExceptionOccurred += ShowException;
            client.MessageReceived += (_, notification) => changeHandler.AddReceivedMessage(notification);
            client.MessageDelivered += (_, notification) => changeHandler.UpdateDeliveryStatus(notification);
            client.ReactionReceived += (_, notification) => changeHandler.UpdateReaction(notification);
            client.ListenForMessages(CancellationToken.None);

            await RunStateMachine(client, storage);

            client.ExceptionOccurred -= ShowException;
            client.Dispose();
            Console.WriteLine("Bye!");
        }

        private static async Task LogIn(MessagingClient client, string profileServer, string connectServer)
        {
            Console.Write("Username bob (ID=1), alice (ID=2), peter (ID=1200): ");
            string username = Console.ReadLine() ?? string.Empty;
            await client.Initialize(username, password: "not-empty", profileServer, connectServer);
        }

        private static void ShowException(object _, Exception exception)
        {
            Console.WriteLine(exception);
        }

        private static async Task RunStateMachine(MessagingClient client, MessageStorage storage)
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
}
