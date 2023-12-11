using System.Reflection;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.ModelBinding;
using CecoChat.AspNet.Swagger;
using CecoChat.Data.Config;
using CecoChat.Npgsql.Health;
using CecoChat.Server.ExceptionHandling;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CecoChat.Server.Admin;

public class Startup : StartupBase 
{
    private readonly SwaggerOptions _swaggerOptions;

    public Startup(IConfiguration configuration) : base(configuration)
    {
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
                tags: new[] { HealthTags.Health, HealthTags.Ready });
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
