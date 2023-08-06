using FluentValidation;

namespace CecoChat.Data;

public static class MessagingValidationRules
{
    public static IRuleBuilderOptions<T, long> ValidMessageId<T>(this IRuleBuilderInitial<T, long> ruleBuilder)
    {
        return ruleBuilder.GreaterThan(0);
    }
}
