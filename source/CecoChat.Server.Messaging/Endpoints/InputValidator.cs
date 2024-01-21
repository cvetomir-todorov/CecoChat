using CecoChat.Contracts.Messaging;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Messaging.Endpoints;

public interface IInputValidator
{
    AbstractValidator<SendPlainTextRequest> SendPlainTextRequest { get; }

    AbstractValidator<ReactRequest> ReactRequest { get; }

    AbstractValidator<UnReactRequest> UnReactRequest { get; }
}

public sealed class InputValidator : IInputValidator
{
    public AbstractValidator<SendPlainTextRequest> SendPlainTextRequest { get; } = new SendPlainTextRequestValidator();

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
            .ValidMessageData();
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
