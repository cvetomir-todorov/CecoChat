using System.Reflection;
using Autofac;
using CecoChat.Autofac;
using CecoChat.Client.History;
using CecoChat.Client.State;
using CecoChat.Data.Config;
using CecoChat.Jwt;
using CecoChat.Otel;
using CecoChat.Server.Backplane;
using CecoChat.Server.Bff.Controllers.Infrastructure;
using CecoChat.Server.Bff.HostedServices;
using CecoChat.Server.Identity;
using CecoChat.Swagger;
using FluentValidation;
using FluentValidation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.Bff;

public class Startup
{
    private readonly HistoryOptions _historyOptions;
    private readonly StateOptions _stateOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly SwaggerOptions _swaggerOptions;
    private readonly OtelSamplingOptions _otelSamplingOptions;
    private readonly JaegerOptions _jaegerOptions;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;

        _historyOptions = new();
        Configuration.GetSection("HistoryClient").Bind(_historyOptions);

        _stateOptions = new();
        Configuration.GetSection("StateClient").Bind(_stateOptions);

        _jwtOptions = new();
        Configuration.GetSection("Jwt").Bind(_jwtOptions);

        _swaggerOptions = new();
        Configuration.GetSection("Swagger").Bind(_swaggerOptions);

        _otelSamplingOptions = new();
        Configuration.GetSection("OtelSampling").Bind(_otelSamplingOptions);

        _jaegerOptions = new();
        Configuration.GetSection("Jaeger").Bind(_jaegerOptions);
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // telemetry
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "Bff", serviceNamespace: "CecoChat", serviceVersion: "0.1")
            .AddEnvironmentVariableDetector();

        services.AddOpenTelemetryTracing(otel =>
        {
            otel.SetResourceBuilder(serviceResourceBuilder);
            otel.AddAspNetCoreInstrumentation();
            otel.AddGrpcClientInstrumentation(grpc => grpc.SuppressDownstreamInstrumentation = false);
            otel.ConfigureSampling(_otelSamplingOptions);
            otel.ConfigureJaegerExporter(_jaegerOptions);
        });

        // security
        services.AddJwtAuthentication(_jwtOptions);

        // web
        services.AddControllers(mvc =>
        {
            // insert it before the default one so that it takes effect
            mvc.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
        });
        services.AddFluentValidationAutoValidation(fluentValidation =>
        {
            fluentValidation.DisableDataAnnotationsValidation = true;
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddSwaggerServices(_swaggerOptions);

        // downstream services
        services.AddHistoryClient(_historyOptions);
        services.AddStateClient(_stateOptions);

        // required
        services.AddOptions();
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
        app.MapWhen(context => context.Request.Path.StartsWithSegments("/swagger"), _ =>
        {
            app.UseSwaggerMiddlewares(_swaggerOptions);
        });

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}