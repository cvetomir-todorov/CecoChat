using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Grpc.Telemetry;

internal static class GrpcStreamInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.GrpcStream";
    private static readonly AssemblyName _assemblyName = typeof(GrpcStreamInstrumentation).Assembly.GetName();
    private static readonly Version _activitySourceVersion = _assemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, _activitySourceVersion.ToString());
}