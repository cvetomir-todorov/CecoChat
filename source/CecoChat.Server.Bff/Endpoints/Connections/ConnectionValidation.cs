using CecoChat.Bff.Contracts.Connections;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Connections;

public sealed class ApproveConnectionRequestValidator : AbstractValidator<ApproveConnectionRequest>
{
    public ApproveConnectionRequestValidator()
    {
        RuleFor(x => x.Version)
            .ValidVersion();
    }
}

public sealed class CancelConnectionRequestValidator : AbstractValidator<CancelConnectionRequest>
{
    public CancelConnectionRequestValidator()
    {
        RuleFor(x => x.Version)
            .ValidVersion();
    }
}

public sealed class RemoveConnectionRequestValidator : AbstractValidator<RemoveConnectionRequest>
{
    public RemoveConnectionRequestValidator()
    {
        RuleFor(x => x.Version)
            .ValidVersion();
    }
}
