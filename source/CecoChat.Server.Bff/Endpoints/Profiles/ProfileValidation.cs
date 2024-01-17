using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Profiles;

public sealed class GetPublicProfilesRequestValidator : AbstractValidator<GetPublicProfilesRequest>
{
    public GetPublicProfilesRequestValidator()
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
            .Must(userIds => userIds.Length < ProfileConstants.UserIds.MaxCount)
            .WithMessage(ProfileConstants.UserIds.MaxCountError)
            .ForEach(userId => userId.ValidUserId());
        RuleFor(x => x.SearchPattern)
            .Matches(ProfileRegexes.UserNameSearchPatternRegex())
            .When(request => !string.IsNullOrEmpty(request.SearchPattern))
            .WithMessage(ProfileConstants.UserName.SearchPatternError);
    }
}
