using CecoChat.Contracts.Bff;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints;

public sealed class GetAllChatsScreenRequestValidation : AbstractValidator<GetAllChatsScreenRequest>
{
    public GetAllChatsScreenRequestValidation()
    {
        RuleFor(x => x.ChatsNewerThan)
            .ValidNewerThanDateTime();
    }
}
