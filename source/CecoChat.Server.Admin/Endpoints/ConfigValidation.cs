using System.Text.RegularExpressions;
using CecoChat.Contracts.Admin;
using FluentValidation;

namespace CecoChat.Server.Admin.Endpoints;

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
    private static readonly Regex NameRegex = new("^([a-z\\-]{2,16}\\.[a-z\\-\\.]{2,128})$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ConfigElementValidator(bool validateName, bool validateValue)
    {
        if (validateName)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .Matches(NameRegex)
                .WithMessage("{PropertyName} should be a correct config element name such as 'section.sub-section' or 'section.sub-section.element', but '{PropertyValue}' was provided.");
        }
        if (validateValue)
        {
            RuleFor(x => x.Value)
                .NotEmpty();
        }
    }
}
