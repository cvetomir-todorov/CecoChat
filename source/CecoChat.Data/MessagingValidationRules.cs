using FluentValidation;

namespace CecoChat.Data;

public static class MessagingValidationRules
{
    public static IRuleBuilderOptions<T, long> ValidMessageId<T>(this IRuleBuilderInitial<T, long> ruleBuilder)
    {
        return ruleBuilder.GreaterThan(0);
    }

    public static IRuleBuilderOptions<T, string> ValidMessageText<T>(this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Length(1, 128);
    }

    public static IRuleBuilderOptions<T, string> ValidReaction<T>(this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(8);
    }
}
