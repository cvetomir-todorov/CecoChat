using CecoChat.Contracts.History;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.History.Endpoints;

public sealed class GetHistoryRequestValidator : AbstractValidator<GetHistoryRequest>
{
    public GetHistoryRequestValidator()
    {
        RuleFor(x => x.OtherUserId)
            .ValidUserId();
        RuleFor(x => x.OlderThan.ToDateTime())
            .ValidOlderThanDateTime();
    }
}
