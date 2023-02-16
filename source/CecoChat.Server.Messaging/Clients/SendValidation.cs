using CecoChat.Contracts.Messaging;
using FluentValidation;

namespace CecoChat.Server.Messaging.Clients;

public sealed class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.ReceiverId).GreaterThan(0);
        RuleFor(x => x.Data).NotEmpty().MinimumLength(1).MaximumLength(128);
    }
}
