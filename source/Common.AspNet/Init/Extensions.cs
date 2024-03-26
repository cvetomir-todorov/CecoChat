using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Common.AspNet.Init;

public static class Extensions
{
    public static void RegisterInitStep<TInitStep>(this ContainerBuilder builder)
        where TInitStep : InitStep
    {
        builder.RegisterType<TInitStep>().As<InitStep>().SingleInstance();
    }

    public static async Task<bool> Init(this IServiceProvider serviceProvider)
    {
        IEnumerable<InitStep> initSteps = serviceProvider.GetServices<InitStep>();

        foreach (InitStep initStep in initSteps)
        {
            bool success = await initStep.Execute();
            if (!success)
            {
                return false;
            }
        }

        return true;
    }
}
