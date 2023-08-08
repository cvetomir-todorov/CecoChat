using CecoChat.Contracts.State;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.State.Endpoints;

public sealed class GetChatsRequestValidator : AbstractValidator<GetChatsRequest>
{
    public GetChatsRequestValidator()
    {
        RuleFor(x => x.NewerThan.ToDateTime())
            .ValidNewerThanDateTime();
    }
}
