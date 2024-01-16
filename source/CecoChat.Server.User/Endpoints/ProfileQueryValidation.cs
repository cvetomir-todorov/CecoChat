using System.Text.RegularExpressions;
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
            .Must(userIds => userIds.Count < 128)
            .WithMessage("{PropertyName} count must not exceed 128, but {PropertyValue} was provided.")
            .ForEach(userId => userId.ValidUserId());
    }
}

public sealed class GetPublicProfilesByPatternRequestValidator : AbstractValidator<GetPublicProfilesByPatternRequest>
{
    public GetPublicProfilesByPatternRequestValidator()
    {
        RuleFor(x => x.SearchPattern)
            .NotEmpty()
            .Matches(ProfileQueryRegexes.UserNameSearchPatternRegex())
            .WithMessage("{PropertyName} should be a string with length [3, 32] which contains only characters, but '{PropertyValue}' was provided.");
    }
}

internal static partial class ProfileQueryRegexes
{
    [GeneratedRegex(
        pattern: "^([\\w]{3,32})$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    public static partial Regex UserNameSearchPatternRegex();
}
