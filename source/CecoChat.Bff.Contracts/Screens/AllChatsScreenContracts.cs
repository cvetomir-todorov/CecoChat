using System.Text.Json.Serialization;
using CecoChat.Bff.Contracts.Chats;
using CecoChat.Bff.Contracts.Connections;
using CecoChat.Bff.Contracts.Files;
using CecoChat.Bff.Contracts.Profiles;
using Refit;

namespace CecoChat.Bff.Contracts.Screens;

public sealed class GetAllChatsScreenRequest
{
    [JsonPropertyName("chatsNewerThan")]
    [AliasAs("chatsNewerThan")]
    public DateTime ChatsNewerThan { get; init; }

    [JsonPropertyName("filesNewerThan")]
    [AliasAs("filesNewerThan")]
    public DateTime FilesNewerThan { get; init; }

    [JsonPropertyName("includeProfiles")]
    [AliasAs("includeProfiles")]
    public bool IncludeProfiles { get; init; }
}

public sealed class GetAllChatsScreenResponse
{
    [JsonPropertyName("chats")]
    [AliasAs("chats")]
    public ChatState[] Chats { get; init; } = Array.Empty<ChatState>();

    [JsonPropertyName("connections")]
    [AliasAs("connections")]
    public Connection[] Connections { get; init; } = Array.Empty<Connection>();

    [JsonPropertyName("files")]
    [AliasAs("files")]
    public FileRef[] Files { get; init; } = Array.Empty<FileRef>();

    [JsonPropertyName("profiles")]
    [AliasAs("profiles")]
    public ProfilePublic[] Profiles { get; init; } = Array.Empty<ProfilePublic>();
}
