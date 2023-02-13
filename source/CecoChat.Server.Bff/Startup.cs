using System.Reflection;
using Autofac;
using CecoChat.Autofac;
using CecoChat.Client.History;
using CecoChat.Client.State;
using CecoChat.Client.User;
using CecoChat.Data.Config;
using CecoChat.Http.Health;
using CecoChat.Jwt;
using CecoChat.Otel;
using CecoChat.Redis;
using CecoChat.Server.Backplane;
using CecoChat.Server.Bff.HostedServices;
using CecoChat.Server.Bff.Infra;
using CecoChat.Server.Config;
using CecoChat.Server.Health;
using CecoChat.Server.Identity;
using CecoChat.Swagger;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.Bff;

public class Startup
{
    private readonly RedisOptions _configDbOptions;
    private readonly HistoryOptions _historyOptions;
    private readonly StateOptions _stateOptions;
    private readonly UserOptions _userOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly SwaggerOptions _swaggerOptions;
    private readonly OtelSamplingOptions _otelSamplingOptions;
    private readonly JaegerOptions _jaegerOptions;
    private readonly PrometheusOptions _prometheusOptions;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;

        _configDbOptions = new();
        Configuration.GetSection("ConfigDB").Bind(_configDbOptions);

        _historyOptions = new();
        Configuration.GetSection("HistoryClient").Bind(_historyOptions);

        _stateOptions = new();
        Configuration.GetSection("StateClient").Bind(_stateOptions);

        _userOptions = new();
        Configuration.GetSection("UserClient").Bind(_userOptions);

        _jwtOptions = new();
        Configuration.GetSection("Jwt").Bind(_jwtOptions);

        _swaggerOptions = new();
        Configuration.GetSection("Swagger").Bind(_swaggerOptions);

        _otelSamplingOptions = new();
        Configuration.GetSection("OtelSampling").Bind(_otelSamplingOptions);

        _jaegerOptions = new();
        Configuration.GetSection("Jaeger").Bind(_jaegerOptions);

        _prometheusOptions = new();
        Configuration.GetSection("Prometheus").Bind(_prometheusOptions);
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        AddTelemetryServices(services);
        AddHealthServices(services);

        // security
        services.AddJwtAuthentication(_jwtOptions);

        // web
        services.AddControllers(mvc =>
        {
            // insert it before the default one so that it takes effect
            mvc.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
        });
        services.AddSwaggerServices(_swaggerOptions);

        // downstream services
        services.AddHistoryClient(_historyOptions);
        services.AddStateClient(_stateOptions);
        services.AddUserClient(_userOptions);

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

        services.AddOpenTelemetryTracing(tracing =>
        {
            tracing.SetResourceBuilder(serviceResourceBuilder);
            tracing.AddAspNetCoreInstrumentation(aspnet =>
            {
                HashSet<string> excludedPaths = new()
                {
                    _prometheusOptions.ScrapeEndpointPath, HealthPaths.Health, HealthPaths.Startup, HealthPaths.Live, HealthPaths.Ready
                };
                aspnet.Filter = httpContext => !excludedPaths.Contains(httpContext.Request.Path);
            });
            tracing.AddGrpcClientInstrumentation(grpc => grpc.SuppressDownstreamInstrumentation = true);
            tracing.ConfigureSampling(_otelSamplingOptions);
            tracing.ConfigureJaegerExporter(_jaegerOptions);
        });
        services.AddOpenTelemetryMetrics(metrics =>
        {
            metrics.SetResourceBuilder(serviceResourceBuilder);
            metrics.AddAspNetCoreInstrumentation();
            metrics.ConfigurePrometheusAspNetExporter(_prometheusOptions);
        });
    }

    private void AddHealthServices(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<ConfigDbInitHealthCheck>(
                "config-db-init",
                tags: new[] { HealthTags.Health, HealthTags.Startup })
            .AddConfigDb(
                _configDbOptions,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddUri(
                new Uri(_historyOptions.Address!, _historyOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                name: "history",
                timeout: _historyOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddUri(
                new Uri(_stateOptions.Address!, _stateOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                name: "state",
                timeout: _stateOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready });

        services.AddSingleton<ConfigDbInitHealthCheck>();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitDynamicConfig>();

        // configuration
        IConfiguration configDbConfig = Configuration.GetSection("ConfigDB");
        builder.RegisterModule(new ConfigDbAutofacModule(configDbConfig, registerPartitioning: true));

        // backplane
        builder.RegisterModule(new PartitionUtilityAutofacModule());

        // downstream services
        IConfiguration historyClientConfig = Configuration.GetSection("HistoryClient");
        builder.RegisterModule(new HistoryClientAutofacModule(historyClientConfig));
        IConfiguration stateClientConfig = Configuration.GetSection("StateClient");
        builder.RegisterModule(new StateClientAutofacModule(stateClientConfig));
        IConfiguration userClientConfig = Configuration.GetSection("UserClient");
        builder.RegisterModule(new UserClientAutofacModule(userClientConfig));

        // security
        builder.RegisterOptions<JwtOptions>(Configuration.GetSection("Jwt"));

        // shared
        builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
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

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == _prometheusOptions.ScrapeEndpointPath);
        app.MapWhen(context => context.Request.Path.StartsWithSegments("/swagger"), _ =>
        {
            app.UseSwaggerMiddlewares(_swaggerOptions);
        });
    }
}
