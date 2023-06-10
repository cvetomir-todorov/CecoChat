using CecoChat.Contracts.Bff;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints;

public sealed class GetOneChatScreenRequestValidator : AbstractValidator<GetOneChatScreenRequest>
{
    public GetOneChatScreenRequestValidator()
    {
        RuleFor(x => x.OtherUserId).ValidUserId();
        RuleFor(x => x.MessagesOlderThan).ValidOlderThanDateTime();
    }
}
