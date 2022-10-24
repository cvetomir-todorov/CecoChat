using System.Diagnostics;
using CecoChat.Otel;
using CecoChat.Tracing;

namespace CecoChat.Grpc.Instrumentation;

public interface IGrpcStreamingActivityUtility
{
    Activity StartStreaming(string name, string service, string method, ActivityContext? parentContext);

    void Stop(Activity activity, bool operationSuccess);
}

internal sealed class GrpcStreamingActivityUtility : IGrpcStreamingActivityUtility
{
    private readonly IActivityUtility _activityUtility;

    public GrpcStreamingActivityUtility(IActivityUtility activityUtility)
    {
        _activityUtility = activityUtility;
    }

    public Activity StartStreaming(string name, string service, string method, ActivityContext? parentContext)
    {
        Activity activity = _activityUtility.Start(
            name,
            GrpcStreamingInstrumentation.ActivitySource,
            ActivityKind.Producer,
            parentContext);

        if (activity.IsAllDataRequested)
        {
            activity.SetTag(OtelInstrumentation.Keys.RpcSystem, OtelInstrumentation.Values.RpcSystemGrpc);
            activity.SetTag(OtelInstrumentation.Keys.RpcService, service);
            activity.SetTag(OtelInstrumentation.Keys.RpcMethod, method);
        }

        return activity;
    }

    public void Stop(Activity activity, bool operationSuccess)
    {
        _activityUtility.Stop(activity, operationSuccess);
    }
}