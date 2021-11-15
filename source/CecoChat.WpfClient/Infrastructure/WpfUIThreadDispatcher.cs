using System;
using System.Windows;

namespace CecoChat.Client.Wpf.Infrastructure
{
    public interface IDispatcher
    {
        void Invoke(Action action);
    }

    public sealed class WpfUIThreadDispatcher : IDispatcher
    {
        public void Invoke(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }
    }
}
