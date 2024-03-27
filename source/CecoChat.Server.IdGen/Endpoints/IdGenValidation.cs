using CecoChat.IdGen.Contracts;
using FluentValidation;

namespace CecoChat.Server.IdGen.Endpoints;

public sealed class GenerateManyRequestValidator : AbstractValidator<GenerateManyRequest>
{
    public GenerateManyRequestValidator()
    {
        RuleFor(x => x.Count)
            .InclusiveBetween(2, 16384);
    }
}
