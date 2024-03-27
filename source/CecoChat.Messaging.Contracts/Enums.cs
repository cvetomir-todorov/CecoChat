namespace CecoChat.Messaging.Contracts;

public enum MessageType
{
    Disconnect = 0,
    DeliveryStatus = 1,
    PlainText = 2,
    File = 3,
    Reaction = 4,
    Connection = 5
}

public enum DeliveryStatus
{
    Unprocessed = 0,
    Lost = 1,
    Processed = 2,
    Delivered = 3,
    Seen = 4
}

public enum ConnectionStatus
{
    NotConnected = 0,
    Pending = 1,
    Connected = 2
}
