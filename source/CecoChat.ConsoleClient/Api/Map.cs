using CecoChat.ConsoleClient.LocalStorage;
using CecoChat.Data;

namespace CecoChat.ConsoleClient.Api;

public static class Map
{
    public static List<Chat> BffChats(Contracts.Bff.Chats.ChatState[] bffChats, long currentUserId)
    {
        List<Chat> chats = new(capacity: bffChats.Length);
        foreach (Contracts.Bff.Chats.ChatState bffChat in bffChats)
        {
            long otherUserId = DataUtility.GetOtherUserId(bffChat.ChatId, currentUserId);
            Chat chat = BffChat(bffChat, otherUserId);
            chats.Add(chat);
        }

        return chats;
    }

    public static Chat BffChat(Contracts.Bff.Chats.ChatState bffChat, long otherUserId)
    {
        return new Chat(otherUserId)
        {
            NewestMessage = bffChat.NewestMessage,
            OtherUserDelivered = bffChat.OtherUserDelivered,
            OtherUserSeen = bffChat.OtherUserSeen
        };
    }

    public static List<Message> BffMessages(Contracts.Bff.Chats.HistoryMessage[] bffMessages)
    {
        List<Message> messages = new(bffMessages.Length);
        foreach (Contracts.Bff.Chats.HistoryMessage bffMessage in bffMessages)
        {
            Message message = BffMessage(bffMessage);
            messages.Add(message);
        }

        return messages;
    }

    public static Message BffMessage(Contracts.Bff.Chats.HistoryMessage bffHistoryMessage)
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
            case Contracts.Bff.Chats.MessageType.PlainText:
                message.Type = MessageType.PlainText;
                break;
            case Contracts.Bff.Chats.MessageType.File:
                message.Type = MessageType.File;
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

    public static List<Connection> Connections(Contracts.Bff.Connections.Connection[] bffConnections)
    {
        return bffConnections.Select(Connection).ToList();
    }

    public static Connection Connection(Contracts.Bff.Connections.Connection bffConnection)
    {
        return new Connection
        {
            ConnectionId = bffConnection.ConnectionId,
            Version = bffConnection.Version,
            Status = ConnectionStatus(bffConnection.Status)
        };
    }

    public static ConnectionStatus ConnectionStatus(Contracts.Bff.Connections.ConnectionStatus bffConnectionStatus)
    {
        switch (bffConnectionStatus)
        {
            case Contracts.Bff.Connections.ConnectionStatus.NotConnected:
                return LocalStorage.ConnectionStatus.NotConnected;
            case Contracts.Bff.Connections.ConnectionStatus.Pending:
                return LocalStorage.ConnectionStatus.Pending;
            case Contracts.Bff.Connections.ConnectionStatus.Connected:
                return LocalStorage.ConnectionStatus.Connected;
            default:
                throw new EnumValueNotSupportedException(bffConnectionStatus);
        }
    }

    public static List<ProfilePublic> PublicProfiles(Contracts.Bff.Profiles.ProfilePublic[] bffProfiles)
    {
        return bffProfiles.Select(PublicProfile).ToList();
    }

    public static ProfilePublic PublicProfile(Contracts.Bff.Profiles.ProfilePublic bffProfile)
    {
        return new ProfilePublic
        {
            UserId = bffProfile.UserId,
            UserName = bffProfile.UserName,
            DisplayName = bffProfile.DisplayName,
            AvatarUrl = bffProfile.AvatarUrl
        };
    }

    public static List<FileRef> Files(Contracts.Bff.Files.FileRef[] bffFiles)
    {
        return bffFiles.Select(File).ToList();
    }

    public static FileRef File(Contracts.Bff.Files.FileRef bffFile)
    {
        return new FileRef
        {
            Bucket = bffFile.Bucket,
            Path = bffFile.Path,
            Version = bffFile.Version
        };
    }
}
