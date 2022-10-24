using Autofac;
using CecoChat.Autofac;
using CecoChat.Client.IDGen;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.Config;
using CecoChat.Grpc.Instrumentation;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Kafka.Instrumentation;
using CecoChat.Otel;
using CecoChat.Server.Backplane;
using CecoChat.Server.Identity;
using CecoChat.Server.Messaging.Backplane;
using CecoChat.Server.Messaging.Clients;
using CecoChat.Server.Messaging.Clients.Streaming;
using CecoChat.Server.Messaging.HostedServices;
using Confluent.Kafka;
using OpenTelemetry.Trace;

namespace CecoChat.Server.Messaging;

public class Startup
{
    private readonly JwtOptions _jwtOptions;
    private readonly OtelSamplingOptions _otelSamplingOptions;
    private readonly JaegerOptions _jaegerOptions;
    private readonly IDGenOptions _idGenOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;

        _jwtOptions = new();
        Configuration.GetSection("Jwt").Bind(_jwtOptions);

        _otelSamplingOptions = new();
        Configuration.GetSection("OtelSampling").Bind(_otelSamplingOptions);

        _jaegerOptions = new();
        Configuration.GetSection("Jaeger").Bind(_jaegerOptions);

        _idGenOptions = new();
        Configuration.GetSection("IDGen").Bind(_idGenOptions);
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // telemetry
        services.AddOpenTelemetryTracing(otel =>
        {
            otel.AddServiceResource(new OtelServiceResource { Namespace = "CecoChat", Name = "Messaging", Version = "0.1" });
            otel.AddAspNetCoreInstrumentation(aspnet => aspnet.EnableGrpcAspNetCoreSupport = true);
            otel.AddKafkaInstrumentation();
            otel.AddGrpcClientInstrumentation(grpc => grpc.SuppressDownstreamInstrumentation = false);
            otel.AddGrpcStreamingInstrumentation();
            otel.ConfigureSampling(_otelSamplingOptions);
            otel.ConfigureJaegerExporter(_jaegerOptions);
        });

        // security
        services.AddJwtAuthentication(_jwtOptions);
        services.AddAuthorization();

        // idgen
        services.AddIDGenClient(_idGenOptions);

        // clients
        services.AddGrpc(rpc => rpc.EnableDetailedErrors = Environment.IsDevelopment());

        // required
        services.AddOptions();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitDynamicConfig>();
        builder.RegisterHostedService<StartBackplaneComponents>();
        builder.RegisterHostedService<HandlePartitionsChanged>();

        // configuration
        IConfiguration configDbConfig = Configuration.GetSection("ConfigDB");
        builder.RegisterModule(new ConfigDbAutofacModule(configDbConfig, registerPartitioning: true));
        builder.RegisterOptions<ConfigOptions>(Configuration.GetSection("Config"));

        // clients
        builder.RegisterModule(new GrpcStreamingInstrumentationAutofacModule());
        builder.RegisterType<ClientContainer>().As<IClientContainer>().SingleInstance();
        builder.RegisterFactory<GrpcListenStreamer, IGrpcListenStreamer>();
        builder.RegisterOptions<ClientOptions>(Configuration.GetSection("Clients"));

        // idgen
        IConfiguration idGenConfiguration = Configuration.GetSection("IDGen");
        builder.RegisterModule(new IDGenAutofacModule(idGenConfiguration));

        // backplane
        builder.RegisterModule(new PartitionUtilityAutofacModule());
        builder.RegisterType<BackplaneComponents>().As<IBackplaneComponents>().SingleInstance();
        builder.RegisterType<TopicPartitionFlyweight>().As<ITopicPartitionFlyweight>().SingleInstance();
        builder.RegisterType<SendersProducer>().As<ISendersProducer>().SingleInstance();
        builder.RegisterType<ReceiversConsumer>().As<IReceiversConsumer>().SingleInstance();
        builder.RegisterType<MessageReplicator>().As<IMessageReplicator>().SingleInstance();
        builder.RegisterFactory<KafkaProducer<Null, BackplaneMessage>, IKafkaProducer<Null, BackplaneMessage>>();
        builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
        builder.RegisterModule(new KafkaInstrumentationAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(Configuration.GetSection("Backplane"));

        // shared
        builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
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
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<GrpcListenService>();
            endpoints.MapGrpcService<GrpcSendService>();
            endpoints.MapGrpcService<GrpcReactionService>();
        });
    }
}