using CecoChat.Contracts.Bff;

namespace CecoChat.Server.Bff;

public interface IContractMapper
{
    ChatState MapChat(Contracts.State.ChatState fromService);

    HistoryMessage MapMessage(Contracts.History.HistoryMessage fromService);
}

public class ContractMapper : IContractMapper
{
    public ChatState MapChat(Contracts.State.ChatState fromService)
    {
        return new ChatState
        {
            ChatId = fromService.ChatId,
            NewestMessage = fromService.NewestMessage,
            OtherUserDelivered = fromService.OtherUserDelivered,
            OtherUserSeen = fromService.OtherUserSeen
        };
    }

    public HistoryMessage MapMessage(Contracts.History.HistoryMessage fromService)
    {
        HistoryMessage toClient = new()
        {
            MessageId = fromService.MessageId,
            SenderId = fromService.SenderId,
            ReceiverId = fromService.ReceiverId
        };

        switch (fromService.DataType)
        {
            case Contracts.History.DataType.PlainText:
                toClient.DataType = DataType.PlainText;
                toClient.Data = fromService.Data;
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
