using CecoChat.Data;
using CecoChat.Server.Identity;
using CecoChat.User.Contracts;
using FluentValidation;

namespace CecoChat.User.Service.Endpoints.Connections;

public sealed class InviteRequestValidator : AbstractValidator<InviteRequest>
{
    public InviteRequestValidator(IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.ConnectionId)
            .ValidUserId()
            .ConnectionIdDifferentFromUserId(httpContextAccessor);
    }
}

public sealed class ApproveRequestValidator : AbstractValidator<ApproveRequest>
{
    public ApproveRequestValidator(IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.ConnectionId)
            .ValidUserId()
            .ConnectionIdDifferentFromUserId(httpContextAccessor);
        RuleFor(x => x.Version.ToDateTime())
            .ValidVersion();
    }
}

public sealed class CancelRequestValidator : AbstractValidator<CancelRequest>
{
    public CancelRequestValidator(IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.ConnectionId)
            .ValidUserId()
            .ConnectionIdDifferentFromUserId(httpContextAccessor);
        RuleFor(x => x.Version.ToDateTime())
            .ValidVersion();
    }
}

public sealed class RemoveRequestValidator : AbstractValidator<RemoveRequest>
{
    public RemoveRequestValidator(IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.ConnectionId)
            .ValidUserId()
            .ConnectionIdDifferentFromUserId(httpContextAccessor);
        RuleFor(x => x.Version.ToDateTime())
            .ValidVersion();
    }
}

internal static class HttpContextValidationRules
{
    public static IRuleBuilderOptions<T, long> ConnectionIdDifferentFromUserId<T>(this IRuleBuilderOptions<T, long> ruleBuilder, IHttpContextAccessor httpContextAccessor)
    {
        return ruleBuilder
            .Must(connectionId => connectionId != httpContextAccessor.HttpContext!.GetUserClaimsHttp().UserId)
            .WithMessage("{PropertyName} should be different from current user ID.");
    }
}
