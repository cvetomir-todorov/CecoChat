using CecoChat.Bff.Contracts.Chats;
using CecoChat.ConsoleClient.LocalStorage;
using CecoChat.Data;
using Common;

namespace CecoChat.ConsoleClient.Api;

public static class Map
{
    public static List<Chat> BffChats(ChatState[] bffChats, long currentUserId)
    {
        List<Chat> chats = new(capacity: bffChats.Length);
        foreach (ChatState bffChat in bffChats)
        {
            long otherUserId = DataUtility.GetOtherUserId(bffChat.ChatId, currentUserId);
            Chat chat = BffChat(bffChat, otherUserId);
            chats.Add(chat);
        }

        return chats;
    }

    public static Chat BffChat(ChatState bffChat, long otherUserId)
    {
        return new Chat(otherUserId)
        {
            NewestMessage = bffChat.NewestMessage,
            OtherUserDelivered = bffChat.OtherUserDelivered,
            OtherUserSeen = bffChat.OtherUserSeen
        };
    }

    public static List<Message> BffMessages(HistoryMessage[] bffMessages)
    {
        List<Message> messages = new(bffMessages.Length);
        foreach (HistoryMessage bffMessage in bffMessages)
        {
            Message message = BffMessage(bffMessage);
            messages.Add(message);
        }

        return messages;
    }

    public static Message BffMessage(HistoryMessage bffHistoryMessage)
    {
        Message message = new()
        {
            MessageId = bffHistoryMessage.MessageId,
            SenderId = bffHistoryMessage.SenderId,
            ReceiverId = bffHistoryMessage.ReceiverId,
            Text = bffHistoryMessage.Text
        };

        switch (bffHistoryMessage.Type)
        {
            case Bff.Contracts.Chats.MessageType.PlainText:
                message.Type = LocalStorage.MessageType.PlainText;
                break;
            case Bff.Contracts.Chats.MessageType.File:
                message.Type = LocalStorage.MessageType.File;
                message.FileBucket = bffHistoryMessage.File!.Bucket;
                message.FilePath = bffHistoryMessage.File!.Path;
                break;
            default:
                throw new EnumValueNotSupportedException(bffHistoryMessage.Type);
        }

        if (bffHistoryMessage.Reactions.Count > 0)
        {
            foreach (KeyValuePair<long, string> reaction in bffHistoryMessage.Reactions)
            {
                message.Reactions.Add(reaction.Key, reaction.Value);
            }
        }

        return message;
    }

    public static List<Connection> Connections(Bff.Contracts.Connections.Connection[] bffConnections)
    {
        return bffConnections.Select(Connection).ToList();
    }

    public static Connection Connection(Bff.Contracts.Connections.Connection bffConnection)
    {
        return new Connection
        {
            ConnectionId = bffConnection.ConnectionId,
            Version = bffConnection.Version,
            Status = ConnectionStatus(bffConnection.Status)
        };
    }

    public static ConnectionStatus ConnectionStatus(Bff.Contracts.Connections.ConnectionStatus bffConnectionStatus)
    {
        switch (bffConnectionStatus)
        {
            case Bff.Contracts.Connections.ConnectionStatus.NotConnected:
                return LocalStorage.ConnectionStatus.NotConnected;
            case Bff.Contracts.Connections.ConnectionStatus.Pending:
                return LocalStorage.ConnectionStatus.Pending;
            case Bff.Contracts.Connections.ConnectionStatus.Connected:
                return LocalStorage.ConnectionStatus.Connected;
            default:
                throw new EnumValueNotSupportedException(bffConnectionStatus);
        }
    }

    public static List<ProfilePublic> PublicProfiles(Bff.Contracts.Profiles.ProfilePublic[] bffProfiles)
    {
        return bffProfiles.Select(PublicProfile).ToList();
    }

    public static ProfilePublic PublicProfile(Bff.Contracts.Profiles.ProfilePublic bffProfile)
    {
        return new ProfilePublic
        {
            UserId = bffProfile.UserId,
            UserName = bffProfile.UserName,
            DisplayName = bffProfile.DisplayName,
            AvatarUrl = bffProfile.AvatarUrl
        };
    }

    public static List<FileRef> Files(Bff.Contracts.Files.FileRef[] bffFiles)
    {
        return bffFiles.Select(File).ToList();
    }

    public static FileRef File(Bff.Contracts.Files.FileRef bffFile)
    {
        return new FileRef
        {
            Bucket = bffFile.Bucket,
            Path = bffFile.Path,
            Version = bffFile.Version
        };
    }
}
