using CecoChat.Contracts.Bff;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .ValidPassword();
        RuleFor(x => x.Version)
            .NotEmpty();
    }
}

public sealed class EditProfileRequestValidator : AbstractValidator<EditProfileRequest>
{
    public EditProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .ValidDisplayName();
        RuleFor(x => x.Version)
            .NotEmpty();
    }
}
