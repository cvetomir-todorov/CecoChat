namespace CecoChat.ConsoleClient.Api;

public static class Map
{
    public static LocalStorage.Chat BffChat(Contracts.Bff.ChatState bffChat, long otherUserID)
    {
        return new LocalStorage.Chat(otherUserID)
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
            MessageID = bffHistoryMessage.MessageID,
            SenderID = bffHistoryMessage.SenderID,
            ReceiverID = bffHistoryMessage.ReceiverID
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

        if (bffHistoryMessage.Reactions != null && bffHistoryMessage.Reactions.Count > 0)
        {
            foreach (KeyValuePair<long, string> reaction in bffHistoryMessage.Reactions)
            {
                message.Reactions.Add(reaction.Key, reaction.Value);
            }
        }

        return message;
    }
}