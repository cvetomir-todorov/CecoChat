using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public abstract class State
{
    protected StateContainer States { get; }

    protected State(StateContainer states)
    {
        States = states;
    }

    protected MessageStorage MessageStorage => States.MessageStorage;
    protected ConnectionStorage ConnectionStorage => States.ConnectionStorage;
    protected ProfileStorage ProfileStorage => States.ProfileStorage;
    protected FileStorage UserFiles => States.FileStorage;
    protected ChatClient Client => States.Client;
    protected StateContext Context => States.Context;

    public abstract Task<State> Execute();

    protected void DisplayUserData()
    {
        if (Client.UserProfile == null)
        {
            throw new InvalidOperationException("Client has not connected.");
        }

        Console.WriteLine("You: {0} | ID={1} | user name={2} | email={3} | phone={4} | avatar={5}",
            Client.UserProfile.DisplayName, Client.UserId, Client.UserProfile.UserName,
            Client.UserProfile.Email, Client.UserProfile.Phone, Client.UserProfile.AvatarUrl);
    }

    protected void DisplayUserFiles()
    {
        foreach (FileRef userFile in UserFiles.EnumerateUserFiles().OrderByDescending(f => f.Version))
        {
            Console.WriteLine("{0}   {1:F}", userFile.Name, userFile.Version);
        }
    }

    protected static void DisplaySplitter()
    {
        Console.WriteLine("=================================================================================================");
    }
}
