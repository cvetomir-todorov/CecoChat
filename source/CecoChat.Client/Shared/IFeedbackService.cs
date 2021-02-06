using System;

namespace CecoChat.Client.Shared
{
    public interface IFeedbackService
    {
        void ShowInfo(string info);

        void ShowWarning(string warning);

        void ShowError(Exception exception);

        void ShowError(string error);
    }
}
