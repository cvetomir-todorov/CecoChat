using System.Diagnostics;
using CecoChat.Tracing;

namespace CecoChat.Grpc.Instrumentation
{
    public interface IGrpcActivityUtility
    {
        Activity StartServiceMethod(string name, string service, string method, ActivityContext? parentContext);

        void Stop(Activity activity, bool operationSuccess);
    }

    internal sealed class GrpcActivityUtility : IGrpcActivityUtility
    {
        private readonly IActivityUtility _activityUtility;

        public GrpcActivityUtility(IActivityUtility activityUtility)
        {
            _activityUtility = activityUtility;
        }

        public Activity StartServiceMethod(string name, string service, string method, ActivityContext? parentContext)
        {
            Activity activity = _activityUtility.Start(
                name,
                GrpcInstrumentation.ActivitySource,
                ActivityKind.Producer,
                parentContext);

            if (activity.IsAllDataRequested)
            {
                activity.SetTag(GrpcInstrumentation.Keys.TagRpcSystem, GrpcInstrumentation.Values.TagRpcSystemGrpc);
                activity.SetTag(GrpcInstrumentation.Keys.TagRpcService, service);
                activity.SetTag(GrpcInstrumentation.Keys.TagRpcMethod, method);
            }

            return activity;
        }

        public void Stop(Activity activity, bool operationSuccess)
        {
            _activityUtility.Stop(activity, operationSuccess);
        }
    }
}
