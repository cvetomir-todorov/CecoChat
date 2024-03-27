using CecoChat.Data;
using CecoChat.User.Contracts;
using FluentValidation;

namespace CecoChat.Server.User.Endpoints.Profiles;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .ValidPassword();
        RuleFor(x => x.Version.ToDateTime())
            .ValidVersion();
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Profile)
            .NotNull();
        RuleFor(x => x.Profile.DisplayName)
            .ValidDisplayName();
        RuleFor(x => x.Profile.Version.ToDateTime())
            .ValidVersion();
    }
}
