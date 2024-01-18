using CecoChat.Contracts.User;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.User.Endpoints.Files;

public sealed class AssociateFileRequestValidator : AbstractValidator<AssociateFileRequest>
{
    public AssociateFileRequestValidator()
    {
        RuleFor(x => x.Bucket)
            .ValidBucketName();
        RuleFor(x => x.Path)
            .ValidPath();
    }
}
