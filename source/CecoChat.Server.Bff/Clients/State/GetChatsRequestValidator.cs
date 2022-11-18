using CecoChat.Contracts.Bff;
using FluentValidation;

namespace CecoChat.Server.Bff.Clients.State;

public sealed class GetChatsRequestValidator : AbstractValidator<GetChatsRequest>
{
    public GetChatsRequestValidator()
    {
        RuleFor(x => x.NewerThan).GreaterThanOrEqualTo(Snowflake.Epoch);
    }
}