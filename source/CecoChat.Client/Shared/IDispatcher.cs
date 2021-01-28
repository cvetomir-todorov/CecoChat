using System;

namespace CecoChat.Client.Shared
{
    public interface IDispatcher
    {
        void Invoke(Action action);
    }
}
