using CecoChat.Data;

namespace CecoChat.ConsoleClient.Api;

public static class Map
{
    public static List<LocalStorage.Chat> BffChats(Contracts.Bff.ChatState[] bffChats, long currentUserId)
    {
        List<LocalStorage.Chat> chats = new(capacity: bffChats.Length);
        foreach (Contracts.Bff.ChatState bffChat in bffChats)
        {
            long otherUserId = DataUtility.GetOtherUsedId(bffChat.ChatId, currentUserId);
            LocalStorage.Chat chat = BffChat(bffChat, otherUserId);
            chats.Add(chat);
        }

        return chats;
    }

    public static LocalStorage.Chat BffChat(Contracts.Bff.ChatState bffChat, long otherUserId)
    {
        return new LocalStorage.Chat(otherUserId)
        {
            NewestMessage = bffChat.NewestMessage,
            OtherUserDelivered = bffChat.OtherUserDelivered,
            OtherUserSeen = bffChat.OtherUserSeen
        };
    }

    public static List<LocalStorage.Message> BffMessages(Contracts.Bff.HistoryMessage[] bffMessages)
    {
        List<LocalStorage.Message> messages = new(bffMessages.Length);
        foreach (Contracts.Bff.HistoryMessage bffMessage in bffMessages)
        {
            LocalStorage.Message message = BffMessage(bffMessage);
            messages.Add(message);
        }

        return messages;
    }

    public static LocalStorage.Message BffMessage(Contracts.Bff.HistoryMessage bffHistoryMessage)
    {
        LocalStorage.Message message = new()
        {
            MessageId = bffHistoryMessage.MessageId,
            SenderId = bffHistoryMessage.SenderId,
            ReceiverId = bffHistoryMessage.ReceiverId
        };

        switch (bffHistoryMessage.DataType)
        {
            case Contracts.Bff.DataType.PlainText:
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

    public static List<LocalStorage.ProfilePublic> PublicProfiles(Contracts.Bff.ProfilePublic[] bffProfiles)
    {
        List<LocalStorage.ProfilePublic> profiles = new(capacity: bffProfiles.Length);

        foreach (Contracts.Bff.ProfilePublic profile in bffProfiles)
        {
            profiles.Add(PublicProfile(profile));
        }

        return profiles;
    }

    public static LocalStorage.ProfilePublic PublicProfile(Contracts.Bff.ProfilePublic bffProfile)
    {
        return new LocalStorage.ProfilePublic
        {
            UserId = bffProfile.UserId,
            UserName = bffProfile.UserName,
            DisplayName = bffProfile.DisplayName,
            AvatarUrl = bffProfile.AvatarUrl
        };
    }
}
