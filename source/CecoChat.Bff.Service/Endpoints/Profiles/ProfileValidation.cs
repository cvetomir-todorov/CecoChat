using CecoChat.Data;
using CecoChat.DynamicConfig.Sections.User;
using FluentValidation;

namespace CecoChat.Bff.Service.Endpoints.Profiles;

public sealed class GetPublicProfilesRequestValidator : AbstractValidator<GetPublicProfilesRequest>
{
    public GetPublicProfilesRequestValidator(IUserConfig userConfig)
    {
        RuleFor(x => x)
            .Must(x =>
            {
                bool isSearchPatternMissing = string.IsNullOrWhiteSpace(x.SearchPattern);
                bool areUserIdsMissing = x.UserIds.Length == 0;

                bool isValid = (isSearchPatternMissing && !areUserIdsMissing) || (!isSearchPatternMissing && areUserIdsMissing);
                return isValid;
            })
            .WithMessage("Either user IDs or a search pattern should be specified.");
        RuleFor(x => x.UserIds)
            .Must(userIds => userIds.Length < userConfig.ProfileCount)
            .WithMessage(ProfileConstants.UserIds.MaxCountError)
            .ForEach(userId => userId.ValidUserId());
        RuleFor(x => x.SearchPattern)
            .Matches(ProfileRegexes.UserNameSearchPatternRegex())
            .When(request => !string.IsNullOrEmpty(request.SearchPattern))
            .WithMessage(ProfileConstants.UserName.SearchPatternError);
    }
}
