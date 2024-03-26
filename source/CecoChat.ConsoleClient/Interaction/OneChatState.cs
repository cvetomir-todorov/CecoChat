using System.Text;
using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;
using Common;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class OneChatState : State
{
    public OneChatState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();
        DisplaySplitter();

        if (Context.ReloadData)
        {
            bool userExists = await Load(Context.UserId);
            if (!userExists)
            {
                Console.WriteLine("User with ID {0} doesn't exist, press ENTER to return to all chats", Context.UserId);
                Console.ReadLine();

                Context.ReloadData = true;
                return States.AllChats;
            }
        }

        List<Message> messages = MessageStorage.GetChatMessages(Context.UserId);
        messages.Sort((left, right) => left.MessageId.CompareTo(right.MessageId));

        ProfilePublic profile = ProfileStorage.GetProfile(Context.UserId);
        Connection? connection = ConnectionStorage.GetConnection(Context.UserId);

        string status = connection != null ? connection.Status.ToString() : ConnectionStatus.NotConnected.ToString();
        Console.WriteLine("Chatting with: {0} | ID={1} | user name={2} | {3} | avatar={4}",
            profile.DisplayName, profile.UserId, profile.UserName, status, profile.AvatarUrl);
        Console.WriteLine("Manage connection - invite/accept/cancel/remove (press 'm')");
        Console.WriteLine("Refresh (press 'f') | Local refresh (press 'l') | Return (press 'x')");
        DisplaySplitter();

        foreach (Message message in messages)
        {
            DisplayMessage(message, profile);
        }

        DisplaySplitter();
        Console.WriteLine("Write (press 'w') | React (press 'r') | Send file (press 'i') | Download sent file (press 'd')");

        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
        return HandleKey(keyInfo);
    }

    private State HandleKey(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.KeyChar == 'm')
        {
            Context.ReloadData = true;
            return States.ManageConnection;
        }
        else if (keyInfo.KeyChar == 'f')
        {
            Context.ReloadData = true;
            return States.OneChat;
        }
        else if (keyInfo.KeyChar == 'x')
        {
            Context.ReloadData = true;
            return States.AllChats;
        }
        else if (keyInfo.KeyChar == 'w')
        {
            Context.ReloadData = false;
            return States.SendMessage;
        }
        else if (keyInfo.KeyChar == 'r')
        {
            Context.ReloadData = false;
            return States.React;
        }
        else if (keyInfo.KeyChar == 'i')
        {
            Context.ReloadData = false;
            return States.SendFile;
        }
        else if (keyInfo.KeyChar == 'd')
        {
            Context.ReloadData = false;
            return States.DownloadSentFile;
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

        string file = string.Empty;
        if (message.Type == MessageType.File)
        {
            string prefix = message.Text.Length > 0 ? " " : string.Empty;
            file = $"{prefix}(file: {message.FileBucket}/{message.FilePath})";
        }

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

        Console.WriteLine("[{0:F}] {1}: {2}{3} (ID: {4} |{5} reaction(s):{6})",
            message.MessageId.ToTimestamp(), sender, message.Text, file, message.MessageId, message.Reactions.Count, reactions);
    }
}
