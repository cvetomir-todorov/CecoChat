using CecoChat.Data;
using CecoChat.Messaging.Contracts;
using FluentValidation;

namespace CecoChat.Messaging.Service.Endpoints;

public interface IInputValidator
{
    AbstractValidator<SendPlainTextRequest> SendPlainTextRequest { get; }

    AbstractValidator<SendFileRequest> SendFileRequest { get; }

    AbstractValidator<ReactRequest> ReactRequest { get; }

    AbstractValidator<UnReactRequest> UnReactRequest { get; }
}

public sealed class InputValidator : IInputValidator
{
    public AbstractValidator<SendPlainTextRequest> SendPlainTextRequest { get; } = new SendPlainTextRequestValidator();

    public AbstractValidator<SendFileRequest> SendFileRequest { get; } = new SendFileRequestValidator();

    public AbstractValidator<ReactRequest> ReactRequest { get; } = new ReactRequestValidator();

    public AbstractValidator<UnReactRequest> UnReactRequest { get; } = new UnReactRequestValidator();
}

public sealed class SendPlainTextRequestValidator : AbstractValidator<SendPlainTextRequest>
{
    public SendPlainTextRequestValidator()
    {
        RuleFor(x => x.ReceiverId)
            .ValidUserId();
        RuleFor(x => x.Text)
            .ValidMessageText();
    }
}

public sealed class SendFileRequestValidator : AbstractValidator<SendFileRequest>
{
    public SendFileRequestValidator()
    {
        RuleFor(x => x.ReceiverId)
            .ValidUserId();
        RuleFor(x => x.Text)
            .NotNull();
        RuleFor(x => x.Bucket)
            .ValidBucketName();
        RuleFor(x => x.Path)
            .ValidPath();
    }
}

public sealed class ReactRequestValidator : AbstractValidator<ReactRequest>
{
    public ReactRequestValidator()
    {
        RuleFor(x => x.MessageId)
            .ValidMessageId();
        RuleFor(x => x.SenderId)
            .ValidUserId();
        RuleFor(x => x.ReceiverId)
            .ValidUserId();
        RuleFor(x => x.Reaction)
            .ValidReaction();
    }
}

public sealed class UnReactRequestValidator : AbstractValidator<UnReactRequest>
{
    public UnReactRequestValidator()
    {
        RuleFor(x => x.MessageId)
            .ValidMessageId();
        RuleFor(x => x.SenderId)
            .ValidUserId();
        RuleFor(x => x.ReceiverId)
            .ValidUserId();
    }
}
