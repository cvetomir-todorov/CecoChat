using Autofac;
using Common.Autofac;

namespace Common.AspNet.Init;

public static class Extensions
{
    public static void RegisterInitStep<TInitStep>(this ContainerBuilder builder)
        where TInitStep : InitStep
    {
        builder.RegisterType<TInitStep>().As<InitStep>().SingleInstance();
    }

    public static void RegisterInit(this ContainerBuilder builder)
    {
        builder.RegisterHostedService<InitHostedService>();
    }
}
