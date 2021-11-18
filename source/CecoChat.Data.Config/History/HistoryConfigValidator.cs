using FluentValidation;

namespace CecoChat.Data.Config.History
{
    internal sealed class HistoryConfigValidator : AbstractValidator<HistoryConfigValues>
    {
        public HistoryConfigValidator(HistoryConfigUsage usage)
        {
            if (usage.UseMessageCount)
            {
                RuleFor(x => x.ChatMessageCount).InclusiveBetween(@from: 16, to: 128);
            }
        }
    }
}