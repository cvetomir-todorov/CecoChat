namespace CecoChat.Data.History.Repos
{
    internal enum DbDataType : sbyte
    {
        PlainText = 0
    }

    internal enum DbDeliveryStatus : sbyte
    {
        Processed = 0,
        Delivered = 1
    }
}
