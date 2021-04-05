using System.Diagnostics;

namespace CecoChat.Grpc.Instrumentation
{
    public interface IGrpcActivityUtility
    {
        ActivitySource ActivitySource { get; }

        void EnrichActivity(string service, string method, Activity activity);
    }

    internal sealed class GrpcActivityUtility : IGrpcActivityUtility
    {
        public ActivitySource ActivitySource => GrpcInstrumentation.ActivitySource;

        public void EnrichActivity(string service, string method, Activity activity)
        {
            if (activity != null && activity.IsAllDataRequested)
            {
                activity.SetTag(GrpcInstrumentation.Keys.TagRpcSystem, GrpcInstrumentation.Values.TagRpcSystemGrpc);
                activity.SetTag(GrpcInstrumentation.Keys.TagRpcService, service);
                activity.SetTag(GrpcInstrumentation.Keys.TagRpcMethod, method);
            }
        }
    }
}
