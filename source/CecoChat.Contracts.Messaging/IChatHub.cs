namespace CecoChat.Contracts.Messaging;

public interface IChatHub
{
    Task<SendPlainTextResponse> SendPlainText(SendPlainTextRequest request);

    Task<ReactResponse> React(ReactRequest request);

    Task<UnReactResponse> UnReact(UnReactRequest request);
}

public sealed class SendPlainTextRequest
{
    public long ReceiverId { get; init; }
    public string Text { get; init; } = string.Empty;
}

public sealed class SendPlainTextResponse
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
