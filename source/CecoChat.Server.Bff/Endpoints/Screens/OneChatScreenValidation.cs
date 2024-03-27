using CecoChat.Bff.Contracts.Screens;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Screens;

public sealed class GetOneChatScreenRequestValidator : AbstractValidator<GetOneChatScreenRequest>
{
    public GetOneChatScreenRequestValidator()
    {
        RuleFor(x => x.OtherUserId)
            .ValidUserId();
        RuleFor(x => x.MessagesOlderThan)
            .ValidOlderThanDateTime();
    }
}
