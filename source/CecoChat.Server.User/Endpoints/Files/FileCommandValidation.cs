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
