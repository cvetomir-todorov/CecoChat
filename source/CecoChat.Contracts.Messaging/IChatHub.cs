namespace CecoChat.Contracts.Messaging;

public interface IChatHub
{
    Task<SendMessageResponse> SendMessage(SendMessageRequest request);

    Task<ReactResponse> React(ReactRequest request);

    Task<UnReactResponse> UnReact(UnReactRequest request);
}

public sealed class SendMessageRequest
{
    public long ReceiverId { get; init; }
    public DataType DataType { get; init; }
    public string Data { get; init; } = string.Empty;
}

public sealed class SendMessageResponse
{
    public long MessageId { get; init; }
}

public sealed class ReactRequest
{
    public long MessageId { get; init; }
    public long SenderId { get; init; }
    public long ReceiverId { get; init; }
    public string Reaction { get; init; } = string.Empty;
}

public sealed class ReactResponse
{ }

public sealed class UnReactRequest
{
    public long MessageId { get; init; }
    public long SenderId { get; init; }
    public long ReceiverId { get; init; }
}

public sealed class UnReactResponse
{ }
