using System.Diagnostics;
using System.Reflection;

namespace Common.AspNet.SignalR.Telemetry;

public static class SignalRInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.SignalR";
    private static readonly AssemblyName AssemblyName = typeof(SignalRInstrumentation).Assembly.GetName();
    private static readonly Version ActivitySourceVersion = AssemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, ActivitySourceVersion.ToString());
    internal static readonly string ActivityName = ActivitySourceName + ".Execute";

    public static class Values
    {
        public const string MessagingSystemSignalR = "signalr";
        public const string MessagingDestinationKindClientGroup = "client_group";

        public const string RpcSystemSignalR = "signalr";
    }
}
