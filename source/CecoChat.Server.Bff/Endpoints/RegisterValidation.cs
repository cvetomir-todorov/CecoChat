using CecoChat.Contracts.Bff;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.UserName)
            .ValidUserName();
        RuleFor(x => x.Password)
            .ValidPassword();
        RuleFor(x => x.DisplayName)
            .ValidDisplayName();
        RuleFor(x => x.Phone)
            .ValidPhone();
        RuleFor(x => x.Email)
            .ValidEmail();
    }
}
