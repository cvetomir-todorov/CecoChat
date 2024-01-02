using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class AllChatsState : State
{
    private DateTime _lastKnownChatState;

    public AllChatsState(StateContainer states) : base(states)
    {
        _lastKnownChatState = Snowflake.Epoch;
    }

    public override async Task<State> Execute()
    {
        if (Context.ReloadData)
        {
            await Load();
        }

        Console.Clear();
        DisplayUserData();
        Console.WriteLine("Chat with a user (press '0'...'9') | New (press 'n') | Refresh (press 'f') | Local refresh (press 'l')");
        Console.WriteLine("Change password (press 'p') | Edit profile (press 'e') | Upload file (press 'u')");
        Console.WriteLine("Exit (press 'x')");
        DisplaySplitter();
        DisplayUserFiles();
        DisplaySplitter();
        List<long> userIds = DisplayUsers();

        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
        if (char.IsNumber(keyInfo.KeyChar))
        {
            return ProcessNumberKey(keyInfo, userIds);
        }
        else if (keyInfo.KeyChar == 'n')
        {
            Context.ReloadData = false;
            return States.FindUser;
        }
        else if (keyInfo.KeyChar == 'f')
        {
            Context.ReloadData = true;
            return States.AllChats;
        }
        else if (keyInfo.KeyChar == 'p')
        {
            return States.ChangePassword;
        }
        else if (keyInfo.KeyChar == 'e')
        {
            return States.EditProfile;
        }
        else if (keyInfo.KeyChar == 'u')
        {
            return States.UploadFile;
        }
        else if (keyInfo.KeyChar == 'x')
        {
            return States.Final;
        }
        else
        {
            // includes local refresh
            Context.ReloadData = false;
            return States.AllChats;
        }
    }

    private async Task Load()
    {
        DateTime newLastKnownState = DateTime.UtcNow;
        AllChatsScreen screen = await Client.LoadAllChatsScreen(chatsNewerThan: _lastKnownChatState, filesNewerThan: _lastKnownChatState, includeProfiles: true);

        foreach (Chat chat in screen.Chats)
        {
            MessageStorage.AddOrUpdateChat(chat);
        }

        ConnectionStorage.UpdateConnections(screen.Connections);

        foreach (ProfilePublic profile in screen.Profiles)
        {
            ProfileStorage.AddOrUpdateProfile(profile);
        }
        
        UserFiles.UpdateUserFiles(screen.Files);

        _lastKnownChatState = newLastKnownState;
    }

    private List<long> DisplayUsers()
    {
        List<long> userIds = new();
        int key = 0;

        foreach (Connection connection in ConnectionStorage.EnumerateConnections())
        {
            ProfilePublic profile = ProfileStorage.GetProfile(connection.ConnectionId);

            DisplayUser(key, connection, profile);
            userIds.Add(connection.ConnectionId);
            key++;
        }

        foreach (ProfilePublic profile in ProfileStorage.EnumerateProfiles())
        {
            if (!userIds.Contains(profile.UserId))
            {
                DisplayUser(key, connection: null, profile);
                userIds.Add(profile.UserId);
                key++;
            }
        }

        return userIds;
    }

    private static void DisplayUser(int key, Connection? connection, ProfilePublic profile)
    {
        string status = connection != null ? connection.Status.ToString() : ConnectionStatus.NotConnected.ToString();
        Console.WriteLine("Press '{0}' for: {1,-24} | {2,-8} | {3,-24} | {4,-14} | {5,-48}",
            key, profile.DisplayName, $"ID={profile.UserId}", $"user name={profile.UserName}", status, $"avatar={profile.AvatarUrl}");
    }

    private State ProcessNumberKey(ConsoleKeyInfo keyInfo, List<long> userIds)
    {
        int index = keyInfo.KeyChar - '0';
        if (index < 0 || index >= userIds.Count)
        {
            Context.ReloadData = false;
            return States.AllChats;
        }
        else
        {
            Context.UserId = userIds[index];
            Context.ReloadData = true;
            return States.OneChat;
        }
    }
}
