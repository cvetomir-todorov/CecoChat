using Autofac;
using CecoChat.Autofac;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.State;
using CecoChat.Data.State.Instrumentation;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Kafka.Instrumentation;
using CecoChat.Otel;
using CecoChat.Server.Identity;
using CecoChat.Server.State.Backplane;
using CecoChat.Server.State.Clients;
using CecoChat.Server.State.HostedServices;
using Confluent.Kafka;
using OpenTelemetry.Trace;

namespace CecoChat.Server.State;

public class Startup
{
    private readonly JwtOptions _jwtOptions;
    private readonly OtelSamplingOptions _otelSamplingOptions;
    private readonly JaegerOptions _jaegerOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;

        _jwtOptions = new();
        configuration.GetSection("Jwt").Bind(_jwtOptions);

        _otelSamplingOptions = new();
        Configuration.GetSection("OtelSampling").Bind(_otelSamplingOptions);

        _jaegerOptions = new();
        Configuration.GetSection("Jaeger").Bind(_jaegerOptions);
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // telemetry
        services.AddOpenTelemetryTracing(otel =>
        {
            otel.AddServiceResource(new OtelServiceResource { Namespace = "CecoChat", Name = "State", Version = "0.1" });
            otel.AddAspNetCoreInstrumentation(aspnet => aspnet.EnableGrpcAspNetCoreSupport = true);
            otel.AddKafkaInstrumentation();
            otel.AddStateInstrumentation();
            otel.ConfigureSampling(_otelSamplingOptions);
            otel.ConfigureJaegerExporter(_jaegerOptions);
        });

        // security
        services.AddJwtAuthentication(_jwtOptions);
        services.AddAuthorization();

        // clients
        services.AddGrpc(rpc => rpc.EnableDetailedErrors = Environment.IsDevelopment());

        // required
        services.AddOptions();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitStateDb>();
        builder.RegisterHostedService<StartBackplaneComponents>();

        // state
        IConfiguration stateDbConfig = Configuration.GetSection("StateDB");
        builder.RegisterModule(new StateDbAutofacModule(stateDbConfig));
        builder.RegisterType<LruStateCache>().As<IStateCache>().SingleInstance();
        builder.RegisterOptions<StateCacheOptions>(Configuration.GetSection("StateCache"));

        // backplane
        builder.RegisterType<StateConsumer>().As<IStateConsumer>().SingleInstance();
        builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
        builder.RegisterModule(new KafkaInstrumentationAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(Configuration.GetSection("Backplane"));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<GrpcStateService>();
        });
    }
}