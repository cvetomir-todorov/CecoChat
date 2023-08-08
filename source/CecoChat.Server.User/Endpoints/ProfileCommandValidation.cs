using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.User.Endpoints;

public sealed class CreateProfileRequestValidator : AbstractValidator<CreateProfileRequest>
{
    public CreateProfileRequestValidator()
    {
        RuleFor(x => x.Profile)
            .NotNull();
        RuleFor(x => x.Profile.UserName)
            .ValidUserName();
        RuleFor(x => x.Profile.Password)
            .ValidPassword();
        RuleFor(x => x.Profile.DisplayName)
            .ValidDisplayName();
        RuleFor(x => x.Profile.AvatarUrl)
            .ValidAvatarUrl();
        RuleFor(x => x.Profile.Phone)
            .ValidPhone();
        RuleFor(x => x.Profile.Email)
            .ValidEmail();
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.Profile)
            .NotNull();
        RuleFor(x => x.Profile.NewPassword)
            .ValidPassword();
        RuleFor(x => x.Profile.Version.ToGuid())
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
