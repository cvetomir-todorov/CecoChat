using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints;

public static class ValidationRules
{
    public static IRuleBuilderOptions<T, DateTime> ValidNewerThanDateTime<T>(this IRuleBuilderInitial<T, DateTime> ruleBuilder)
    {
        return ruleBuilder.GreaterThanOrEqualTo(Snowflake.Epoch);
    }

    public static IRuleBuilderOptions<T, long> ValidUserId<T>(this IRuleBuilderInitial<T, long> ruleBuilder)
    {
        return ruleBuilder.GreaterThan(0);
    }

    public static IRuleBuilderOptions<T, DateTime> ValidOlderThanDateTime<T>(this IRuleBuilderInitial<T, DateTime> ruleBuilder)
    {
        return ruleBuilder.GreaterThan(Snowflake.Epoch);
    }
}
