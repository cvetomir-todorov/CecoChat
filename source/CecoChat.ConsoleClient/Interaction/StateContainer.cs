using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class StateContainer
{
    public StateContainer(ChatClient client, MessageStorage messageStorage, ConnectionStorage connectionStorage, ProfileStorage profileStorageStorage)
    {
        Client = client;
        ConnectionStorage = connectionStorage;
        MessageStorage = messageStorage;
        ProfileStorage = profileStorageStorage;
        Context = new StateContext();

        AllChats = new AllChatsState(this);
        FindUser = new FindUserState(this);
        OneChat = new OneChatState(this);
        SendMessage = new SendMessageState(this);
        React = new ReactState(this);

        ChangePassword = new ChangePasswordState(this);
        EditProfile = new EditProfileState(this);

        Final = new FinalState(this);
    }

    public ChatClient Client { get; }
    public ConnectionStorage ConnectionStorage { get; }
    public MessageStorage MessageStorage { get; }
    public ProfileStorage ProfileStorage { get; }
    public StateContext Context { get; }

    public State AllChats { get; }
    public State FindUser { get; }
    public State OneChat { get; }
    public State SendMessage { get; }
    public State React { get; }

    public State ChangePassword { get; }
    public State EditProfile { get; }

    public State Final { get; }
}

public sealed class StateContext
{
    public bool ReloadData { get; set; }
    public long UserId { get; set; }
}
