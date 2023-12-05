using CecoChat.Contracts.Chats;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Chats.Endpoints;

public sealed class GetChatHistoryRequestValidator : AbstractValidator<GetChatHistoryRequest>
{
    public GetChatHistoryRequestValidator()
    {
        RuleFor(x => x.OtherUserId)
            .ValidUserId();
        RuleFor(x => x.OlderThan.ToDateTime())
            .ValidOlderThanDateTime();
    }
}

public sealed class GetUserChatsRequestValidator : AbstractValidator<GetUserChatsRequest>
{
    public GetUserChatsRequestValidator()
    {
        RuleFor(x => x.NewerThan.ToDateTime())
            .ValidNewerThanDateTime();
    }
}
