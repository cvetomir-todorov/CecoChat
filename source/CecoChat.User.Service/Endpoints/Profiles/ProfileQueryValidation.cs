using CecoChat.Data;
using CecoChat.DynamicConfig.Sections.User;
using CecoChat.User.Contracts;
using FluentValidation;

namespace CecoChat.User.Service.Endpoints.Profiles;

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
    public GetPublicProfilesByIdListRequestValidator(IUserConfig userConfig)
    {
        RuleFor(x => x.UserIds)
            .NotEmpty()
            .Must(userIds => userIds.Count <= userConfig.ProfileCount)
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
