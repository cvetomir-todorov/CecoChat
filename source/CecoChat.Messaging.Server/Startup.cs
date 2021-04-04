using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.Data.Configuration;
using CecoChat.DependencyInjection;
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
using CecoChat.Tracing;
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
        private readonly IJaegerOptions _jaegerOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            JaegerOptions jaegerOptions = new();
            Configuration.GetSection("Jaeger").Bind(jaegerOptions);
            _jaegerOptions = jaegerOptions;

            JwtOptions jwtOptions = new();
            Configuration.GetSection("Jwt").Bind(jwtOptions);
            _jwtOptions = jwtOptions;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // telemetry
            services.AddOpenTelemetryTracing(otel =>
            {
                otel.AddServiceResource(new OtelServiceResource { Namespace = "CecoChat", Service = "Messaging", Version = "0.1" });
                otel.AddAspNetCoreInstrumentation(aspnet => aspnet.EnableGrpcAspNetCoreSupport = true);
                otel.AddKafkaInstrumentation();
                otel.ConfigureJaegerExporter(_jaegerOptions);
            });

            // ordered hosted services
            services.AddHostedService<ConfigurationHostedService>();
            services.AddHostedService<BackendHostedService>();
            services.AddHostedService<PartitionsChangedHostedService>();

            // clients
            services.AddGrpc();
            services.AddSingleton<IClientContainer, ClientContainer>();
            services.AddFactory<IGrpcStreamer<ListenResponse>, GrpcStreamer<ListenResponse>>();
            services.Configure<ClientOptions>(Configuration.GetSection("Clients"));

            // security
            services.AddJwtAuthentication(_jwtOptions);
            services.AddAuthorization();

            // backend
            services.AddPartitionUtility();
            services.AddSingleton<IBackendComponents, BackendComponents>();
            services.AddSingleton<ITopicPartitionFlyweight, TopicPartitionFlyweight>();
            services.AddSingleton<IMessagesToBackendProducer, MessagesToBackendProducer>();
            services.AddSingleton<IMessagesToReceiversConsumer, MessagesToReceiversConsumer>();
            services.AddSingleton<IMessagesToSendersConsumer, MessagesToSendersConsumer>();
            services.AddFactory<IKafkaProducer<Null, BackendMessage>, KafkaProducer<Null, BackendMessage>>();
            services.AddFactory<IKafkaConsumer<Null, BackendMessage>, KafkaConsumer<Null, BackendMessage>>();
            services.AddSingleton<IKafkaActivityUtility, KafkaActivityUtility>();
            services.Configure<BackendOptions>(Configuration.GetSection("Backend"));

            // configuration
            services.AddConfiguration(Configuration.GetSection("ConfigurationDB"), addPartitioning: true);

            // shared
            services.AddSingleton<IClock, MonotonicClock>();
            services.AddSingleton<IClientBackendMapper, ClientBackendMapper>();
            services.AddSingleton<IActivityUtility, ActivityUtility>();
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
