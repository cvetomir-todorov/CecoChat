using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Files;

public sealed class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    public UploadFileRequestValidator()
    {
        RuleFor(x => x.FileSize)
            .InclusiveBetween(1, 512 * 1024) // 512 KB
            .WithMessage($"{{PropertyName}} should be a value within [0, {512*1024}] but provided value '{{PropertyValue}}' is not.");
    }
}
