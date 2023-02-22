namespace CecoChat.Contracts.Messaging;

public enum MessageType
{
    Disconnect = 0,
    DeliveryStatus = 1,
    Data = 2,
    Reaction = 3
}

public enum DataType
{
    PlainText = 0
}

public enum DeliveryStatus
{
    Unprocessed = 0,
    Lost = 1,
    Processed = 2,
    Delivered = 3,
    Seen = 4
}
