﻿using FluentValidation;

namespace CecoChat.DynamicConfig.Sections.History;

internal sealed class HistoryValidator : AbstractValidator<HistoryValues>
{
    public HistoryValidator()
    {
        RuleFor(x => x.MessageCount).InclusiveBetween(from: 16, to: 128);
    }
}
