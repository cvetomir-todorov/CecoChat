using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Grpc.Instrumentation;

internal static class GrpcStreamingInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.GrpcStreaming";
    private static readonly AssemblyName _assemblyName = typeof(GrpcStreamingInstrumentation).Assembly.GetName();
    private static readonly Version _activitySourceVersion = _assemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, _activitySourceVersion.ToString());
}