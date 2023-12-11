namespace CecoChat.Server.Admin;

public static class Program
{
    public static void Main(string[] args)
    {
        IHostBuilder hostBuilder = EntryPoint.CreateDefaultHostBuilder(args, typeof(Startup));
        EntryPoint.CreateAndRunHost(hostBuilder, typeof(Program));
    }
}
