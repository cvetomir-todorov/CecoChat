using CecoChat.Contracts.Bff.Chats;
using Common;

namespace CecoChat.Server.Bff;

public interface IContractMapper
{
    ChatState MapChat(Contracts.Chats.ChatState fromService);

    HistoryMessage MapMessage(Contracts.Chats.HistoryMessage fromService);
}

public class ContractMapper : IContractMapper
{
    public ChatState MapChat(Contracts.Chats.ChatState fromService)
    {
        return new ChatState
        {
            ChatId = fromService.ChatId,
            NewestMessage = fromService.NewestMessage,
            OtherUserDelivered = fromService.OtherUserDelivered,
            OtherUserSeen = fromService.OtherUserSeen
        };
    }

    public HistoryMessage MapMessage(Contracts.Chats.HistoryMessage fromService)
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
            case Contracts.Chats.DataType.PlainText:
                toClient.Type = MessageType.PlainText;
                break;
            case Contracts.Chats.DataType.File:
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
