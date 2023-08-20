using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.User.Endpoints;

public sealed class InviteRequestValidator : AbstractValidator<InviteRequest>
{
    public InviteRequestValidator()
    {
        RuleFor(x => x.ConnectionId)
            .ValidUserId();
    }
}

public sealed class ApproveRequestValidator : AbstractValidator<ApproveRequest>
{
    public ApproveRequestValidator()
    {
        RuleFor(x => x.ConnectionId)
            .ValidUserId();
        RuleFor(x => x.Version.ToGuid())
            .ValidVersion();
    }
}

public sealed class CancelRequestValidator : AbstractValidator<CancelRequest>
{
    public CancelRequestValidator()
    {
        RuleFor(x => x.ConnectionId)
            .ValidUserId();
        RuleFor(x => x.Version.ToGuid())
            .ValidVersion();
    }
}

public sealed class RemoveRequestValidator : AbstractValidator<RemoveRequest>
{
    public RemoveRequestValidator()
    {
        RuleFor(x => x.ConnectionId)
            .ValidUserId();
        RuleFor(x => x.Version.ToGuid())
            .ValidVersion();
    }
}
