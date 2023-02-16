using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class ReactState : State
{
    public ReactState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Write("Choose message ID ('0' to exit): ");
        string? messageIdString = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(messageIdString) ||
            !long.TryParse(messageIdString, out long messageId) ||
            messageId == 0)
        {
            Context.ReloadData = false;
            return States.OneChat;
        }

        if (!Storage.TryGetMessage(Client.UserId, Context.UserId, messageId, out Message? message))
        {
            Context.ReloadData = false;
            return States.OneChat;
        }

        if (message.Reactions.ContainsKey(Client.UserId))
        {
            await Client.UnReact(messageId, message.SenderId, message.ReceiverId);
            message.Reactions.Remove(Client.UserId);
        }
        else
        {
            string reaction = Reactions.ThumbsUp;
            await Client.React(messageId, message.SenderId, message.ReceiverId, reaction);
            message.Reactions.Add(Client.UserId, reaction);
        }

        Context.ReloadData = false;
        return States.OneChat;
    }

    private static class Reactions
    {
        public static readonly string ThumbsUp = "\\u1F44D";
    }
}