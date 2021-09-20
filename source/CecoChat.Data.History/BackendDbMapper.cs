using System;
using System.Collections.Generic;
using CecoChat.Contracts.Backplane;

namespace CecoChat.Data.History
{
    internal interface IBackendDbMapper
    {
        sbyte MapBackplaneToDbMessageType(BackplaneMessageType backplaneMessageType);

        IDictionary<string, string> MapBackplaneToDbData(BackplaneMessage backplaneMessage);

        BackplaneMessageType MapDbToBackplaneMessageType(sbyte dbMessageType);

        void MapDbToBackplaneData(IDictionary<string, string> data, BackplaneMessage backplaneMessage);
    }

    internal sealed class BackendDbMapper : IBackendDbMapper
    {
        private const string PlainTextKey = "plain_text";

        public sbyte MapBackplaneToDbMessageType(BackplaneMessageType backplaneMessageType)
        {
            switch (backplaneMessageType)
            {
                case BackplaneMessageType.PlainText: return (sbyte) DbMessageType.PlainText;
                default:
                    throw new NotSupportedException($"{typeof(BackplaneMessageType).FullName} value {backplaneMessageType} is not supported.");
            }
        }

        public IDictionary<string, string> MapBackplaneToDbData(BackplaneMessage backplaneMessage)
        {
            switch (backplaneMessage.Type)
            {
                case BackplaneMessageType.PlainText: return new SortedDictionary<string, string>
                {
                    {PlainTextKey, backplaneMessage.Text}
                };
                default:
                    throw new NotSupportedException($"{typeof(BackplaneMessageType).FullName} value {backplaneMessage.Type} is not supported.");
            }
        }

        public BackplaneMessageType MapDbToBackplaneMessageType(sbyte dbMessageType)
        {
            DbMessageType dbMessageTypeAsEnum = (DbMessageType) dbMessageType;

            switch (dbMessageTypeAsEnum)
            {
                case DbMessageType.PlainText: return BackplaneMessageType.PlainText;
                default:
                    throw new NotSupportedException($"{typeof(DbMessageType).FullName} value {dbMessageTypeAsEnum} is not supported.");
            }
        }

        public void MapDbToBackplaneData(IDictionary<string, string> data, BackplaneMessage backplaneMessage)
        {
            switch (backplaneMessage.Type)
            {
                case BackplaneMessageType.PlainText:
                {
                    backplaneMessage.Text = data[PlainTextKey];
                    break;
                }
                default:
                    throw new NotSupportedException($"{typeof(BackplaneMessageType).FullName} value {backplaneMessage.Type} is not supported.");
            }
        }
    }
}
