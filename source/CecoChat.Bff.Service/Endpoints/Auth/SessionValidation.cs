using CecoChat.Bff.Contracts.Auth;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Bff.Service.Endpoints.Auth;

public sealed class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionRequestValidator()
    {
        RuleFor(x => x.UserName)
            .ValidUserName();
        RuleFor(x => x.Password)
            .ValidPassword();
    }
}
