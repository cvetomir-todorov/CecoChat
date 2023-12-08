using System.Reflection;
using Autofac;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.Prometheus;
using CecoChat.AspNet.Swagger;
using CecoChat.Autofac;
using CecoChat.Client.Chats;
using CecoChat.Client.User;
using CecoChat.Data.Config;
using CecoChat.Http.Health;
using CecoChat.Jwt;
using CecoChat.Npgsql.Health;
using CecoChat.Otel;
using CecoChat.Server.Backplane;
using CecoChat.Server.Bff.HostedServices;
using CecoChat.Server.Bff.Infra;
using CecoChat.Server.ExceptionHandling;
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

    public Startup(IConfiguration configuration)
        : base(configuration)
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

        // config db
        services.AddConfigDb(ConfigDbOptions.Connect);

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
        services.AddHealthChecks()
            .AddCheck<ConfigDbInitHealthCheck>(
                "config-db-init",
                tags: new[] { HealthTags.Health, HealthTags.Startup })
            .AddNpgsql(
                "config-db",
                ConfigDbOptions.Connect,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddUri(
                "chats",
                new Uri(_chatsClientOptions.Address!, _chatsClientOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                timeout: _chatsClientOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddUri(
                "user",
                new Uri(_userClientOptions.Address!, _userClientOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                timeout: _userClientOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready });

        services.AddSingleton<ConfigDbInitHealthCheck>();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitDynamicConfig>();

        // configuration
        IConfiguration configDbConfig = Configuration.GetSection("ConfigDb");
        builder.RegisterModule(new ConfigDbAutofacModule(configDbConfig, registerPartitioning: true));

        // backplane
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
