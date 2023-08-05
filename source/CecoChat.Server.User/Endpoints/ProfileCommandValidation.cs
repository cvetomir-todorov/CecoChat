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
