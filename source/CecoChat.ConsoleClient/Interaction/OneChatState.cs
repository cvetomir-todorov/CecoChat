using System.Text;
using CecoChat.ConsoleClient.Api;
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
            bool userExists = await Load(Context.UserId);
            if (!userExists)
            {
                Console.WriteLine("User with ID {0} doesn't exist, press ENTER to return to all chats", Context.UserId);
                Console.ReadLine();
                return States.AllChats;
            }
        }

        List<Message> messages = MessageStorage.GetChatMessages(Context.UserId);
        messages.Sort((left, right) => left.MessageId.CompareTo(right.MessageId));

        Console.Clear();
        DisplayUserData();

        ProfilePublic profile = ProfileStorage.GetProfile(Context.UserId);
        Connection? connection = ConnectionStorage.GetConnection(Context.UserId);

        string status = connection != null ? connection.Status.ToString() : ConnectionStatus.NotConnected.ToString();
        Console.WriteLine("Chatting with: {0} | ID={1} | user name={2} | {3} | avatar={4}",
            profile.DisplayName, profile.UserId, profile.UserName, status, profile.AvatarUrl);
        Console.WriteLine("Manage connection - invite/accept/cancel/remove (press 'm') | Return (press 'x')");

        foreach (Message message in messages)
        {
            DisplayMessage(message, profile);
        }
        Console.WriteLine("Write (press 'w') | React (press 'r') | Refresh (press 'f') | Local refresh (press 'l')");

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
        else if (keyInfo.KeyChar == 'm' || keyInfo.KeyChar == 'M')
        {
            Context.ReloadData = true;
            return States.ManageConnection;
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

    private async Task<bool> Load(long otherUserId)
    {
        OneChatScreen screen = await Client.LoadOneChatScreen(otherUserId, messagesOlderThan: DateTime.UtcNow, includeProfile: true, includeConnection: true);
        if (screen.Profile == null)
        {
            return false;
        }

        foreach (Message message in screen.Messages)
        {
            MessageStorage.AddMessage(message);
        }

        ProfileStorage.AddOrUpdateProfile(screen.Profile);

        if (screen.Connection != null)
        {
            ConnectionStorage.UpdateConnection(screen.Connection);
        }

        return true;
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
