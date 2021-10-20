using Autofac;
using CecoChat.Autofac;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.History;
using CecoChat.Data.History.Instrumentation;
using CecoChat.Kafka;
using CecoChat.Kafka.Instrumentation;
using CecoChat.Otel;
using CecoChat.Server.Materialize.Backplane;
using CecoChat.Server.Materialize.HostedServices;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Server.Materialize
{
    public class Startup
    {
        private readonly OtelSamplingOptions _otelSamplingOptions;
        private readonly JaegerOptions _jaegerOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            _otelSamplingOptions = new();
            Configuration.GetSection("OtelSampling").Bind(_otelSamplingOptions);

            _jaegerOptions = new();
            Configuration.GetSection("Jaeger").Bind(_jaegerOptions);
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // telemetry
            services.AddOpenTelemetryTracing(otel =>
            {
                otel.AddServiceResource(new OtelServiceResource {Namespace = "CecoChat", Name = "Materialize", Version = "0.1"});
                otel.AddKafkaInstrumentation();
                otel.AddHistoryInstrumentation();
                otel.ConfigureSampling(_otelSamplingOptions);
                otel.ConfigureJaegerExporter(_jaegerOptions);
            });

            // required
            services.AddOptions();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // ordered hosted services
            builder.RegisterHostedService<InitHistoryDb>();
            builder.RegisterHostedService<PrepareHistoryQueries>();
            builder.RegisterHostedService<StartMaterializeMessages>();

            // history
            builder.RegisterModule(new HistoryDbAutofacModule
            {
                HistoryDbConfiguration = Configuration.GetSection("HistoryDB"),
                RegisterNewMessage = true,
                RegisterReactions = true
            });

            // backplane
            builder.RegisterType<MaterializeConsumer>().As<IMaterializeConsumer>().SingleInstance();
            builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
            builder.RegisterModule(new KafkaInstrumentationAutofacModule());
            builder.RegisterOptions<BackplaneOptions>(Configuration.GetSection("Backplane"));

            // shared
            builder.RegisterType<ContractDataMapper>().As<IContractDataMapper>().SingleInstance();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
        }
    }
}
