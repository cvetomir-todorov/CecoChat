using CecoChat.Contracts.Admin;
using FluentValidation;

namespace CecoChat.Server.Admin.Endpoints;

public sealed class UpdateConfigElementsRequestValidator : AbstractValidator<UpdateConfigElementsRequest>
{
    public UpdateConfigElementsRequestValidator()
    {
        ConfigElementValidator configElementValidator = new();

        RuleFor(x => x.Elements)
            .NotEmpty()
            .ForEach(element => element.SetValidator(configElementValidator));
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
