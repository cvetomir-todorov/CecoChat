using CecoChat.Contracts.History;

namespace CecoChat.Data.History.Repos
{
    internal interface IDataMapper
    {
        sbyte MapHistoryToDbDataType(DataType dataType);

        DataType MapDbToHistoryDataType(sbyte dbDataType);
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
    }
}
