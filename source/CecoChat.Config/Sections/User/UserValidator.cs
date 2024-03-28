using FluentValidation;

namespace CecoChat.Config.Sections.User;

internal sealed class UserValidator : AbstractValidator<UserValues>
{
    public UserValidator()
    {
        RuleFor(x => x.ProfileCount).InclusiveBetween(from: 1, to: 128);
    }
}
