namespace CecoChat.Data.History
{
    internal enum DbMessageType : sbyte
    {
        Unknown = 0,
        PlainText = 1,
        Ack = 2
    }

    internal enum DbMessageStatus : sbyte
    {
        Processed = 0,
        Delivered = 1
    }
}
