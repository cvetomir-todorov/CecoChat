using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class ReactState : State
{
    public ReactState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Write("Choose message ID ('0' to exit): ");
        string? messageIDString = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(messageIDString) ||
            !long.TryParse(messageIDString, out long messageID) ||
            messageID == 0)
        {
            Context.ReloadData = false;
            return States.OneChat;
        }

        if (!Storage.TryGetMessage(Client.UserID, Context.UserID, messageID, out Message? message))
        {
            Context.ReloadData = false;
            return States.OneChat;
        }

        if (message.Reactions.ContainsKey(Client.UserID))
        {
            await Client.UnReact(messageID, Context.UserID);
            message.Reactions.Remove(Client.UserID);
        }
        else
        {
            string reaction = Reactions.ThumbsUp;
            await Client.React(messageID, Context.UserID, reaction);
            message.Reactions.Add(Client.UserID, reaction);
        }

        Context.ReloadData = false;
        return States.OneChat;
    }

    private static class Reactions
    {
        public static readonly string ThumbsUp = "\\u1F44D";
    }
}