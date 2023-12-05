using CecoChat.Contracts.Chats;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Chats.Endpoints;

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

public sealed class GetChatsRequestValidator : AbstractValidator<GetUserChatsRequest>
{
    public GetChatsRequestValidator()
    {
        RuleFor(x => x.NewerThan.ToDateTime())
            .ValidNewerThanDateTime();
    }
}
