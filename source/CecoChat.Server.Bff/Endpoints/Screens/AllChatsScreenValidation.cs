using CecoChat.Contracts.Bff.Screens;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Screens;

public sealed class GetAllChatsScreenRequestValidation : AbstractValidator<GetAllChatsScreenRequest>
{
    public GetAllChatsScreenRequestValidation()
    {
        RuleFor(x => x.ChatsNewerThan)
            .ValidNewerThanDateTime();
    }
}
