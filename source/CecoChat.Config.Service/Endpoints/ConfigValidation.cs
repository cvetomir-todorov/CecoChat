using System.Text.RegularExpressions;
using CecoChat.Config.Contracts;
using FluentValidation;

namespace CecoChat.Config.Service.Endpoints;

public sealed class UpdateConfigElementsRequestValidator : AbstractValidator<UpdateConfigElementsRequest>
{
    public UpdateConfigElementsRequestValidator()
    {
        ConfigElementValidator nameAndValueConfigElementValidator = new(validateName: true, validateValue: true);
        ConfigElementValidator nameOnlyConfigElementValidator = new(validateName: true, validateValue: false);

        RuleFor(x => x.ExistingElements)
            .ForEach(element => element.SetValidator(nameAndValueConfigElementValidator));
        RuleFor(x => x.NewElements)
            .ForEach(element => element.SetValidator(nameAndValueConfigElementValidator));
        RuleFor(x => x.DeletedElements)
            .ForEach(element => element.SetValidator(nameOnlyConfigElementValidator));
        RuleFor(x => x)
            .Must(request => request.ExistingElements.Length + request.NewElements.Length + request.DeletedElements.Length > 0)
            .WithMessage("There should be at least 1: a) existing config element updated or b) new config element added or c) existing config element deleted ...when updating config.");
    }
}

public sealed class ConfigElementValidator : AbstractValidator<ConfigElement>
{
    public ConfigElementValidator(bool validateName, bool validateValue)
    {
        if (validateName)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .Matches(ConfigRegexes.ConfigElementNameRegex())
                .WithMessage("{PropertyName} should be a correct config element name such as 'section.sub-section' or 'section.sub-section.element', but '{PropertyValue}' was provided.");
        }
        if (validateValue)
        {
            RuleFor(x => x.Value)
                .NotEmpty();
        }
    }
}

public sealed class GetConfigElementsRequestValidator : AbstractValidator<GetConfigElementsRequest>
{
    public GetConfigElementsRequestValidator()
    {
        RuleFor(x => x.ConfigSection)
            .NotEmpty()
            .Matches(ConfigRegexes.ConfigSectionNameRegex())
            .WithMessage("{PropertyName} should be a correct config section such as 'partitioning', but {PropertyValue} was provided.");
    }
}

internal static partial class ConfigRegexes
{
    [GeneratedRegex(
        pattern: "^([a-z\\-]{2,16}\\.[a-z\\-\\.]{2,128})$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    public static partial Regex ConfigElementNameRegex();

    [GeneratedRegex(
        pattern: "^([a-z\\-]{2,16})$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    public static partial Regex ConfigSectionNameRegex();
}
