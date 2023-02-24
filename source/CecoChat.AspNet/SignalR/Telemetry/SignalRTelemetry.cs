using System.Diagnostics;
using System.Diagnostics.Metrics;
using CecoChat.Otel;
using Microsoft.AspNetCore.SignalR;
using OpenTelemetry.Trace;

namespace CecoChat.AspNet.SignalR.Telemetry;

public interface ISignalRTelemetry : IDisposable
{
    Activity? Start(HubInvocationContext context, Action<Activity>? enrich = null);

    void Succeed(Activity? activity);

    void Fail(Activity? activity, Exception? exception);
}

public sealed class SignalRTelemetry : ISignalRTelemetry
{
    private readonly Meter _meter;
    private readonly Histogram<double> _clientDuration;

    public SignalRTelemetry()
    {
        _meter = new Meter(SignalRInstrumentation.ActivitySource.Name, SignalRInstrumentation.ActivitySource.Version);
        _clientDuration = _meter.CreateHistogram<double>("signalr.client.duration", "ms", "measures the duration of the SignalR request");
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public Activity? Start(HubInvocationContext context, Action<Activity>? enrich = null)
    {
        // we want this to be a root span for a new trace
        Activity.Current = null;

        Activity? activity = SignalRInstrumentation.ActivitySource.StartActivity(SignalRInstrumentation.ActivityName, ActivityKind.Server);
        if (activity != null)
        {
            string rpcService = context.HubMethod.DeclaringType!.Name.ToLowerInvariant();
            string rpcMethod = context.HubMethodName.ToLowerInvariant();
            activity.DisplayName = $"{rpcService}/{rpcMethod}";

            activity.SetTag(OtelInstrumentation.Keys.RpcSystem, OtelInstrumentation.Values.RpcSystemSignalR);
            activity.SetTag(OtelInstrumentation.Keys.RpcService, rpcService);
            activity.SetTag(OtelInstrumentation.Keys.RpcMethod, rpcMethod);

            if (activity.IsAllDataRequested)
            {
                activity.SetTag("signalr.connection_id", context.Context.ConnectionId);
                enrich?.Invoke(activity);
            }
        }

        return activity;
    }
    
    public void Succeed(Activity? activity)
    {
        if (activity == null)
        {
            return;
        }

        activity.Stop();
        activity.SetStatus(ActivityStatusCode.Ok);
        Record(activity);
    }

    public void Fail(Activity? activity, Exception? exception)
    {
        if (activity == null)
        {
            return;
        }

        activity.Stop();

        if (activity.IsAllDataRequested && exception != null)
        {
            activity.SetStatus(Status.Error.WithDescription(exception.Message));
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error);
        }

        Record(activity);
    }

    private void Record(Activity activity)
    {
        if (!activity.IsStopped)
        {
            throw new InvalidOperationException("Activity should have already been stopped.");
        }

        TagList tags = new();
        foreach (KeyValuePair<string,string?> tag in activity.Tags)
        {
            tags.Add(tag.Key, tag.Value);
        }

        _clientDuration.Record(activity.Duration.TotalMilliseconds, tags);
    }
}
