using CecoChat.Contracts.Bff.Chats;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Chats;

public sealed class GetHistoryRequestValidator : AbstractValidator<GetHistoryRequest>
{
    public GetHistoryRequestValidator()
    {
        RuleFor(x => x.OtherUserId)
            .ValidUserId();
        RuleFor(x => x.OlderThan)
            .ValidOlderThanDateTime();
    }
}
