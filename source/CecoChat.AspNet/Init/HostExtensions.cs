using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.AspNet.Init;

public static class HostExtensions
{
    public static async Task<bool> Init(this IHost host)
    {
        IEnumerable<InitStep> initSteps = host.Services.GetServices<InitStep>();

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
