using FluentValidation;

namespace CecoChat.Config.Sections.History;

internal sealed class HistoryValidator : AbstractValidator<HistoryValues>
{
    public HistoryValidator()
    {
        RuleFor(x => x.MessageCount).InclusiveBetween(from: 4, to: 128);
    }
}
