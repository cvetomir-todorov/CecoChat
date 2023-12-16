using System.Reflection;
using Autofac;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.ModelBinding;
using CecoChat.AspNet.Prometheus;
using CecoChat.AspNet.Swagger;
using CecoChat.Autofac;
using CecoChat.Client.Chats;
using CecoChat.Client.Config;
using CecoChat.Client.User;
using CecoChat.DynamicConfig;
using CecoChat.Http.Health;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Kafka.Telemetry;
using CecoChat.Otel;
using CecoChat.Server.Backplane;
using CecoChat.Server.Bff.HostedServices;
using CecoChat.Server.Identity;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.Bff;

public class Startup : StartupBase
{
    private readonly ChatsClientOptions _chatsClientOptions;
    private readonly UserClientOptions _userClientOptions;
    private readonly SwaggerOptions _swaggerOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        : base(configuration, environment)
    {
        _chatsClientOptions = new();
        Configuration.GetSection("ChatsClient").Bind(_chatsClientOptions);

        _userClientOptions = new();
        Configuration.GetSection("UserClient").Bind(_userClientOptions);

        _swaggerOptions = new();
        Configuration.GetSection("Swagger").Bind(_swaggerOptions);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        AddTelemetryServices(services);
        AddHealthServices(services);

        // security
        services.AddJwtAuthentication(JwtOptions);
        services.AddUserPolicyAuthorization();

        // dynamic config
        services.AddConfigClient(ConfigClientOptions);

        // web
        services.AddControllers(mvc =>
        {
            // insert it before the default one so that it takes effect
            mvc.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
        });
        services.AddSwaggerServices(_swaggerOptions);

        // downstream services
        services.AddChatsClient(_chatsClientOptions);
        services.AddUserClient(_userClientOptions);

        // common
        services.AddAutoMapper(config =>
        {
            config.AddMaps(typeof(AutoMapperProfile));
        });
        services.AddFluentValidationAutoValidation(fluentValidation =>
        {
            fluentValidation.DisableDataAnnotationsValidation = true;
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddOptions();
    }

    private void AddTelemetryServices(IServiceCollection services)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "Bff", serviceNamespace: "CecoChat", serviceVersion: "0.1")
            .AddEnvironmentVariableDetector();

        services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(serviceResourceBuilder);
                tracing.AddAspNetCoreInstrumentation(aspnet =>
                {
                    HashSet<string> excludedPaths = new()
                    {
                        PrometheusOptions.ScrapeEndpointPath, HealthPaths.Health, HealthPaths.Startup, HealthPaths.Live, HealthPaths.Ready
                    };
                    aspnet.Filter = httpContext => !excludedPaths.Contains(httpContext.Request.Path);
                });
                tracing.AddKafkaInstrumentation();
                tracing.AddGrpcClientInstrumentation(grpc => grpc.SuppressDownstreamInstrumentation = true);
                tracing.ConfigureSampling(TracingSamplingOptions);
                tracing.ConfigureOtlpExporter(TracingExportOptions);
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(serviceResourceBuilder);
                metrics.AddAspNetCoreInstrumentation();
                metrics.ConfigurePrometheusAspNetExporter(PrometheusOptions);
            });
    }

    private void AddHealthServices(IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddDynamicConfigInit()
            .AddConfigChangesConsumer()
            .AddConfigService(ConfigClientOptions)
            .AddBackplane(Configuration)
            .AddUri(
                "chats-svc",
                new Uri(_chatsClientOptions.Address!, _chatsClientOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                timeout: _chatsClientOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddUri(
                "user-svc",
                new Uri(_userClientOptions.Address!, _userClientOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                timeout: _userClientOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready });
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitDynamicConfig>();
        builder.RegisterHostedService<InitBackplane>();
        builder.RegisterHostedService<InitBackplaneComponents>();

        // dynamic config
        IConfiguration backplaneConfiguration = Configuration.GetSection("Backplane");
        builder.RegisterModule(new DynamicConfigAutofacModule(backplaneConfiguration, registerConfigChangesConsumer: true, registerPartitioning: true));
        IConfiguration configClientConfiguration = Configuration.GetSection("ConfigClient");
        builder.RegisterModule(new ConfigClientAutofacModule(configClientConfiguration));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(Configuration.GetSection("Backplane:Kafka"));
        builder.RegisterModule(new PartitionerAutofacModule());

        // downstream services
        IConfiguration chatsClientConfig = Configuration.GetSection("ChatsClient");
        builder.RegisterModule(new ChatsClientAutofacModule(chatsClientConfig));
        IConfiguration userClientConfig = Configuration.GetSection("UserClient");
        builder.RegisterModule(new UserClientAutofacModule(userClientConfig));

        // security
        builder.RegisterOptions<JwtOptions>(Configuration.GetSection("Jwt"));

        // shared
        builder.RegisterType<ContractMapper>().As<IContractMapper>().SingleInstance();
        builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCustomExceptionHandler();
        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHttpHealthEndpoints(setup =>
            {
                Func<HttpContext, HealthReport, Task> responseWriter = (context, report) => CustomHealth.Writer(serviceName: "bff", context, report);
                setup.Health.ResponseWriter = responseWriter;

                if (env.IsDevelopment())
                {
                    setup.Startup.ResponseWriter = responseWriter;
                    setup.Live.ResponseWriter = responseWriter;
                    setup.Ready.ResponseWriter = responseWriter;
                }
            });
        });

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == PrometheusOptions.ScrapeEndpointPath);
        app.MapWhen(context => context.Request.Path.StartsWithSegments("/swagger"), _ =>
        {
            app.UseSwaggerMiddlewares(_swaggerOptions);
        });
    }
}
