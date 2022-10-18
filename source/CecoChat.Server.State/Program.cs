using Microsoft.Extensions.Hosting;

namespace CecoChat.Server.State;

public static class Program
{
    public static void Main(string[] args)
    {
        IHostBuilder hostBuilder = EntryPoint.CreateDefaultHostBuilder(args, startupContext: typeof(Startup));
        EntryPoint.CreateAndRunHost(hostBuilder, loggerContext: typeof(Program));
    }
}