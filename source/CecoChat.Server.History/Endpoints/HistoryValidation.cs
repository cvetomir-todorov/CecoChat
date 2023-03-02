using CecoChat.Contracts.History;
using FluentValidation;

namespace CecoChat.Server.History.Endpoints;

public sealed class GetHistoryRequestValidator : AbstractValidator<GetHistoryRequest>
{
    public GetHistoryRequestValidator()
    {
        RuleFor(x => x.OtherUserId).GreaterThan(0);
    }
}
