using CecoChat.Data;

namespace CecoChat.ConsoleClient.Api;

public static class Map
{
    public static List<LocalStorage.Chat> BffChats(Contracts.Bff.Chats.ChatState[] bffChats, long currentUserId)
    {
        List<LocalStorage.Chat> chats = new(capacity: bffChats.Length);
        foreach (Contracts.Bff.Chats.ChatState bffChat in bffChats)
        {
            long otherUserId = DataUtility.GetOtherUserId(bffChat.ChatId, currentUserId);
            LocalStorage.Chat chat = BffChat(bffChat, otherUserId);
            chats.Add(chat);
        }

        return chats;
    }

    public static LocalStorage.Chat BffChat(Contracts.Bff.Chats.ChatState bffChat, long otherUserId)
    {
        return new LocalStorage.Chat(otherUserId)
        {
            NewestMessage = bffChat.NewestMessage,
            OtherUserDelivered = bffChat.OtherUserDelivered,
            OtherUserSeen = bffChat.OtherUserSeen
        };
    }

    public static List<LocalStorage.Message> BffMessages(Contracts.Bff.Chats.HistoryMessage[] bffMessages)
    {
        List<LocalStorage.Message> messages = new(bffMessages.Length);
        foreach (Contracts.Bff.Chats.HistoryMessage bffMessage in bffMessages)
        {
            LocalStorage.Message message = BffMessage(bffMessage);
            messages.Add(message);
        }

        return messages;
    }

    public static LocalStorage.Message BffMessage(Contracts.Bff.Chats.HistoryMessage bffHistoryMessage)
    {
        LocalStorage.Message message = new()
        {
            MessageId = bffHistoryMessage.MessageId,
            SenderId = bffHistoryMessage.SenderId,
            ReceiverId = bffHistoryMessage.ReceiverId
        };

        switch (bffHistoryMessage.DataType)
        {
            case Contracts.Bff.Chats.DataType.PlainText:
                message.DataType = LocalStorage.DataType.PlainText;
                message.Data = bffHistoryMessage.Data;
                break;
            default:
                throw new EnumValueNotSupportedException(bffHistoryMessage.DataType);
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

    public static List<LocalStorage.Connection> Connections(Contracts.Bff.Connections.Connection[] bffConnections)
    {
        return bffConnections.Select(Connection).ToList();
    }

    public static LocalStorage.Connection Connection(Contracts.Bff.Connections.Connection bffConnection)
    {
        return new LocalStorage.Connection
        {
            ConnectionId = bffConnection.ConnectionId,
            Version = bffConnection.Version,
            Status = ConnectionStatus(bffConnection.Status)
        };
    }

    public static LocalStorage.ConnectionStatus ConnectionStatus(Contracts.Bff.Connections.ConnectionStatus bffConnectionStatus)
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

    public static List<LocalStorage.ProfilePublic> PublicProfiles(Contracts.Bff.Profiles.ProfilePublic[] bffProfiles)
    {
        return bffProfiles.Select(PublicProfile).ToList();
    }

    public static LocalStorage.ProfilePublic PublicProfile(Contracts.Bff.Profiles.ProfilePublic bffProfile)
    {
        return new LocalStorage.ProfilePublic
        {
            UserId = bffProfile.UserId,
            UserName = bffProfile.UserName,
            DisplayName = bffProfile.DisplayName,
            AvatarUrl = bffProfile.AvatarUrl
        };
    }

    public static List<LocalStorage.FileRef> Files(Contracts.Bff.Files.FileRef[] bffFiles)
    {
        return bffFiles.Select(File).ToList();
    }

    public static LocalStorage.FileRef File(Contracts.Bff.Files.FileRef bffFile)
    {
        return new LocalStorage.FileRef
        {
            Bucket = bffFile.Bucket,
            Path = bffFile.Path,
            Version = bffFile.Version
        };
    }
}
