using FluentValidation;

namespace CecoChat.Server.Bff.Endpoints;

public static class ValidationRules
{
    public static IRuleBuilderOptions<T, DateTime> ValidNewerThanDateTime<T>(this IRuleBuilderInitial<T, DateTime> ruleBuilder)
    {
        return ruleBuilder.GreaterThanOrEqualTo(Snowflake.Epoch);
    }
}
