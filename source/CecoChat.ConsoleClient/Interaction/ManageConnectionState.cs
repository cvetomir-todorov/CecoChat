using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;
using CecoChat.Contracts.Bff.Connections;

namespace CecoChat.ConsoleClient.Interaction;

public class ManageConnectionState : State
{
    public ManageConnectionState(StateContainer states) : base(states)
    { }

    public override async Task<State> Execute()
    {
        if (Context.ReloadData)
        {
            // TODO: get the connection
        }

        ProfilePublic profile = ProfileStorage.GetProfile(Context.UserId);
        LocalStorage.Connection? connection = ConnectionStorage.GetConnection(Context.UserId);

        Console.Clear();
        Console.WriteLine("Profile for: {0} | ID={1} | user name={2} | avatar={3}",
            profile.DisplayName, profile.UserId, profile.UserName, profile.AvatarUrl);
        DisplayConnection(connection);

        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
        if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
        {
            Context.ReloadData = true;
            return States.OneChat;
        }

        if (keyInfo.KeyChar == 'i' || keyInfo.KeyChar == 'I')
        {
            return await SendInvite(Context.UserId);
        }
        else if (keyInfo.KeyChar == 'a' || keyInfo.KeyChar == 'A')
        {
            return await ApproveInvite(connection!);
        }
        else if (keyInfo.KeyChar == 'c' || keyInfo.KeyChar == 'C')
        {
            return await CancelInvite(connection!);
        }
        else if (keyInfo.KeyChar == 'r' || keyInfo.KeyChar == 'R')
        {
            return await RemoveConnection(connection!);
        }
        else if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
        {
            Context.ReloadData = true;
            return States.OneChat;
        }
        else
        {
            Context.ReloadData = false;
            return States.ManageConnection;
        }
    }

    private void DisplayConnection(LocalStorage.Connection? connection)
    {
        if (connection == null)
        {
            Console.Write("Not connected | Send an invite (press 'i')");
        }
        else switch (connection.Status)
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

        Console.WriteLine(" | Return (press 'x')");
    }

    private async Task<State> SendInvite(long userId)
    {
        ClientResponse<InviteConnectionResponse> response = await Client.InviteConnection(userId);
        if (!response.Success)
        {
            return DisplayErrorsAndReturnSameState(response);
        }
        else
        {
            ConnectionStorage.AddConnection(userId, LocalStorage.ConnectionStatus.Pending, response.Content!.Version);
            return DisplaySuccessAndReturnOneChatState("Invite sent.");
        }
    }

    private async Task<State> ApproveInvite(LocalStorage.Connection connection)
    {
        ClientResponse<ApproveConnectionResponse> response = await Client.ApproveConnection(connection.ConnectionId, connection.Version);
        if (!response.Success)
        {
            return DisplayErrorsAndReturnSameState(response);
        }
        else
        {
            ConnectionStorage.UpdateConnection(connection.ConnectionId, LocalStorage.ConnectionStatus.Connected, response.Content!.NewVersion);
            return DisplaySuccessAndReturnOneChatState("Invite approved.");
        }
    }

    private async Task<State> CancelInvite(LocalStorage.Connection connection)
    {
        ClientResponse<CancelConnectionResponse> response = await Client.CancelConnection(connection.ConnectionId, connection.Version);
        if (!response.Success)
        {
            return DisplayErrorsAndReturnSameState(response);
        }
        else
        {
            ConnectionStorage.RemoveConnection(connection.ConnectionId);
            return DisplaySuccessAndReturnOneChatState("Invite removed.");
        }
    }

    private async Task<State> RemoveConnection(LocalStorage.Connection connection)
    {
        ClientResponse<RemoveConnectionResponse> response = await Client.RemoveConnection(connection.ConnectionId, connection.Version);
        if (!response.Success)
        {
            return DisplayErrorsAndReturnSameState(response);
        }
        else
        {
            ConnectionStorage.RemoveConnection(connection.ConnectionId);
            return DisplaySuccessAndReturnOneChatState("Connection removed.");
        }
    }

    private State DisplayErrorsAndReturnSameState(ClientResponse response)
    {
        foreach (string error in response.Errors)
        {
            Console.WriteLine(error);
        }

        Console.WriteLine("If the error persists, try logging again. Press ENTER to return.");
        Console.ReadLine();

        Context.ReloadData = true;
        return States.ManageConnection;
    }

    private State DisplaySuccessAndReturnOneChatState(string successMessage)
    {
        Console.WriteLine(successMessage);
        Console.WriteLine("Press ENTER to return.");
        Console.ReadLine();

        Context.ReloadData = true;
        return States.OneChat;
    }
}
