namespace CecoChat.ConsoleClient.Api;

public sealed class ClientResponse
{
    public bool Success { get; set; }
    public List<string> Errors { get; init; } = new();
}

public sealed class AllChatsScreen
{
    public List<LocalStorage.Chat> Chats { get; init; } = new();
    public List<LocalStorage.ProfilePublic> Profiles { get; init; } = new();
}

public sealed class OneChatScreen
{
    public List<LocalStorage.Message> Messages { get; init; } = new();
    public LocalStorage.ProfilePublic? Profile { get; init; }
}
