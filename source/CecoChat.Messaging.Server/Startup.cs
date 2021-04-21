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
using CecoChat.Messaging.Server.Identity;
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
        private readonly IIdentityClientOptions _identityClientOptions;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;

            JwtOptions jwtOptions = new();
            Configuration.GetSection("Jwt").Bind(jwtOptions);
            _jwtOptions = jwtOptions;

            OtelSamplingOptions otelSamplingOptions = new();
            Configuration.GetSection("OtelSampling").Bind(otelSamplingOptions);
            _otelSamplingOptions = otelSamplingOptions;

            JaegerOptions jaegerOptions = new();
            Configuration.GetSection("Jaeger").Bind(jaegerOptions);
            _jaegerOptions = jaegerOptions;

            IdentityClientOptions identityClientOptions = new();
            Configuration.GetSection("IdentityClient").Bind(identityClientOptions);
            _identityClientOptions = identityClientOptions;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // telemetry
            services.AddOpenTelemetryTracing(otel =>
            {
                otel.AddServiceResource(new OtelServiceResource {Namespace = "CecoChat", Name = "Messaging", Version = "0.1"});
                otel.AddAspNetCoreInstrumentation(aspnet => aspnet.EnableGrpcAspNetCoreSupport = true);
                otel.AddKafkaInstrumentation();
                otel.AddGrpcClientInstrumentation(grpc => grpc.SuppressDownstreamInstrumentation = true);
                otel.AddGrpcInstrumentation();
                otel.ConfigureSampling(_otelSamplingOptions);
                otel.ConfigureJaegerExporter(_jaegerOptions);
            });

            // security
            services.AddJwtAuthentication(_jwtOptions);
            services.AddAuthorization();

            // identity client
            services.AddIdentityClient(_identityClientOptions);

            // clients
            services.AddGrpc(rpc => rpc.EnableDetailedErrors = Environment.IsDevelopment());

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
            builder.RegisterModule(new ConfigurationDbAutofacModule
            {
                RedisConfiguration = Configuration.GetSection("ConfigurationDB"),
                RegisterPartitioning = true
            });

            // clients
            builder.RegisterModule(new GrpcInstrumentationAutofacModule());
            builder.RegisterType<ClientContainer>().As<IClientContainer>().SingleInstance();
            builder.RegisterFactory<GrpcListenStreamer, IGrpcListenStreamer>();
            builder.RegisterOptions<ClientOptions>(Configuration.GetSection("Clients"));

            // identity
            builder.RegisterType<IdentityClient>().As<IIdentityClient>().SingleInstance();
            builder.RegisterOptions<IdentityClientOptions>(Configuration.GetSection("IdentityClient"));

            // backend
            builder.RegisterModule(new PartitionUtilityAutofacModule());
            builder.RegisterType<BackendComponents>().As<IBackendComponents>().SingleInstance();
            builder.RegisterType<TopicPartitionFlyweight>().As<ITopicPartitionFlyweight>().SingleInstance();
            builder.RegisterType<SendProducer>().As<ISendProducer>().SingleInstance();
            builder.RegisterType<ReceiversConsumer>().As<IReceiversConsumer>().SingleInstance();
            builder.RegisterFactory<KafkaProducer<Null, BackendMessage>, IKafkaProducer<Null, BackendMessage>>();
            builder.RegisterFactory<KafkaConsumer<Null, BackendMessage>, IKafkaConsumer<Null, BackendMessage>>();
            builder.RegisterModule(new KafkaInstrumentationAutofacModule());
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
