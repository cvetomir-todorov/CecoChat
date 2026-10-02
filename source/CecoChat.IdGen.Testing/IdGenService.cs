using Autofac;
using CecoChat.Config;
using CecoChat.Config.Client;
using CecoChat.Config.Contracts;
using CecoChat.IdGen.Service;
using CecoChat.Server;
using CecoChat.Testing.Config;
using Common.Autofac;
using Common.Kafka;
using Common.Testing.AspNet;
using Common.Testing.Kafka;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;

namespace CecoChat.IdGen.Testing;

public sealed class IdGenService : BaseService
{
    public IdGenService(ServiceOptions options)
    {
        WebApplicationBuilder builder = CreateBuilder(typeof(Program), options);

        CommonOptions commonOptions = new(builder.Configuration);

        Program.AddServices(builder, commonOptions);
        Program.AddHealth(builder, commonOptions);
        Program.AddTelemetry(builder, commonOptions);

        builder.Host.ConfigureContainer<ContainerBuilder>((host, autofacBuilder) =>
        {
            Program.ConfigureContainer(host, autofacBuilder);

            // override registrations
            autofacBuilder.Register(_ => new ConfigClientStub(
                [
                    new() { Name = ConfigKeys.Snowflake.GeneratorIds, Value = "123=0,1" }
                ]))
                .As<IConfigClient>().SingleInstance();
            autofacBuilder.RegisterType<KafkaAdminDummy>().As<IKafkaAdmin>().SingleInstance();
            autofacBuilder.RegisterFactory<KafkaConsumerDummy<Null, ConfigChange>, IKafkaConsumer<Null, ConfigChange>>();
        });

        App = builder.Build();
        Program.ConfigurePipeline(App);
    }
}
