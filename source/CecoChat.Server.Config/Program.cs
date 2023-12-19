namespace CecoChat.Server.Config;

public static class Program
{
    public static async Task Main(string[] args)
    {
        IHostBuilder hostBuilder = EntryPoint.CreateDefaultHostBuilder(args, typeof(Startup));
        await EntryPoint.CreateAndRunHost(hostBuilder, typeof(Program));
    }
}
