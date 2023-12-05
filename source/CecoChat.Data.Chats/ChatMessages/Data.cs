using CecoChat.Contracts.History;

namespace CecoChat.Data.Chats.ChatMessages;

internal enum DbDataType : sbyte
{
    PlainText = 0
}

internal interface IDataMapper
{
    sbyte MapContractToDbDataType(DataType dataType);

    DataType MapDbToContractDataType(sbyte dbDataType);
}

internal sealed class DataMapper : IDataMapper
{
    public sbyte MapContractToDbDataType(DataType dataType)
    {
        switch (dataType)
        {
            case DataType.PlainText:
                return (sbyte)DbDataType.PlainText;
            default:
                throw new EnumValueNotSupportedException(dataType);
        }
    }

    public DataType MapDbToContractDataType(sbyte dbDataType)
    {
        DbDataType dbDataTypeAsEnum = (DbDataType)dbDataType;

        switch (dbDataTypeAsEnum)
        {
            case DbDataType.PlainText:
                return DataType.PlainText;
            default:
                throw new EnumValueNotSupportedException(dbDataTypeAsEnum);
        }
    }
}
