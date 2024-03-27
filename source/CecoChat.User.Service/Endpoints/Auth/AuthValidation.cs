using CecoChat.Data;
using CecoChat.User.Contracts;
using FluentValidation;

namespace CecoChat.User.Service.Endpoints.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Registration)
            .NotNull();
        RuleFor(x => x.Registration.UserName)
            .ValidUserName();
        RuleFor(x => x.Registration.Password)
            .ValidPassword();
        RuleFor(x => x.Registration.DisplayName)
            .ValidDisplayName();
        RuleFor(x => x.Registration.AvatarUrl)
            .ValidAvatarUrl();
        RuleFor(x => x.Registration.Phone)
            .ValidPhone();
        RuleFor(x => x.Registration.Email)
            .ValidEmail();
    }
}

public sealed class AuthenticateRequestValidator : AbstractValidator<AuthenticateRequest>
{
    public AuthenticateRequestValidator()
    {
        RuleFor(x => x.UserName)
            .ValidUserName();
        RuleFor(x => x.Password)
            .ValidPassword();
    }
}
