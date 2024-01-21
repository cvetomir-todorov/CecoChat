namespace CecoChat.Contracts.Messaging;

public enum MessageType
{
    Disconnect = 0,
    DeliveryStatus = 1,
    PlainText = 2,
    Reaction = 3,
    Connection = 4
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
