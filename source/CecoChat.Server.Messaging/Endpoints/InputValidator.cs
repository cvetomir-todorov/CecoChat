using CecoChat.Contracts.Messaging;
using FluentValidation;

namespace CecoChat.Server.Messaging.Endpoints;

public interface IInputValidator
{
    AbstractValidator<SendMessageRequest> SendMessageRequest { get; }

    AbstractValidator<ReactRequest> ReactRequest { get; }

    AbstractValidator<UnReactRequest> UnReactRequest { get; }
}

public sealed class InputValidator : IInputValidator
{
    public AbstractValidator<SendMessageRequest> SendMessageRequest { get; } = new SendMessageRequestValidator();

    public AbstractValidator<ReactRequest> ReactRequest { get; } = new ReactRequestValidator();

    public AbstractValidator<UnReactRequest> UnReactRequest { get; } = new UnReactRequestValidator();
}

public sealed class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.ReceiverId).GreaterThan(0);
        RuleFor(x => x.Data).NotEmpty().MinimumLength(1).MaximumLength(128);
    }
}

public sealed class ReactRequestValidator : AbstractValidator<ReactRequest>
{
    public ReactRequestValidator()
    {
        RuleFor(x => x.MessageId).GreaterThan(0);
        RuleFor(x => x.SenderId).GreaterThan(0);
        RuleFor(x => x.ReceiverId).GreaterThan(0);
        RuleFor(x => x.Reaction).NotEmpty().MaximumLength(8);
    }
}

public sealed class UnReactRequestValidator : AbstractValidator<UnReactRequest>
{
    public UnReactRequestValidator()
    {
        RuleFor(x => x.MessageId).GreaterThan(0);
        RuleFor(x => x.SenderId).GreaterThan(0);
        RuleFor(x => x.ReceiverId).GreaterThan(0);
    }
}
