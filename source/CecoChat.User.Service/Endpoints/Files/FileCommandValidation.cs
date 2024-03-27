using CecoChat.Data;
using CecoChat.User.Contracts;
using FluentValidation;

namespace CecoChat.User.Service.Endpoints.Files;

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

public sealed class AddFileAccessRequestValidator : AbstractValidator<AddFileAccessRequest>
{
    public AddFileAccessRequestValidator()
    {
        RuleFor(x => x.Bucket)
            .ValidBucketName();
        RuleFor(x => x.Path)
            .ValidPath();
        RuleFor(x => x.AllowedUserId)
            .ValidUserId();
        RuleFor(x => x.Version.ToDateTime())
            .ValidVersion();
    }
}
