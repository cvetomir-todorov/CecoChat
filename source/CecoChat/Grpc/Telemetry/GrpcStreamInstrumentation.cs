using System.Diagnostics;
using System.Reflection;

namespace CecoChat.Grpc.Telemetry;

internal static class GrpcStreamInstrumentation
{
    private static readonly string ActivitySourceName = "OpenTelemetry.Instrumentation.GrpcStream";
    private static readonly AssemblyName AssemblyName = typeof(GrpcStreamInstrumentation).Assembly.GetName();
    private static readonly Version ActivitySourceVersion = AssemblyName.Version!;

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, ActivitySourceVersion.ToString());
}
