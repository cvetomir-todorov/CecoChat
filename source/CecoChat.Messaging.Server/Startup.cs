using Autofac;
using CecoChat.Autofac;
using CecoChat.Contracts.Backend;
using CecoChat.Data.Configuration;
using CecoChat.Grpc.Instrumentation;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Kafka.Instrumentation;
using CecoChat.Messaging.Server.Backend;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Messaging.Server.Initialization;
using CecoChat.Otel;
using CecoChat.Server;
using CecoChat.Server.Backend;
using CecoChat.Server.Identity;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace CecoChat.Messaging.Server
{
    public class Startup
    {
        private readonly IJwtOptions _jwtOptions;
        private readonly IOtelSamplingOptions _otelSamplingOptions;
        private readonly IJaegerOptions _jaegerOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            JwtOptions jwtOptions = new();
            Configuration.GetSection("Jwt").Bind(jwtOptions);
            _jwtOptions = jwtOptions;

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
                otel.AddServiceResource(new OtelServiceResource {Namespace = "CecoChat", Name = "Messaging", Version = "0.1"});
                otel.AddAspNetCoreInstrumentation(aspnet => aspnet.EnableGrpcAspNetCoreSupport = true);
                otel.AddKafkaInstrumentation();
                otel.AddGrpcInstrumentation();
                otel.ConfigureSampling(_otelSamplingOptions);
                otel.ConfigureJaegerExporter(_jaegerOptions);
            });

            // security
            services.AddJwtAuthentication(_jwtOptions);
            services.AddAuthorization();

            // clients
            services.AddGrpc();

            // required
            services.AddOptions();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // ordered hosted services
            builder.RegisterHostedService<ConfigurationHostedService>();
            builder.RegisterHostedService<BackendHostedService>();
            builder.RegisterHostedService<PartitionsChangedHostedService>();

            // configuration
            builder.RegisterModule(new ConfigurationModule
            {
                RedisConfiguration = Configuration.GetSection("ConfigurationDB"),
                AddPartitioning = true
            });

            // clients
            builder.RegisterModule(new GrpcInstrumentationModule());
            builder.RegisterType<ClientContainer>().As<IClientContainer>().SingleInstance();
            builder.RegisterFactory<GrpcListenStreamer, IGrpcListenStreamer>();
            builder.RegisterOptions<ClientOptions>(Configuration.GetSection("Clients"));

            // backend
            builder.RegisterModule(new PartitionUtilityModule());
            builder.RegisterType<BackendComponents>().As<IBackendComponents>().SingleInstance();
            builder.RegisterType<TopicPartitionFlyweight>().As<ITopicPartitionFlyweight>().SingleInstance();
            builder.RegisterType<MessagesToBackendProducer>().As<IMessagesToBackendProducer>().SingleInstance();
            builder.RegisterType<MessagesToReceiversConsumer>().As<IBackendConsumer>().SingleInstance();
            builder.RegisterType<MessagesToSendersConsumer>().As<IBackendConsumer>().SingleInstance();
            builder.RegisterFactory<KafkaProducer<Null, BackendMessage>, IKafkaProducer<Null, BackendMessage>>();
            builder.RegisterFactory<KafkaConsumer<Null, BackendMessage>, IKafkaConsumer<Null, BackendMessage>>();
            builder.RegisterModule(new KafkaInstrumentationModule());
            builder.RegisterOptions<BackendOptions>(Configuration.GetSection("Backend"));

            // shared
            builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
            builder.RegisterType<ClientBackendMapper>().As<IClientBackendMapper>().SingleInstance();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GrpcListenService>();
                endpoints.MapGrpcService<GrpcSendService>();
            });
        }
    }
}
