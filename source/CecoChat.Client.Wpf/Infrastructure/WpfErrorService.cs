using System;
using System.Windows;

namespace CecoChat.Client.Wpf.Infrastructure
{
    public sealed class WpfErrorService : IErrorService
    {
        private readonly IDispatcher _dispatcher;

        public WpfErrorService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void ShowError(Exception exception)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            _dispatcher.Invoke(() => MessageBox.Show(
                Application.Current.MainWindow, exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error));
        }

        public void ShowError(string error)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            _dispatcher.Invoke(() => MessageBox.Show(
                Application.Current.MainWindow, error, "Error", MessageBoxButton.OK, MessageBoxImage.Error));
        }
    }
}
