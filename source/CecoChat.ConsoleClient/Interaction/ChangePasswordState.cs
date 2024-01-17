using CecoChat.ConsoleClient.Api;

namespace CecoChat.ConsoleClient.Interaction;

public class ChangePasswordState : State
{
    public ChangePasswordState(StateContainer states) : base(states)
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
        Console.Write("Enter new password: ");
        string newPassword = Console.ReadLine() ?? string.Empty;

        ClientResponse response = await Client.ChangePassword(newPassword);
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
            Console.WriteLine("Password changed successfully!");
            Console.WriteLine("Press ENTER to return.");
            Console.ReadLine();
        }

        Context.ReloadData = true;
        return States.AllChats;
    }
}
