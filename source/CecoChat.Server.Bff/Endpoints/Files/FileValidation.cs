using CecoChat.Contracts.Bff.Files;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints.Files;

public sealed class GetUserFilesRequestValidator : AbstractValidator<GetUserFilesRequest>
{
    public GetUserFilesRequestValidator()
    {
        RuleFor(x => x.NewerThan)
            .ValidNewerThanDateTime();
    }
}
