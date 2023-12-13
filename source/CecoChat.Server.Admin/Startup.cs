using System.Reflection;
using Autofac;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.ModelBinding;
using CecoChat.AspNet.Swagger;
using CecoChat.Autofac;
using CecoChat.Data.Config;
using CecoChat.Kafka;
using CecoChat.Kafka.Health;
using CecoChat.Npgsql;
using CecoChat.Npgsql.Health;
using CecoChat.Server.Admin.Backplane;
using CecoChat.Server.Admin.HostedServices;
using CecoChat.Server.Backplane;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CecoChat.Server.Admin;

public class Startup : StartupBase
{
    private readonly BackplaneOptions _backplaneOptions;
    private readonly SwaggerOptions _swaggerOptions;

    public Startup(IConfiguration configuration) : base(configuration)
    {
        _backplaneOptions = new();
        Configuration.GetSection("Backplane").Bind(_backplaneOptions);

        _swaggerOptions = new();
        Configuration.GetSection("Swagger").Bind(_swaggerOptions);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        AddHealthServices(services);

        // web
        services.AddControllers(mvc =>
        {
            // insert it before the default one so that it takes effect
            mvc.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
        });
        services.AddSwaggerServices(_swaggerOptions);

        // config db
        services.AddConfigDb(ConfigDbOptions.Connect);

        // common
        services.AddFluentValidationAutoValidation(fluentValidation =>
        {
            fluentValidation.DisableDataAnnotationsValidation = true;
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }

    private void AddHealthServices(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddNpgsql(
                "config-db",
                ConfigDbOptions.Connect,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddKafka(
                "backplane",
                _backplaneOptions.Kafka,
                _backplaneOptions.Health,
                tags: new[] { HealthTags.Health });
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitConfigDb>();
        builder.RegisterHostedService<InitBackplane>();

        // config db
        builder.RegisterType<NpgsqlDbInitializer>().As<INpgsqlDbInitializer>().SingleInstance();
        IConfiguration configDbConfig = Configuration.GetSection("ConfigDb");
        builder.RegisterOptions<ConfigDbOptions>(configDbConfig);

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(Configuration.GetSection("Backplane:Kafka"));
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
                Func<HttpContext, HealthReport, Task> responseWriter = (context, report) => CustomHealth.Writer(serviceName: "admin", context, report);
                setup.Health.ResponseWriter = responseWriter;

                if (env.IsDevelopment())
                {
                    setup.Startup.ResponseWriter = responseWriter;
                    setup.Live.ResponseWriter = responseWriter;
                    setup.Ready.ResponseWriter = responseWriter;
                }
            });
        });

        app.MapWhen(context => context.Request.Path.StartsWithSegments("/swagger"), _ =>
        {
            app.UseSwaggerMiddlewares(_swaggerOptions);
        });
    }
}
