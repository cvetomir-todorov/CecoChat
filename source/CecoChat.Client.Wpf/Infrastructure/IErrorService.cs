using System;

namespace CecoChat.Client.Wpf.Infrastructure
{
    public interface IErrorService
    {
        void ShowError(Exception exception);

        void ShowError(string error);
    }
}
