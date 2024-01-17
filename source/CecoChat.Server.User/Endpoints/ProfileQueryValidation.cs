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

public sealed class GetPublicProfilesByIdListRequestValidator : AbstractValidator<GetPublicProfilesByIdListRequest>
{
    public GetPublicProfilesByIdListRequestValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty()
            .Must(userIds => userIds.Count < ProfileConstants.UserIds.MaxCount)
            .WithMessage(ProfileConstants.UserIds.MaxCountError)
            .ForEach(userId => userId.ValidUserId());
    }
}

public sealed class GetPublicProfilesByPatternRequestValidator : AbstractValidator<GetPublicProfilesByPatternRequest>
{
    public GetPublicProfilesByPatternRequestValidator()
    {
        RuleFor(x => x.SearchPattern)
            .Matches(ProfileRegexes.UserNameSearchPatternRegex())
            .WithMessage(ProfileConstants.UserName.SearchPatternError);
    }
}
