namespace CecoChat.ConsoleClient.Api;

public static class Map
{
    public static LocalStorage.Chat BffChat(Contracts.Bff.ChatState bffChat, long otherUserId)
    {
        return new LocalStorage.Chat(otherUserId)
        {
            NewestMessage = bffChat.NewestMessage,
            OtherUserDelivered = bffChat.OtherUserDelivered,
            OtherUserSeen = bffChat.OtherUserSeen
        };
    }

    public static LocalStorage.Message BffMessage(Contracts.Bff.HistoryMessage bffHistoryMessage)
    {
        LocalStorage.Message message = new()
        {
            MessageId = bffHistoryMessage.MessageID,
            SenderId = bffHistoryMessage.SenderID,
            ReceiverId = bffHistoryMessage.ReceiverID
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