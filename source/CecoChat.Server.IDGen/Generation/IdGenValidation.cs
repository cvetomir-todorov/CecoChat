using CecoChat.Contracts.IDGen;
using FluentValidation;

namespace CecoChat.Server.IDGen.Generation;

public sealed class GenerateOneRequestValidator : AbstractValidator<GenerateOneRequest>
{
    public GenerateOneRequestValidator()
    {
        RuleFor(x => x.OriginatorId).GreaterThan(0);
    }
}

public sealed class GenerateManyRequestValidator : AbstractValidator<GenerateManyRequest>
{
    public GenerateManyRequestValidator()
    {
        RuleFor(x => x.OriginatorId).GreaterThan(0);
        RuleFor(x => x.Count).InclusiveBetween(2, 16384);
    }
}
