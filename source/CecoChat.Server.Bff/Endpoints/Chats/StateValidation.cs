using CecoChat.Contracts.Bff.Chats;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Chats;

public sealed class GetChatsRequestValidator : AbstractValidator<GetChatsRequest>
{
    public GetChatsRequestValidator()
    {
        RuleFor(x => x.NewerThan)
            .ValidNewerThanDateTime();
    }
}
