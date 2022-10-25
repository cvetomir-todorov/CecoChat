using Autofac;
using CecoChat.Autofac;
using CecoChat.Otel;

namespace CecoChat.Grpc.Telemetry;

public sealed class GrpcStreamAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        string telemetryName = $"{nameof(GrpcStreamTelemetry)}.{nameof(ITelemetry)}";

        builder.RegisterType<GrpcStreamTelemetry>().As<IGrpcStreamTelemetry>()
            .WithNamedParameter(typeof(ITelemetry), telemetryName)
            .SingleInstance();
        builder.RegisterType<OtelTelemetry>().Named<ITelemetry>(telemetryName).SingleInstance();
    }
}