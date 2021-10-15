using System.Collections.Generic;
using CecoChat.Contracts.History;

namespace CecoChat.Data.History
{
    internal interface IMessageMapper
    {
        sbyte MapHistoryToDbMessageType(HistoryMessageType historyMessageType);

        sbyte MapHistoryToDbMessageStatus(HistoryMessageStatus historyMessageStatus);

        IDictionary<string, string> MapHistoryToDbData(HistoryMessage historyMessage);

        HistoryMessageType MapDbToHistoryMessageType(sbyte dbMessageType);

        HistoryMessageStatus MapDbToHistoryMessageStatus(sbyte dbMessageStatus);

        void MapDbToHistoryData(IDictionary<string, string> data, HistoryMessage historyMessage);
    }

    internal sealed class MessageMapper : IMessageMapper
    {
        private const string PlainTextKey = "plain_text";

        public sbyte MapHistoryToDbMessageType(HistoryMessageType historyMessageType)
        {
            switch (historyMessageType)
            {
                case HistoryMessageType.PlainText: return (sbyte) DbMessageType.PlainText;
                default:
                    throw new EnumValueNotSupportedException(historyMessageType);
            }
        }

        public sbyte MapHistoryToDbMessageStatus(HistoryMessageStatus historyMessageStatus)
        {
            switch (historyMessageStatus)
            {
                case HistoryMessageStatus.Processed: return (sbyte)DbMessageStatus.Processed;
                case HistoryMessageStatus.Delivered: return (sbyte)DbMessageStatus.Delivered;
                default:
                    throw new EnumValueNotSupportedException(historyMessageStatus);
            }
        }

        public IDictionary<string, string> MapHistoryToDbData(HistoryMessage historyMessage)
        {
            switch (historyMessage.Type)
            {
                case HistoryMessageType.PlainText: return new SortedDictionary<string, string>
                {
                    {PlainTextKey, historyMessage.Text}
                };
                default:
                    throw new EnumValueNotSupportedException(historyMessage.Type);
            }
        }

        public HistoryMessageType MapDbToHistoryMessageType(sbyte dbMessageType)
        {
            DbMessageType dbMessageTypeAsEnum = (DbMessageType) dbMessageType;

            switch (dbMessageTypeAsEnum)
            {
                case DbMessageType.PlainText: return HistoryMessageType.PlainText;
                default:
                    throw new EnumValueNotSupportedException(dbMessageTypeAsEnum);
            }
        }

        public HistoryMessageStatus MapDbToHistoryMessageStatus(sbyte dbMessageStatus)
        {
            DbMessageStatus dbMessageStatusAsEnum = (DbMessageStatus)dbMessageStatus;

            switch (dbMessageStatusAsEnum)
            {
                case DbMessageStatus.Processed: return HistoryMessageStatus.Processed;
                case DbMessageStatus.Delivered: return HistoryMessageStatus.Delivered;
                default:
                    throw new EnumValueNotSupportedException(dbMessageStatusAsEnum);
            }
        }

        public void MapDbToHistoryData(IDictionary<string, string> data, HistoryMessage historyMessage)
        {
            switch (historyMessage.Type)
            {
                case HistoryMessageType.PlainText:
                {
                    historyMessage.Text = data[PlainTextKey];
                    break;
                }
                default:
                    throw new EnumValueNotSupportedException(historyMessage.Type);
            }
        }
    }
}
