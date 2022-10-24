using OpenTelemetry.Trace;

namespace CecoChat.Grpc.Instrumentation;

public static class GrpcStreamingInstrumentationRegistrations
{
    public static TracerProviderBuilder AddGrpcStreamingInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(GrpcStreamingInstrumentation.ActivitySource.Name);
    }
}