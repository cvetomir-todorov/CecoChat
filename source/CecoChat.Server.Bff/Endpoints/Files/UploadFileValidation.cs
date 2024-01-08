using CecoChat.Server.Bff.Files;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Bff.Endpoints.Files;

public sealed class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    public UploadFileRequestValidator(IOptions<FilesOptions> options)
    {
        FilesOptions filesOptions = options.Value;

        RuleFor(x => x.FileSize)
            .InclusiveBetween(1, filesOptions.MaxUploadedFileBytes)
            .WithMessage($"{{PropertyName}} should be a value within [1, {filesOptions.MaxUploadedFileBytes}] but provided value '{{PropertyValue}}' is not.");
    }
}
