using System;
using System.Collections.Generic;

namespace CecoChat.Data.History
{
    internal interface IMessageMapper
    {
        sbyte MapHistoryToDbMessageType(HistoryMessageType historyMessageType);

        IDictionary<string, string> MapHistoryToDbData(HistoryMessage historyMessage);

        HistoryMessageType MapDbToHistoryMessageType(sbyte dbMessageType);

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
                    throw new NotSupportedException($"{typeof(HistoryMessageType).FullName} value {historyMessageType} is not supported.");
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
                    throw new NotSupportedException($"{typeof(HistoryMessageType).FullName} value {historyMessage.Type} is not supported.");
            }
        }

        public HistoryMessageType MapDbToHistoryMessageType(sbyte dbMessageType)
        {
            DbMessageType dbMessageTypeAsEnum = (DbMessageType) dbMessageType;

            switch (dbMessageTypeAsEnum)
            {
                case DbMessageType.PlainText: return HistoryMessageType.PlainText;
                default:
                    throw new NotSupportedException($"{typeof(DbMessageType).FullName} value {dbMessageTypeAsEnum} is not supported.");
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
                    throw new NotSupportedException($"{typeof(HistoryMessageType).FullName} value {historyMessage.Type} is not supported.");
            }
        }
    }
}
