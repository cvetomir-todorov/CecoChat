using CecoChat.Contracts.Bff;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints;

public sealed class GetHistoryRequestValidator : AbstractValidator<GetHistoryRequest>
{
    public GetHistoryRequestValidator()
    {
        RuleFor(x => x.OtherUserId).ValidUserId();
        RuleFor(x => x.OlderThan).ValidOlderThanDateTime();
    }
}
