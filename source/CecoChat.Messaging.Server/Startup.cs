using CecoChat.Contracts.Backend;
using CecoChat.Data.Configuration;
using CecoChat.Grpc;
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

            // ordered hosted services
            services.AddHostedService<ConfigurationHostedService>();
            services.AddHostedService<BackendHostedService>();
            services.AddHostedService<PartitionsChangedHostedService>();

            // security
            services.AddJwtAuthentication(_jwtOptions);
            services.AddAuthorization();

            // configuration
            services.AddConfiguration(Configuration.GetSection("ConfigurationDB"), addPartitioning: true);

            // clients
            services.AddGrpc();
            services.AddGrpcCustomUtilies();
            services.AddSingleton<IClientContainer, ClientContainer>();
            services.AddFactory<IGrpcListenStreamer, GrpcListenStreamer>();
            services.Configure<ClientOptions>(Configuration.GetSection("Clients"));

            // backend
            services.AddPartitionUtility();
            services.AddSingleton<IBackendComponents, BackendComponents>();
            services.AddSingleton<ITopicPartitionFlyweight, TopicPartitionFlyweight>();
            services.AddSingleton<IMessagesToBackendProducer, MessagesToBackendProducer>();
            services.AddSingleton<IMessagesToReceiversConsumer, MessagesToReceiversConsumer>();
            services.AddSingleton<IMessagesToSendersConsumer, MessagesToSendersConsumer>();
            services.AddFactory<IKafkaProducer<Null, BackendMessage>, KafkaProducer<Null, BackendMessage>>();
            services.AddFactory<IKafkaConsumer<Null, BackendMessage>, KafkaConsumer<Null, BackendMessage>>();
            services.AddKafka();
            services.Configure<BackendOptions>(Configuration.GetSection("Backend"));

            // shared
            services.AddSingleton<IClock, MonotonicClock>();
            services.AddSingleton<IClientBackendMapper, ClientBackendMapper>();
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
