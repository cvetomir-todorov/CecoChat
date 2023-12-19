using Autofac;

namespace CecoChat.AspNet.Init;

public static class AutofacRegistrations
{
    public static void RegisterInitStep<TInitStep>(this ContainerBuilder builder)
        where TInitStep : InitStep
    {
        builder.RegisterType<TInitStep>().As<InitStep>().SingleInstance();
    }
}
