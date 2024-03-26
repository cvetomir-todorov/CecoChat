using Autofac;

namespace Common.AspNet.SignalR.Telemetry;

public class SignalRTelemetryAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SignalRTelemetry>().As<ISignalRTelemetry>().SingleInstance();
    }
}
