using Autofac;
using CecoChat.Autofac;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.Config;
using CecoChat.Data.IDGen;
using CecoChat.Grpc.Instrumentation;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Kafka.Instrumentation;
using CecoChat.Messaging.Server.Backplane;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Messaging.Server.Initialization;
using CecoChat.Otel;
using CecoChat.Server;
using CecoChat.Server.Backplane;
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
        private readonly IIdentityOptions _identityOptions;

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

            IdentityOptions identityOptions = new();
            Configuration.GetSection("Identity").Bind(identityOptions);
            _identityOptions = identityOptions;
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

            // idgen
            services.AddIdentityClient(_identityOptions);

            // clients
            services.AddGrpc(rpc => rpc.EnableDetailedErrors = Environment.IsDevelopment());

            // required
            services.AddOptions();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // ordered hosted services
            builder.RegisterHostedService<ConfigHostedService>();
            builder.RegisterHostedService<BackplaneHostedService>();
            builder.RegisterHostedService<PartitionsChangedHostedService>();

            // configuration
            builder.RegisterModule(new ConfigDbAutofacModule
            {
                RedisConfiguration = Configuration.GetSection("ConfigDB"),
                RegisterPartitioning = true
            });

            // clients
            builder.RegisterModule(new GrpcInstrumentationAutofacModule());
            builder.RegisterType<ClientContainer>().As<IClientContainer>().SingleInstance();
            builder.RegisterFactory<GrpcListenStreamer, IGrpcListenStreamer>();
            builder.RegisterOptions<ClientOptions>(Configuration.GetSection("Clients"));

            // identity
            builder.RegisterType<IdentityClient>().As<IIdentityClient>().SingleInstance();
            builder.RegisterOptions<IdentityOptions>(Configuration.GetSection("Identity"));

            // backplane
            builder.RegisterModule(new PartitionUtilityAutofacModule());
            builder.RegisterType<BackplaneComponents>().As<IBackplaneComponents>().SingleInstance();
            builder.RegisterType<TopicPartitionFlyweight>().As<ITopicPartitionFlyweight>().SingleInstance();
            builder.RegisterType<SendProducer>().As<ISendProducer>().SingleInstance();
            builder.RegisterType<ReceiversConsumer>().As<IReceiversConsumer>().SingleInstance();
            builder.RegisterFactory<KafkaProducer<Null, BackplaneMessage>, IKafkaProducer<Null, BackplaneMessage>>();
            builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
            builder.RegisterModule(new KafkaInstrumentationAutofacModule());
            builder.RegisterOptions<BackplaneOptions>(Configuration.GetSection("Backplane"));

            // shared
            builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
            builder.RegisterType<MessageMapper>().As<IMessageMapper>().SingleInstance();
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
