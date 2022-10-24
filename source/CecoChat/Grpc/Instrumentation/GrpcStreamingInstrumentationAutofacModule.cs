using Autofac;
using CecoChat.Autofac;
using CecoChat.Tracing;

namespace CecoChat.Grpc.Instrumentation;

public sealed class GrpcStreamingInstrumentationAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        string utilityName = $"{nameof(GrpcStreamingActivityUtility)}.{nameof(IActivityUtility)}";

        builder.RegisterType<GrpcStreamingActivityUtility>().As<IGrpcStreamingActivityUtility>()
            .WithNamedParameter(typeof(IActivityUtility), utilityName)
            .SingleInstance();
        builder.RegisterType<ActivityUtility>().Named<IActivityUtility>(utilityName).SingleInstance();
    }
}