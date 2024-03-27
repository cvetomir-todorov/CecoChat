using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;
using CecoChat.Messaging.Client;

namespace CecoChat.ConsoleClient.Interaction;

public sealed class StateContainer
{
    public StateContainer(
        ChatClient client,
        IMessagingClient messagingClient,
        MessageStorage messageStorage,
        ConnectionStorage connectionStorage,
        ProfileStorage profileStorageStorage,
        FileStorage fileStorage)
    {
        Client = client;
        MessagingClient = messagingClient;
        ConnectionStorage = connectionStorage;
        MessageStorage = messageStorage;
        ProfileStorage = profileStorageStorage;
        FileStorage = fileStorage;
        Context = new StateContext();

        AllChats = new AllChatsState(this);
        EnterUserId = new EnterUserIdState(this);
        EnterSearchPattern = new EnterSearchPatternState(this);
        SearchUsers = new SearchUsersState(this);
        OneChat = new OneChatState(this);
        SendMessage = new SendMessageState(this);
        React = new ReactState(this);
        SendFile = new SendFileState(this);
        DownloadSentFile = new DownloadSentFileState(this);
        ManageConnection = new ManageConnectionState(this);
        Files = new FilesState(this);
        UploadFile = new UploadFileState(this);
        DownloadOwnFile = new DownloadOwnFileState(this);

        ChangePassword = new ChangePasswordState(this);
        EditProfile = new EditProfileState(this);

        Final = new FinalState(this);
    }

    public ChatClient Client { get; }
    public IMessagingClient MessagingClient { get; }
    public ConnectionStorage ConnectionStorage { get; }
    public MessageStorage MessageStorage { get; }
    public ProfileStorage ProfileStorage { get; }
    public FileStorage FileStorage { get; }
    public StateContext Context { get; }

    public State AllChats { get; }
    public State EnterUserId { get; }
    public State EnterSearchPattern { get; }
    public State SearchUsers { get; }
    public State OneChat { get; }
    public State SendMessage { get; }
    public State React { get; }
    public State SendFile { get; }
    public State DownloadSentFile { get; }
    public State ManageConnection { get; }
    public State Files { get; }
    public State UploadFile { get; }
    public State DownloadOwnFile { get; }

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
    public string SearchPattern { get; set; } = string.Empty;
    public List<ProfilePublic> UserSearchResult { get; set; } = new(capacity: 0);
}
