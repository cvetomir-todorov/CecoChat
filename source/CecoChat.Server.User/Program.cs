namespace CecoChat.Server.User;

public static class Program
{
    public static async Task Main(string[] args)
    {
        IHostBuilder hostBuilder = EntryPoint.CreateDefaultHostBuilder(args, startupContext: typeof(Startup));
        await EntryPoint.CreateAndRunHost(hostBuilder, loggerContext: typeof(Program));
    }
}
