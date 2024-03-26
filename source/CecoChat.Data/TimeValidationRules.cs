using Common;
using FluentValidation;

namespace CecoChat.Data;

public static class TimeValidationRules
{
    public static IRuleBuilderOptions<T, DateTime> ValidNewerThanDateTime<T>(this IRuleBuilderInitial<T, DateTime> ruleBuilder)
    {
        return ruleBuilder.GreaterThanOrEqualTo(Snowflake.Epoch);
    }

    public static IRuleBuilderOptions<T, DateTime> ValidOlderThanDateTime<T>(this IRuleBuilderInitial<T, DateTime> ruleBuilder)
    {
        return ruleBuilder.GreaterThan(Snowflake.Epoch);
    }
}
