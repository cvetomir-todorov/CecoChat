using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class StateContainer
{
    public StateContainer(
        ChatClient client,
        MessageStorage messageStorage,
        ConnectionStorage connectionStorage,
        ProfileStorage profileStorageStorage,
        FileStorage fileStorage)
    {
        Client = client;
        ConnectionStorage = connectionStorage;
        MessageStorage = messageStorage;
        ProfileStorage = profileStorageStorage;
        FileStorage = fileStorage;
        Context = new StateContext();

        AllChats = new AllChatsState(this);
        EnterUserId = new EnterUserIdState(this);
        OneChat = new OneChatState(this);
        SendMessage = new SendMessageState(this);
        React = new ReactState(this);
        ManageConnection = new ManageConnectionState(this);
        Files = new FilesState(this);
        UploadFile = new UploadFileState(this);
        DownloadFile = new DownloadFileState(this);

        ChangePassword = new ChangePasswordState(this);
        EditProfile = new EditProfileState(this);

        Final = new FinalState(this);
    }

    public ChatClient Client { get; }
    public ConnectionStorage ConnectionStorage { get; }
    public MessageStorage MessageStorage { get; }
    public ProfileStorage ProfileStorage { get; }
    public FileStorage FileStorage { get; }
    public StateContext Context { get; }

    public State AllChats { get; }
    public State EnterUserId { get; }
    public State OneChat { get; }
    public State SendMessage { get; }
    public State React { get; }
    public State ManageConnection { get; }
    public State Files { get; }
    public State UploadFile { get; }
    public State DownloadFile { get; }

    public State ChangePassword { get; }
    public State EditProfile { get; }

    public State Final { get; }
}

public sealed class StateContext
{
    public bool ReloadData { get; set; }
    public long UserId { get; set; }
    public DateTime LastKnownChatsState { get; set; }
    public DateTime LastKnownFilesState { get; set; }
    public FileRef DownloadFile { get; set; } = null!;
}
