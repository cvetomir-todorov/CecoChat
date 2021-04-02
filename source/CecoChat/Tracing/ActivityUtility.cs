using System.Diagnostics;

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
    }
}
