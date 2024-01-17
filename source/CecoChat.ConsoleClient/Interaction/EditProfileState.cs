using CecoChat.ConsoleClient.Api;

namespace CecoChat.ConsoleClient.Interaction;

public class EditProfileState : State
{
    public EditProfileState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Clear();

        if (Client.UserProfile == null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        DisplayUserData();
        DisplaySplitter();
        Console.WriteLine("Edit your profile - press empty without entering anything to preserve current.");

        Console.Write("Enter display name, current is '{0}': ", Client.UserProfile.DisplayName);
        string displayName = Console.ReadLine() ?? string.Empty;
        if (displayName.Length == 0)
        {
            displayName = Client.UserProfile.DisplayName;
        }

        ClientResponse response = await Client.EditProfile(displayName);
        if (!response.Success)
        {
            foreach (string error in response.Errors)
            {
                Console.WriteLine(error);
            }

            Console.WriteLine("If the error persists, try logging again. Press ENTER to return.");
            Console.ReadLine();
        }
        else
        {
            Client.UserProfile.DisplayName = displayName;

            Console.WriteLine("Profile edited successfully!");
            Console.WriteLine("Press ENTER to return.");
            Console.ReadLine();
        }

        Context.ReloadData = true;
        return States.AllChats;
    }
}
