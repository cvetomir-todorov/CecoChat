using System;
using System.Windows;

namespace CecoChat.Client.Wpf.Infrastructure
{
    public interface IFeedbackService
    {
        void ShowInfo(string info);

        void ShowWarning(string warning);

        void ShowError(Exception exception);

        void ShowError(string error);
    }

    public sealed class WpfFeedbackService : IFeedbackService
    {
        private readonly IDispatcher _dispatcher;

        public WpfFeedbackService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void ShowInfo(string info)
        {
            Show(info, "Info", MessageBoxImage.Information);
        }

        public void ShowWarning(string warning)
        {
            Show(warning, "Warning", MessageBoxImage.Warning);
        }

        public void ShowError(Exception exception)
        {
            Show(exception.ToString(), "Error", MessageBoxImage.Error);
        }

        public void ShowError(string error)
        {
            Show(error, "Error", MessageBoxImage.Error);
        }

        private void Show(string text, string caption, MessageBoxImage image)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            _dispatcher.Invoke(() => MessageBox.Show(
                Application.Current.MainWindow, text, caption, MessageBoxButton.OK, image, MessageBoxResult.OK));
        }
    }
}
