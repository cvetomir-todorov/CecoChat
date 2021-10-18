using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.History;

namespace CecoChat.Server
{
    public interface IMessageMapper
    {
        HistoryMessage MapBackplaneToHistoryMessage(BackplaneMessage backplaneMessage);
    }

    public sealed class MessageMapper : IMessageMapper
    {
        public HistoryMessage MapBackplaneToHistoryMessage(BackplaneMessage backplaneMessage)
        {
            HistoryMessage historyMessage = new()
            {
                MessageId = backplaneMessage.MessageId,
                SenderId = backplaneMessage.SenderId,
                ReceiverId = backplaneMessage.ReceiverId
            };

            switch (backplaneMessage.Type)
            {
                case BackplaneMessageType.PlainText:
                    historyMessage.Type = HistoryMessageType.PlainText;
                    historyMessage.Text = backplaneMessage.Text;
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Type);
            }

            switch (backplaneMessage.Status)
            {
                case BackplaneMessageStatus.Processed:
                    historyMessage.Status = HistoryMessageStatus.Processed;
                    break;
                case BackplaneMessageStatus.Delivered:
                    historyMessage.Status = HistoryMessageStatus.Delivered;
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Status);
            }

            return historyMessage;
        }
    }
}
