using System.Diagnostics;
using OpenTelemetry.Trace;

namespace CecoChat.Tracing
{
    public interface IActivityUtility
    {
        /// <summary>
        /// By default calling <see cref="Activity.Stop"/> sets the <see cref="Activity.Current"/>
        /// to the <see cref="Activity.Parent"/> of the stopped activity which may not be desired.
        /// </summary>
        void Stop(Activity activity, bool success, bool relyOnDefaultPolicyOfSettingCurrentActivity = true);
    }

    public sealed class ActivityUtility : IActivityUtility
    {
        public void Stop(Activity activity, bool success, bool relyOnDefaultPolicyOfSettingCurrentActivity = true)
        {
            if (activity != null)
            {
                Status status = success ? Status.Ok : Status.Error;
                activity.SetStatus(status);

                Activity currentActivity = Activity.Current;
                activity.Stop();

                if (!relyOnDefaultPolicyOfSettingCurrentActivity)
                {
                    Activity.Current = currentActivity;
                }
            }
        }
    }
}
