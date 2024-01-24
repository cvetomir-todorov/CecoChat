using CecoChat.Contracts.Bff.Files;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Files;

public sealed class AddFileAccessRouteValidator : AbstractValidator<AddFileAccessRoute>
{
    public AddFileAccessRouteValidator()
    {
        RuleFor(x => x.BucketUrlDecoded)
            .ValidBucketName();
        RuleFor(x => x.PathUrlDecoded)
            .ValidPath();
    }
}

public sealed class AddFileAccessRequestValidator : AbstractValidator<AddFileAccessRequest>
{
    public AddFileAccessRequestValidator()
    {
        RuleFor(x => x.AllowedUserId)
            .ValidUserId();
        RuleFor(x => x.Version)
            .ValidVersion();
    }
}
