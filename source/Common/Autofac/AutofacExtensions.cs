using Autofac;
using Autofac.Builder;
using Autofac.Core;

namespace Common.Autofac;

/// <summary>
/// Shortcuts for Autofac functionality.
/// </summary>
public static class AutofacExtensions
{
    public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle>
        WithKeyedParameter<TLimit, TActivatorData, TRegistrationStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> builder,
            Type parameterType,
            object parameterKey)
        where TActivatorData : ReflectionActivatorData
    {
        return builder.WithParameter(
            new ResolvedParameter(
                (parameterInfo, _) => parameterInfo.ParameterType == parameterType,
                (_, componentContext) => componentContext.ResolveKeyed(parameterKey, parameterType)));
    }

    public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle>
        WithNamedParameter<TLimit, TActivatorData, TRegistrationStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> builder,
            Type parameterType,
            string parameterName)
        where TActivatorData : ReflectionActivatorData
    {
        return builder.WithParameter(
            new ResolvedParameter(
                (parameterInfo, _) => parameterInfo.ParameterType == parameterType,
                (_, componentContext) => componentContext.ResolveNamed(parameterName, parameterType)));
    }
}
