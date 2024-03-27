using CecoChat.Chats.Contracts;
using Common;

namespace CecoChat.Chats.Data.Entities.ChatMessages;

internal sealed class DbFileData
{
    public string Bucket { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

internal enum DbDataType : sbyte
{
    PlainText = 0,
    File = 1
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
            case DataType.File:
                return (sbyte)DbDataType.File;
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
            case DbDataType.File:
                return DataType.File;
            default:
                throw new EnumValueNotSupportedException(dbDataTypeAsEnum);
        }
    }
}
