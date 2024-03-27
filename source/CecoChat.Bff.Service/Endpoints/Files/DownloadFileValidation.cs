using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Bff.Service.Endpoints.Files;

public sealed class DownloadFileRequestValidator : AbstractValidator<DownloadFileRequest>
{
    public DownloadFileRequestValidator()
    {
        RuleFor(x => x.BucketUrlDecoded)
            .ValidBucketName();
        RuleFor(x => x.PathUrlDecoded)
            .ValidPath();
    }
}
