using CecoChat.Bff.Contracts.Files;
using CecoChat.Data;
using FluentValidation;

namespace CecoChat.Bff.Service.Endpoints.Files;

public sealed class GetUserFilesRequestValidator : AbstractValidator<GetUserFilesRequest>
{
    public GetUserFilesRequestValidator()
    {
        RuleFor(x => x.NewerThan)
            .ValidNewerThanDateTime();
    }
}
