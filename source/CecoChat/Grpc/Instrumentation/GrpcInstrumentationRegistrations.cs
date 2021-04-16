using OpenTelemetry.Trace;

namespace CecoChat.Grpc.Instrumentation
{
    public static class GrpcInstrumentationRegistrations
    {
        public static TracerProviderBuilder AddGrpcInstrumentation(this TracerProviderBuilder builder)
        {
            return builder.AddSource(GrpcInstrumentation.ActivitySource.Name);
        }
    }
}