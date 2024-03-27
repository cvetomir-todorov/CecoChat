using CecoChat.Bff.Contracts.Chats;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Chats;

public sealed class GetChatHistoryRequestValidator : AbstractValidator<GetChatHistoryRequest>
{
    public GetChatHistoryRequestValidator()
    {
        RuleFor(x => x.OtherUserId)
            .ValidUserId();
        RuleFor(x => x.OlderThan)
            .ValidOlderThanDateTime();
    }
}

public sealed class GetUserChatsRequestValidator : AbstractValidator<GetUserChatsRequest>
{
    public GetUserChatsRequestValidator()
    {
        RuleFor(x => x.NewerThan)
            .ValidNewerThanDateTime();
    }
}
