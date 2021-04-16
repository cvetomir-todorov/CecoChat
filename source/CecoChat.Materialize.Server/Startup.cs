using Autofac;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Contracts.Backend;
using CecoChat.Data.History;
using CecoChat.Data.History.Instrumentation;
using CecoChat.Kafka;
using CecoChat.Kafka.Instrumentation;
using CecoChat.Materialize.Server.Backend;
using CecoChat.Materialize.Server.Initialization;
using CecoChat.Otel;
using CecoChat.Tracing;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Materialize.Server
{
    public class Startup
    {
        private readonly IOtelSamplingOptions _otelSamplingOptions;
        private readonly IJaegerOptions _jaegerOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            OtelSamplingOptions otelSamplingOptions = new();
            Configuration.GetSection("OtelSampling").Bind(otelSamplingOptions);
            _otelSamplingOptions = otelSamplingOptions;

            JaegerOptions jaegerOptions = new();
            Configuration.GetSection("Jaeger").Bind(jaegerOptions);
            _jaegerOptions = jaegerOptions;
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
            builder.RegisterHostedService<InitializeDbHostedService>();
            builder.RegisterHostedService<PrepareQueriesHostedService>();
            builder.RegisterHostedService<MaterializeMessagesHostedService>();

            // history
            builder.RegisterModule(new HistoryDbAutofacModule
            {
                HistoryDbConfiguration = Configuration.GetSection("HistoryDB"),
                RegisterNewMessage = true
            });

            // backend
            builder.RegisterType<MaterializeMessagesConsumer>().As<IMaterializeMessagesConsumer>().SingleInstance();
            builder.RegisterFactory<KafkaConsumer<Null, BackendMessage>, IKafkaConsumer<Null, BackendMessage>>();
            builder.RegisterModule(new KafkaInstrumentationAutofacModule());
            builder.RegisterOptions<BackendOptions>(Configuration.GetSection("Backend"));
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
