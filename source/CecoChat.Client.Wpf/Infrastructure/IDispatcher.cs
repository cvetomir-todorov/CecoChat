using System;

namespace CecoChat.Client.Wpf.Infrastructure
{
    public interface IDispatcher
    {
        void Invoke(Action action);
    }
}
