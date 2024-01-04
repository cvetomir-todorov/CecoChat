using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Files;

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
