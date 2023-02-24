using Autofac;

namespace CecoChat.AspNet.SignalR.Telemetry;

public class SignalRTelemetryAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SignalRTelemetry>().As<ISignalRTelemetry>().SingleInstance();
    }
}
