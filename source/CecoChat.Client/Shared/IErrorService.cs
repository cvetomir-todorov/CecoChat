using System;

namespace CecoChat.Client.Shared
{
    public interface IErrorService
    {
        void ShowError(Exception exception);

        void ShowError(string error);
    }
}
