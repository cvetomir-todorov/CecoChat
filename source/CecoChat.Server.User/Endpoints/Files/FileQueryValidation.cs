using CecoChat.Contracts.User;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.User.Endpoints.Files;

public sealed class GetUserFilesRequestValidator : AbstractValidator<GetUserFilesRequest>
{
    public GetUserFilesRequestValidator()
    {
        RuleFor(x => x.NewerThan.ToDateTime())
            .ValidNewerThanDateTime();
    }
}

public sealed class HasUserFileAccessRequestValidator : AbstractValidator<HasUserFileAccessRequest>
{
    public HasUserFileAccessRequestValidator()
    {
        RuleFor(x => x.Bucket)
            .ValidBucketName();
        RuleFor(x => x.Path)
            .ValidPath();
    }
}
