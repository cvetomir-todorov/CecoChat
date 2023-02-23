namespace CecoChat.Contracts.Messaging;

public interface IChatHub
{
    Task<SendMessageResponse> SendMessage(SendMessageRequest request);

    Task<ReactResponse> React(ReactRequest request);

    Task<UnReactResponse> UnReact(UnReactRequest request);
}

public sealed class SendMessageRequest
{
    public long ReceiverId { get; set; }
    public DataType DataType { get; set; }
    public string Data { get; set; } = string.Empty;
}

public sealed class SendMessageResponse
{
    public long MessageId { get; set; }
}

public sealed class ReactRequest
{
    public long MessageId { get; set; }
    public long SenderId { get; set; }
    public long ReceiverId { get; set; }
    public string Reaction { get; set; } = string.Empty;
}

public sealed class ReactResponse
{ }

public sealed class UnReactRequest
{
    public long MessageId { get; set; }
    public long SenderId { get; set; }
    public long ReceiverId { get; set; }
}

public sealed class UnReactResponse
{ }
