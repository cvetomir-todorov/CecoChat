using CecoChat.Bff.Contracts.Screens;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Bff.Service.Endpoints.Screens;

public sealed class GetAllChatsScreenRequestValidation : AbstractValidator<GetAllChatsScreenRequest>
{
    public GetAllChatsScreenRequestValidation()
    {
        RuleFor(x => x.ChatsNewerThan)
            .ValidNewerThanDateTime();
    }
}
