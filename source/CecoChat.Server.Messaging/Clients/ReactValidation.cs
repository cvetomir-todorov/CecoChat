using CecoChat.Contracts.Messaging;
using FluentValidation;

namespace CecoChat.Server.Messaging.Clients;

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
