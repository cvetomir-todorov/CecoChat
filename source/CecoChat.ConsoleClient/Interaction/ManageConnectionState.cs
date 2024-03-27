using CecoChat.Bff.Contracts.Connections;
using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;
using Common;

namespace CecoChat.ConsoleClient.Interaction;

public class ManageConnectionState : State
{
    public ManageConnectionState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        Console.Clear();
        DisplayUserData();
        DisplaySplitter();

        ProfilePublic profile = ProfileStorage.GetProfile(Context.UserId);
        LocalStorage.Connection? connection = ConnectionStorage.GetConnection(Context.UserId);

        Console.WriteLine("Profile for: {0} | ID={1} | user name={2} | avatar={3}",
            profile.DisplayName, profile.UserId, profile.UserName, profile.AvatarUrl);
        DisplayConnection(connection);

        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

        bool unexpectedKey = false;
        if (keyInfo.KeyChar == 'i' || keyInfo.KeyChar == 'I')
        {
            await SendInvite(Context.UserId);
        }
        else if (keyInfo.KeyChar == 'a' || keyInfo.KeyChar == 'A')
        {
            await ApproveInvite(connection!);
        }
        else if (keyInfo.KeyChar == 'c' || keyInfo.KeyChar == 'C')
        {
            await CancelInvite(connection!);
        }
        else if (keyInfo.KeyChar == 'r' || keyInfo.KeyChar == 'R')
        {
            await RemoveConnection(connection!);
        }
        else if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
        {
            // simple exit
        }
        else
        {
            // includes local refresh
            unexpectedKey = true;
        }

        if (unexpectedKey)
        {
            Context.ReloadData = true;
            return States.ManageConnection;
        }
        else
        {
            Context.ReloadData = true;
            return States.OneChat;
        }
    }

    private static void DisplayConnection(LocalStorage.Connection? connection)
    {
        if (connection == null || connection.Status == LocalStorage.ConnectionStatus.NotConnected)
        {
            Console.Write("Not connected | Send an invite (press 'i')");
        }
        else
        {
            switch (connection.Status)
            {
                case LocalStorage.ConnectionStatus.Pending:
                    Console.Write("Invitation pending | Accept (press 'a') | Cancel (press 'c')");
                    break;
                case LocalStorage.ConnectionStatus.Connected:
                    Console.Write("Connected | Remove (press 'r')");
                    break;
                default:
                    throw new EnumValueNotSupportedException(connection.Status);
            }
        }

        Console.WriteLine(" | Local refresh (press 'l') | Return (press 'x')");
    }

    private async Task SendInvite(long userId)
    {
        ClientResponse<InviteConnectionResponse> response = await Client.InviteConnection(userId);
        if (!response.Success)
        {
            DisplayErrors(response);
        }
        else
        {
            ConnectionStorage.UpdateConnection(userId, LocalStorage.ConnectionStatus.Pending, response.Content!.Version);
            DisplaySuccess("Invite sent.");
        }
    }

    private async Task ApproveInvite(LocalStorage.Connection connection)
    {
        ClientResponse<ApproveConnectionResponse> response = await Client.ApproveConnection(connection.ConnectionId, connection.Version);
        if (!response.Success)
        {
            DisplayErrors(response);
        }
        else
        {
            ConnectionStorage.UpdateConnection(connection.ConnectionId, LocalStorage.ConnectionStatus.Connected, response.Content!.NewVersion);
            DisplaySuccess("Invite approved.");
        }
    }

    private async Task CancelInvite(LocalStorage.Connection connection)
    {
        ClientResponse<CancelConnectionResponse> response = await Client.CancelConnection(connection.ConnectionId, connection.Version);
        if (!response.Success)
        {
            DisplayErrors(response);
        }
        else
        {
            ConnectionStorage.UpdateConnection(connection.ConnectionId, LocalStorage.ConnectionStatus.NotConnected, response.Content!.NewVersion);
            DisplaySuccess("Invite removed.");
        }
    }

    private async Task RemoveConnection(LocalStorage.Connection connection)
    {
        ClientResponse<RemoveConnectionResponse> response = await Client.RemoveConnection(connection.ConnectionId, connection.Version);
        if (!response.Success)
        {
            DisplayErrors(response);
        }
        else
        {
            ConnectionStorage.UpdateConnection(connection.ConnectionId, LocalStorage.ConnectionStatus.NotConnected, response.Content!.NewVersion);
            DisplaySuccess("Connection removed.");
        }
    }

    private static void DisplaySuccess(string successMessage)
    {
        Console.WriteLine(successMessage);
        Console.WriteLine("Press ENTER to return.");
        Console.ReadLine();
    }
}
