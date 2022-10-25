using OpenTelemetry.Trace;

namespace CecoChat.Grpc.Telemetry;

public static class GrpcStreamRegistrations
{
    public static TracerProviderBuilder AddGrpcStreamInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(GrpcStreamInstrumentation.ActivitySource.Name);
    }
}