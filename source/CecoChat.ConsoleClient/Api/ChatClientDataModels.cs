using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Api;

public class ClientResponse
{
    public bool Success { get; set; }
    public List<string> Errors { get; init; } = new();
}

public sealed class ClientResponse<TContent> : ClientResponse
{
    public TContent? Content { get; set; }
}

public sealed class AllChatsScreen
{
    public List<Chat> Chats { get; init; } = new();
    public List<Connection> Connections { get; init; } = new();
    public List<ProfilePublic> Profiles { get; init; } = new();
    public List<FileRef> Files { get; init; } = new();
}

public sealed class OneChatScreen
{
    public List<Message> Messages { get; init; } = new();
    public ProfilePublic? Profile { get; init; }
    public Connection? Connection { get; init; }
}
