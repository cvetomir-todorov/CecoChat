using Autofac;
using CecoChat.Otel;

namespace CecoChat.Grpc.Telemetry;

public sealed class GrpcStreamAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<GrpcStreamTelemetry>().As<IGrpcStreamTelemetry>().SingleInstance();
        builder.RegisterType<OtelTelemetry>().As<ITelemetry>().SingleInstance();
    }
}
