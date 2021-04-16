using Autofac;
using CecoChat.Autofac;
using CecoChat.Tracing;

namespace CecoChat.Grpc.Instrumentation
{
    public sealed class GrpcInstrumentationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            string utilityName = $"{nameof(GrpcActivityUtility)}.{nameof(IActivityUtility)}";

            builder.RegisterType<GrpcActivityUtility>().As<IGrpcActivityUtility>()
                .WithNamedParameter(typeof(IActivityUtility), utilityName)
                .SingleInstance();
            builder.RegisterType<ActivityUtility>().Named<IActivityUtility>(utilityName).SingleInstance();
        }
    }
}
