using OpenTelemetry.Trace;

namespace CecoChat.Grpc.Instrumentation
{
    public static class GrpcInstrumentationExtensions
    {
        public static TracerProviderBuilder AddGrpcInstrumentation(this TracerProviderBuilder builder)
        {
            return builder.AddSource(GrpcInstrumentation.ActivitySource.Name);
        }
    }
}