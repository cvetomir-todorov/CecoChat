using System.Diagnostics;
using CecoChat.Otel;

namespace CecoChat.Grpc.Telemetry;

public interface IGrpcStreamTelemetry
{
    Activity StartStream(string name, string service, string method, ActivityContext? parentContext);

    void StopStream(Activity activity, bool operationSuccess);
}

internal sealed class GrpcStreamTelemetry : IGrpcStreamTelemetry
{
    private readonly ITelemetry _telemetry;

    public GrpcStreamTelemetry(ITelemetry telemetry)
    {
        _telemetry = telemetry;
    }

    public Activity StartStream(string name, string service, string method, ActivityContext? parentContext)
    {
        Activity activity = _telemetry.Start(
            name,
            GrpcStreamInstrumentation.ActivitySource,
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

    public void StopStream(Activity activity, bool operationSuccess)
    {
        _telemetry.Stop(activity, operationSuccess);
    }
}