using CecoChat.Contracts.User;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.User.Endpoints.Files;

public sealed class GetUserFilesRequestValidator : AbstractValidator<GetUserFilesRequest>
{
    public GetUserFilesRequestValidator()
    {
        RuleFor(x => x.NewerThan.ToDateTime())
            .ValidNewerThanDateTime();
    }
}
