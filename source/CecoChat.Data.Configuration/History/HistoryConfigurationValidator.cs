using System;
using FluentValidation;

namespace CecoChat.Data.Configuration.History
{
    internal sealed class HistoryConfigurationValidator : AbstractValidator<HistoryConfigurationValues>
    {
        public HistoryConfigurationValidator(HistoryConfigurationUsage usage)
        {
            if (usage.UseServerAddress)
            {
                RuleFor(x => x.ServerAddress)
                    .NotEmpty()
                    .Must(address => Uri.TryCreate(address, UriKind.Absolute, out _))
                    .WithMessage("{PropertyName} should be a valid URL but '{PropertyValue}' provided instead.");
            }
            if (usage.UseMessageCount)
            {
                RuleFor(x => x.UserMessageCount).InclusiveBetween(@from: 16, to: 128);
                RuleFor(x => x.DialogMessageCount).InclusiveBetween(@from: 16, to: 128);
            }
        }
    }
}