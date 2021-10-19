using CecoChat.Contracts.History;

namespace CecoChat.Data.History
{
    internal interface IMessageMapper
    {
        sbyte MapHistoryToDbDataType(DataType dataType);

        sbyte MapHistoryToDbDeliveryStatus(DeliveryStatus deliveryStatus);

        DataType MapDbToHistoryDataType(sbyte dbDataType);

        DeliveryStatus MapDbToHistoryDeliveryStatus(sbyte dbDeliveryStatus);
    }

    internal sealed class MessageMapper : IMessageMapper
    {
        public sbyte MapHistoryToDbDataType(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.PlainText: return (sbyte) DbMessageType.PlainText;
                default:
                    throw new EnumValueNotSupportedException(dataType);
            }
        }

        public sbyte MapHistoryToDbDeliveryStatus(DeliveryStatus deliveryStatus)
        {
            switch (deliveryStatus)
            {
                case DeliveryStatus.Processed: return (sbyte)DbMessageStatus.Processed;
                case DeliveryStatus.Delivered: return (sbyte)DbMessageStatus.Delivered;
                default:
                    throw new EnumValueNotSupportedException(deliveryStatus);
            }
        }

        public DataType MapDbToHistoryDataType(sbyte dbDataType)
        {
            DbMessageType dbDataTypeAsEnum = (DbMessageType)dbDataType;

            switch (dbDataTypeAsEnum)
            {
                case DbMessageType.PlainText: return DataType.PlainText;
                default:
                    throw new EnumValueNotSupportedException(dbDataTypeAsEnum);
            }
        }

        public DeliveryStatus MapDbToHistoryDeliveryStatus(sbyte dbDeliveryStatus)
        {
            DbMessageStatus dbMessageStatusAsEnum = (DbMessageStatus)dbDeliveryStatus;

            switch (dbMessageStatusAsEnum)
            {
                case DbMessageStatus.Processed: return DeliveryStatus.Processed;
                case DbMessageStatus.Delivered: return DeliveryStatus.Delivered;
                default:
                    throw new EnumValueNotSupportedException(dbMessageStatusAsEnum);
            }
        }
    }
}
