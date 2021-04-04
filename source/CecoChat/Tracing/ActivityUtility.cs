using System.Diagnostics;
using OpenTelemetry.Trace;

namespace CecoChat.Tracing
{
    public interface IActivityUtility
    {
        Activity Start(string name, ActivityKind kind);
        Activity StartClient(string name);
        Activity StartServer(string name);
        Activity StartProducer(string name);
        Activity StartConsumer(string name);
        Activity StartInternal(string name);

        /// <summary>
        /// By default calling <see cref="Activity.Stop"/> sets the <see cref="Activity.Current"/>
        /// to the <see cref="Activity.Parent"/> of the stopped activity which may not be desired.
        /// </summary>
        void Stop(Activity activity, bool success, bool relyOnDefaultPolicyOfSettingCurrentActivity = true);
    }

    public sealed class ActivityUtility : IActivityUtility
    {
        public Activity Start(string name, ActivityKind kind)
        {
            return Activity.Current?.Source.StartActivity(name, kind, Activity.Current.Context);
        }

        public Activity StartClient(string name)
        {
            return Start(name, ActivityKind.Client);
        }

        public Activity StartServer(string name)
        {
            return Start(name, ActivityKind.Server);
        }

        public Activity StartProducer(string name)
        {
            return Start(name, ActivityKind.Producer);
        }

        public Activity StartConsumer(string name)
        {
            return Start(name, ActivityKind.Consumer);
        }

        public Activity StartInternal(string name)
        {
            return Start(name, ActivityKind.Internal);
        }

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
