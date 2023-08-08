using CecoChat.FluentValidation;
using FluentValidation;

namespace CecoChat.Data;

public static class ProfileValidationRules
{
    public static IRuleBuilderOptions<T, long> ValidUserId<T>(this IRuleBuilderInitial<T, long> ruleBuilder)
    {
        return ruleBuilder.GreaterThan(0);
    }

    public static IRuleBuilderOptions<IEnumerable<long>, long> ValidUserId(this IRuleBuilderInitialCollection<IEnumerable<long>, long> ruleBuilder)
    {
        return ruleBuilder.GreaterThan(0);
    }

    public static IRuleBuilderOptions<T, Guid> ValidVersion<T>(this IRuleBuilderInitial<T, Guid> ruleBuilder)
    {
        return ruleBuilder
            .Must(version => version != Guid.Empty)
            .WithMessage("{PropertyName} should not be an empty UUID/GUID but provided value '{PropertyValue}' is.");
    }

    public static IRuleBuilderOptions<T, string> ValidUserName<T>(this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Length(4, 32)
            .Must(userName => !userName.Any(char.IsWhiteSpace))
            .WithMessage("{PropertyName} should not contain white-space characters but provided value '{PropertyValue}' does.")
            .Must(userName => !userName.Any(char.IsUpper))
            .WithMessage("{PropertyName} should not contain upper-case characters but provided value '{PropertyValue}' does.");
    }

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Length(8, 128);
    }

    public static IRuleBuilderOptions<T, string> ValidDisplayName<T>(this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Length(4, 32);
    }

    public static IRuleBuilderOptions<T, string> ValidAvatarUrl<T>(this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .ValidUri();
    }

    public static IRuleBuilderOptions<T, string> ValidPhone<T>(this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Length(8, 16)
            .Must(phone => phone.All(char.IsDigit))
            .WithMessage("{PropertyName} should contain only digits but provided value '{PropertyValue}' doesn't.");
    }

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .EmailAddress();
    }
}
