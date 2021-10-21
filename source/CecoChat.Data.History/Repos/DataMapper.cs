using CecoChat.Contracts.History;

namespace CecoChat.Data.History.Repos
{
    internal interface IDataMapper
    {
        sbyte MapHistoryToDbDataType(DataType dataType);

        sbyte MapHistoryToDbDeliveryStatus(DeliveryStatus deliveryStatus);

        DataType MapDbToHistoryDataType(sbyte dbDataType);

        DeliveryStatus MapDbToHistoryDeliveryStatus(sbyte dbDeliveryStatus);
    }

    internal sealed class DataMapper : IDataMapper
    {
        public sbyte MapHistoryToDbDataType(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.PlainText: return (sbyte) DbDataType.PlainText;
                default:
                    throw new EnumValueNotSupportedException(dataType);
            }
        }

        public sbyte MapHistoryToDbDeliveryStatus(DeliveryStatus deliveryStatus)
        {
            switch (deliveryStatus)
            {
                case DeliveryStatus.Processed: return (sbyte)DbDeliveryStatus.Processed;
                case DeliveryStatus.Delivered: return (sbyte)DbDeliveryStatus.Delivered;
                default:
                    throw new EnumValueNotSupportedException(deliveryStatus);
            }
        }

        public DataType MapDbToHistoryDataType(sbyte dbDataType)
        {
            DbDataType dbDataTypeAsEnum = (DbDataType)dbDataType;

            switch (dbDataTypeAsEnum)
            {
                case DbDataType.PlainText: return DataType.PlainText;
                default:
                    throw new EnumValueNotSupportedException(dbDataTypeAsEnum);
            }
        }

        public DeliveryStatus MapDbToHistoryDeliveryStatus(sbyte dbDeliveryStatus)
        {
            DbDeliveryStatus dbDeliveryStatusAsEnum = (DbDeliveryStatus)dbDeliveryStatus;

            switch (dbDeliveryStatusAsEnum)
            {
                case DbDeliveryStatus.Processed: return DeliveryStatus.Processed;
                case DbDeliveryStatus.Delivered: return DeliveryStatus.Delivered;
                default:
                    throw new EnumValueNotSupportedException(dbDeliveryStatusAsEnum);
            }
        }
    }
}
