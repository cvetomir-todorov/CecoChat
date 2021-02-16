using System;
using System.Collections.Generic;
using CecoChat.Contracts.Backend;

namespace CecoChat.Data.History
{
    public interface IBackendDbMapper
    {
        sbyte MapBackendToDbMessageType(BackendMessageType backendMessageType);

        IDictionary<string, string> MapBackendToDbData(BackendMessage backendMessage);

        BackendMessageType MapDbToBackendMessageType(sbyte dbMessageType);

        void MapDbToBackendData(IDictionary<string, string> data, BackendMessage backendMessage);
    }

    public sealed class BackendDbMapper : IBackendDbMapper
    {
        private const string PlainTextKey = "plain_text";

        public sbyte MapBackendToDbMessageType(BackendMessageType backendMessageType)
        {
            switch (backendMessageType)
            {
                case BackendMessageType.PlainText: return (sbyte) DbMessageType.PlainText;
                default:
                    throw new NotSupportedException($"{typeof(BackendMessageType).FullName} value {backendMessageType} is not supported.");
            }
        }

        public IDictionary<string, string> MapBackendToDbData(BackendMessage backendMessage)
        {
            switch (backendMessage.Type)
            {
                case BackendMessageType.PlainText: return new SortedDictionary<string, string>
                {
                    {PlainTextKey, backendMessage.Text}
                };
                default:
                    throw new NotSupportedException($"{typeof(BackendMessageType).FullName} value {backendMessage.Type} is not supported.");
            }
        }

        public BackendMessageType MapDbToBackendMessageType(sbyte dbMessageType)
        {
            DbMessageType dbMessageTypeAsEnum = (DbMessageType) dbMessageType;

            switch (dbMessageTypeAsEnum)
            {
                case DbMessageType.PlainText: return BackendMessageType.PlainText;
                default:
                    throw new NotSupportedException($"{typeof(DbMessageType).FullName} value {dbMessageTypeAsEnum} is not supported.");
            }
        }

        public void MapDbToBackendData(IDictionary<string, string> data, BackendMessage backendMessage)
        {
            switch (backendMessage.Type)
            {
                case BackendMessageType.PlainText:
                {
                    backendMessage.Text = data[PlainTextKey];
                    break;
                }
                default:
                    throw new NotSupportedException($"{typeof(BackendMessageType).FullName} value {backendMessage.Type} is not supported.");
            }
        }
    }
}
