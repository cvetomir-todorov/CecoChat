using Microsoft.Extensions.Hosting;

namespace CecoChat.Server.Bff;

public static class Program
{
    public static void Main(string[] args)
    {
        IHostBuilder hostBuilder = EntryPoint.CreateDefaultHostBuilder(args, typeof(Startup));
        EntryPoint.CreateAndRunHost(hostBuilder, typeof(Program));
    }
}