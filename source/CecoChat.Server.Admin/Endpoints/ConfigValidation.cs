using CecoChat.Contracts.Admin;
using FluentValidation;

namespace CecoChat.Server.Admin.Endpoints;

public sealed class UpdateConfigElementsRequestValidator : AbstractValidator<UpdateConfigElementsRequest>
{
    public UpdateConfigElementsRequestValidator()
    {
        ConfigElementValidator configElementValidator = new();

        RuleFor(x => x.ExistingElements)
            .ForEach(element => element.SetValidator(configElementValidator));
        RuleFor(x => x.NewElements)
            .ForEach(element => element.SetValidator(configElementValidator));
        RuleFor(x => x)
            .Must(request => request.ExistingElements.Length + request.NewElements.Length > 0)
            .WithMessage("There should be at least 1 existing config element updated or new config element added when updating config.");
    }
}

public sealed class ConfigElementValidator : AbstractValidator<ConfigElement>
{
    public ConfigElementValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Matches("^([a-z\\-]{2,16}\\.[a-z\\-\\.]{2,128})$")
            .WithMessage("{PropertyName} should be a correct config element name such as 'section.sub-section' or 'section.sub-section.element', but '{PropertyValue}' was provided.");
        RuleFor(x => x.Value)
            .NotEmpty();
    }
}
