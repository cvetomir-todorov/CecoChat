using CecoChat.Contracts.Bff;

namespace CecoChat.Server.Bff;

public interface IContractMapper
{
    ChatState MapChat(Contracts.State.ChatState fromService);
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
}
