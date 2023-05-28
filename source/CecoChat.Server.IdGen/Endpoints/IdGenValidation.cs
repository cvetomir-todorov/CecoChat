using CecoChat.Contracts.IdGen;
using FluentValidation;

namespace CecoChat.Server.IDGen.Endpoints;

public sealed class GenerateOneRequestValidator : AbstractValidator<GenerateOneRequest>
{
    public GenerateOneRequestValidator()
    {
        RuleFor(x => x.OriginatorId).GreaterThanOrEqualTo(0);
    }
}

public sealed class GenerateManyRequestValidator : AbstractValidator<GenerateManyRequest>
{
    public GenerateManyRequestValidator()
    {
        RuleFor(x => x.OriginatorId).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Count).InclusiveBetween(2, 16384);
    }
}
