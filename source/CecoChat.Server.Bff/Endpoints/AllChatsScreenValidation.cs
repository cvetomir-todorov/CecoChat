using CecoChat.Contracts.Bff;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints;

public class AllChatsScreenRequestValidation : AbstractValidator<GetAllChatsScreenRequest>
{
    public AllChatsScreenRequestValidation()
    {
        RuleFor(x => x.ChatsNewerThan).ValidNewerThanDateTime();
    }
}
