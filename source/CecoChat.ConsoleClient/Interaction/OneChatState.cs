using System.Text;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class OneChatState : State
{
    public OneChatState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        if (Context.ReloadData)
        {
            await GetHistory(Context.UserId);
        }

        List<Message> messages = Storage.GetChatMessages(Context.UserId);
        messages.Sort((left, right) => left.MessageId.CompareTo(right.MessageId));

        Console.Clear();
        DisplayUserData();
        ProfilePublic profile = await Client.GetPublicProfile(Context.UserId);
        Console.WriteLine("Chatting with: {0} | ID={1} | user name={2} | avatar={3}", profile.DisplayName, profile.UserId, profile.UserName, profile.AvatarUrl);

        foreach (Message message in messages)
        {
            DisplayMessage(message, profile);
        }
        Console.WriteLine("Write (press 'w') | React (press 'r') | Refresh (press 'f') | Local refresh (press 'l') | Return (press 'x')");

        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
        if (keyInfo.KeyChar == 'w' || keyInfo.KeyChar == 'W')
        {
            Context.ReloadData = false;
            return States.SendMessage;
        }
        else if (keyInfo.KeyChar == 'r' || keyInfo.KeyChar == 'R')
        {
            Context.ReloadData = false;
            return States.React;
        }
        else if (keyInfo.KeyChar == 'f' || keyInfo.KeyChar == 'F')
        {
            Context.ReloadData = true;
            return States.OneChat;
        }
        else if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
        {
            Context.ReloadData = true;
            return States.AllChats;
        }
        else
        {
            // includes local refresh
            Context.ReloadData = false;
            return States.OneChat;
        }
    }

    private async Task GetHistory(long userId)
    {
        IList<Message> history = await Client.GetHistory(userId, DateTime.UtcNow);
        foreach (Message message in history)
        {
            Storage.AddMessage(message);
        }
    }

    private void DisplayMessage(Message message, ProfilePublic profilePublic)
    {
        string sender = message.SenderId == Client.UserId ? "You" : profilePublic.DisplayName;
        string reactions = string.Empty;
        if (message.Reactions.Count > 0)
        {
            StringBuilder reactionsBuilder = new();
            foreach (KeyValuePair<long, string> pair in message.Reactions)
            {
                reactionsBuilder.AppendFormat(" {0}={1}", pair.Key, pair.Value);
            }

            reactions = reactionsBuilder.ToString();
        }

        Console.WriteLine("[{0:F}] {1}: {2} (ID: {3} |{4} reaction(s):{5})",
            message.MessageId.ToTimestamp(), sender, message.Data, message.MessageId, message.Reactions.Count, reactions);
    }
}
