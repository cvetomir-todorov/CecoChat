using System.Diagnostics;
using System.Reflection;

namespace CecoChat.AspNet.SignalR.Telemetry;

public static class SignalRInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.SignalR";
    private static readonly AssemblyName AssemblyName = typeof(SignalRInstrumentation).Assembly.GetName();
    private static readonly Version ActivitySourceVersion = AssemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, ActivitySourceVersion.ToString());
    internal static readonly string ActivityName = ActivitySourceName + ".Execute";
}
