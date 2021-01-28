using System;
using CecoChat.Client.Shared;
using CecoChat.Client.ViewModels;
using CecoChat.Client.Wpf.Infrastructure;
using CecoChat.Client.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.Wpf
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            IServiceProvider serviceProvider = BuildServiceProvider();
            App app = new();
            MainWindow mainWindow = new();

            mainWindow.DataContext = serviceProvider.GetRequiredService<MainViewModel>();
            app.Run(mainWindow);
        }

        private static IServiceProvider BuildServiceProvider()
        {
            ServiceCollection services = new();

            services.AddSingleton<IDispatcher, WpfUIThreadDispatcher>();
            services.AddSingleton<IErrorService, WpfErrorService>();
            services.AddClientSharedServices();

            return services.BuildServiceProvider();
        }
    }
}
