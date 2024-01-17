using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class SearchUsersState : State
{
    public SearchUsersState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();
        DisplaySplitter();

        if (Context.ReloadData)
        {
            Context.UserSearchResult = await Client.GetPublicProfiles(Context.SearchPattern);
        }

        DisplayUsers(Context.UserSearchResult);
        DisplaySplitter();
        Console.WriteLine("Exit (press 'x')");

        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
        if (char.IsNumber(keyInfo.KeyChar))
        {
            return ProcessNumberKey(keyInfo, Context.UserSearchResult);
        }
        else if (keyInfo.KeyChar == 'x')
        {
            Context.UserSearchResult.Clear();
            Context.ReloadData = true;
            return States.AllChats;
        }
        else
        {
            Context.ReloadData = false;
            return States.SearchUsers;
        }
    }

    private void DisplayUsers(List<ProfilePublic> profiles)
    {
        Console.WriteLine("{0} user(s) found for search pattern '{1}':", profiles.Count, Context.SearchPattern);

        for (int i = 0; i < profiles.Count; ++i)
        {
            ProfilePublic profile = profiles[i];
            Connection? connection = ConnectionStorage.GetConnection(profile.UserId);
            string status = connection != null ? connection.Status.ToString() : ConnectionStatus.NotConnected.ToString();

            Console.WriteLine("Press '{0}' for: {1,-24} | {2,-8} | {3,-24} | {4,-14} | {5,-48}",
                i, profile.DisplayName, $"ID={profile.UserId}", $"user name={profile.UserName}", status, $"avatar={profile.AvatarUrl}");
        }
    }

    private State ProcessNumberKey(ConsoleKeyInfo keyInfo, List<ProfilePublic> profiles)
    {
        int index = keyInfo.KeyChar - '0';
        if (index < 0 || index >= profiles.Count)
        {
            Context.ReloadData = false;
            return States.SearchUsers;
        }
        else
        {
            Context.UserId = profiles[index].UserId;
            Context.UserSearchResult.Clear();
            Context.ReloadData = true;
            return States.OneChat;
        }
    }
}
