using CecoChat.Bff.Contracts.Chats;
using Common;

namespace CecoChat.Server.Bff;

public interface IContractMapper
{
    ChatState MapChat(CecoChat.Chats.Contracts.ChatState fromService);

    HistoryMessage MapMessage(CecoChat.Chats.Contracts.HistoryMessage fromService);
}

public class ContractMapper : IContractMapper
{
    public ChatState MapChat(CecoChat.Chats.Contracts.ChatState fromService)
    {
        return new ChatState
        {
            ChatId = fromService.ChatId,
            NewestMessage = fromService.NewestMessage,
            OtherUserDelivered = fromService.OtherUserDelivered,
            OtherUserSeen = fromService.OtherUserSeen
        };
    }

    public HistoryMessage MapMessage(CecoChat.Chats.Contracts.HistoryMessage fromService)
    {
        HistoryMessage toClient = new()
        {
            MessageId = fromService.MessageId,
            SenderId = fromService.SenderId,
            ReceiverId = fromService.ReceiverId,
            Text = fromService.Text
        };

        switch (fromService.DataType)
        {
            case Chats.Contracts.DataType.PlainText:
                toClient.Type = MessageType.PlainText;
                break;
            case Chats.Contracts.DataType.File:
                toClient.Type = MessageType.File;
                toClient.File = new FileData
                {
                    Bucket = fromService.File.Bucket,
                    Path = fromService.File.Path
                };
                break;
            default:
                throw new EnumValueNotSupportedException(fromService.DataType);
        }

        if (fromService.Reactions != null && fromService.Reactions.Count > 0)
        {
            toClient.Reactions = new Dictionary<long, string>(capacity: fromService.Reactions.Count);

            foreach (KeyValuePair<long, string> reaction in fromService.Reactions)
            {
                toClient.Reactions.Add(reaction.Key, reaction.Value);
            }
        }

        return toClient;
    }
}
