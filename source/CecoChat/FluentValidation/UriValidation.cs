using FluentValidation;
using FluentValidation.Validators;

namespace CecoChat.FluentValidation;

public static class UriValidation
{
    public static IRuleBuilderOptions<T, string> ValidUri<T>(this IRuleBuilderOptions<T, string> ruleBuilder, UriKind uriKind = UriKind.Absolute)
    {
        return ruleBuilder.SetValidator(new UriPropertyValidator<T>(uriKind));
    }
}

public class UriPropertyValidator<T> : IPropertyValidator<T, string>
{
    private readonly UriKind _uriKind;

    public UriPropertyValidator(UriKind uriKind = UriKind.Absolute)
    {
        _uriKind = uriKind;
    }

    public string Name => "UriPropertyValidator";

    public bool IsValid(ValidationContext<T> context, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Uri.TryCreate(value, _uriKind, out _);
    }

    public string GetDefaultMessageTemplate(string errorCode)
    {
        return $"{{PropertyName}} must be a valid URI, but the attempted value {{PropertyValue}} is not.";
    }
}
