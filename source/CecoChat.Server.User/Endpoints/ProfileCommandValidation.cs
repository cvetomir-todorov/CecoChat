using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.User.Endpoints;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .ValidPassword();
        RuleFor(x => x.Version.ToGuid())
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
        RuleFor(x => x.Profile.Version.ToGuid())
            .ValidVersion();
    }
}
