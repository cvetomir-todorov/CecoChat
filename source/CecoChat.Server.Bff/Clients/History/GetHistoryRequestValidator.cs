using CecoChat.Contracts.Bff;
using FluentValidation;

namespace CecoChat.Server.Bff.Clients.History;

public sealed class GetHistoryRequestValidator : AbstractValidator<GetHistoryRequest>
{
    public GetHistoryRequestValidator()
    {
        RuleFor(x => x.OtherUserID).GreaterThan(0);
        RuleFor(x => x.OlderThan).GreaterThan(Snowflake.Epoch);
    }
}