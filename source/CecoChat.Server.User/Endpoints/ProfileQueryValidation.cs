using CecoChat.Contracts.User;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.User.Endpoints;

public sealed class GetPublicProfileRequestValidator : AbstractValidator<GetPublicProfileRequest>
{
    public GetPublicProfileRequestValidator()
    {
        RuleFor(x => x.UserId)
            .ValidUserId();
    }
}

public sealed class GetPublicProfilesRequestValidator : AbstractValidator<GetPublicProfilesRequest>
{
    public GetPublicProfilesRequestValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty()
            .Must(userIds => userIds.Count < 128)
            .WithMessage("{PropertyName} count must not exceed 128, but {PropertyValue} was provided.")
            .ForEach(userId => userId.ValidUserId());
    }
}
