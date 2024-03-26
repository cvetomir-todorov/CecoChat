using FluentValidation;
using FluentValidation.Validators;

namespace Common.FluentValidation;

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

#pragma warning disable IDE0060
    // dotnet format suggests to remove unused parameter which though is from an external interface
    public bool IsValid(ValidationContext<T> context, string value)
#pragma warning restore IDE0060
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Uri.TryCreate(value, _uriKind, out _);
    }

#pragma warning disable IDE0060
    // dotnet format suggests to remove unused parameter which though is from an external interface
    public string GetDefaultMessageTemplate(string errorCode)
#pragma warning restore IDE0060
    {
        return $"{{PropertyName}} must be a valid URI, but the attempted value {{PropertyValue}} is not.";
    }
}
