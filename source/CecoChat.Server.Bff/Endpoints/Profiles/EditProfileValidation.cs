using CecoChat.Bff.Contracts.Profiles;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Profiles;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .ValidPassword();
        RuleFor(x => x.Version)
            .ValidVersion();
    }
}

public sealed class EditProfileRequestValidator : AbstractValidator<EditProfileRequest>
{
    public EditProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .ValidDisplayName();
        RuleFor(x => x.Version)
            .ValidVersion();
    }
}
