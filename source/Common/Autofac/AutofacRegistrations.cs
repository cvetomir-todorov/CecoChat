using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Common.Autofac;

/// <summary>
/// Typical registration but accomplished with Autofac.
/// </summary>
public static class AutofacRegistrations
{
    public static void RegisterHostedService<THostedService>(this ContainerBuilder builder)
        where THostedService : IHostedService
    {
        builder.RegisterType<THostedService>().As<IHostedService>().SingleInstance();
    }

    /// <summary>
    /// Requires calling services.AddOptions() in Startup.ConfigureServices
    /// </summary>
    public static void RegisterOptions<TOptions>(this ContainerBuilder builder, IConfiguration configuration)
        where TOptions : class
    {
        builder.Register(_ => new ConfigurationChangeTokenSource<TOptions>(Options.DefaultName, configuration))
            .As<IOptionsChangeTokenSource<TOptions>>()
            .SingleInstance();

        builder.Register(_ => new NamedConfigureFromConfigurationOptions<TOptions>(Options.DefaultName, configuration, _ => { }))
            .As<IConfigureOptions<TOptions>>()
            .SingleInstance();
    }

    public static void RegisterFactory<TImplementation, TService>(this ContainerBuilder builder)
        where TImplementation : notnull
        where TService : notnull
    {
        builder.RegisterType<TImplementation>().As<TService>().InstancePerDependency();
        builder.Register(componentContext =>
        {
            IComponentContext resolvedContext = componentContext.Resolve<IComponentContext>();
            Func<TService> factoryMethod = resolvedContext.Resolve<TService>;
            return factoryMethod;
        }).As<Func<TService>>().SingleInstance();
        builder.RegisterType<Factory<TService>>().As<IFactory<TService>>().SingleInstance();
    }
}
