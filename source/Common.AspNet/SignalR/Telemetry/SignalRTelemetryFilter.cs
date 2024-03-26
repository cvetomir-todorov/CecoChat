using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;

namespace Common.AspNet.SignalR.Telemetry;

public class SignalRTelemetryFilter : IHubFilter
{
    private readonly ISignalRTelemetry _signalRTelemetry;

    public SignalRTelemetryFilter(ISignalRTelemetry signalRTelemetry)
    {
        _signalRTelemetry = signalRTelemetry;
    }

    public ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        Activity? activity = _signalRTelemetry.StartHubInvocationActivity(invocationContext);

        try
        {
            ValueTask<object?> result = next(invocationContext);
            _signalRTelemetry.Succeed(activity);
            return result;
        }
        catch (Exception exception)
        {
            _signalRTelemetry.Fail(activity, exception);
            throw;
        }
    }
}
