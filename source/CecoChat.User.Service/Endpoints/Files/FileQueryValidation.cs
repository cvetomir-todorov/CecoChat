using CecoChat.Data;
using CecoChat.User.Contracts;
using FluentValidation;

namespace CecoChat.User.Service.Endpoints.Files;

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
