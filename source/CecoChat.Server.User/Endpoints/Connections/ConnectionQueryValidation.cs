using CecoChat.Data;
using CecoChat.User.Contracts;
using FluentValidation;

namespace CecoChat.Server.User.Endpoints.Connections;

public sealed class GetConnectionRequestValidator : AbstractValidator<GetConnectionRequest>
{
    public GetConnectionRequestValidator()
    {
        RuleFor(x => x.ConnectionId)
            .ValidUserId();
    }
}
