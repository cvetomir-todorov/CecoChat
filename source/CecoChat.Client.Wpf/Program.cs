using System;
using CecoChat.Client.Wpf.Infrastructure;

namespace CecoChat.Client.Wpf
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            DIContainer.Initialize();
            new App().Run();
        }
    }
}
