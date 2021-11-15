using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.Config;
using CecoChat.Jwt;
using CecoChat.Otel;
using CecoChat.Server.Backplane;
using CecoChat.Server.Identity;
using CecoChat.Swagger;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace CecoChat.Server.Connect
{
    public class Startup
    {
        private readonly JwtOptions _jwtOptions;
        private readonly OtelSamplingOptions _otelSamplingOptions;
        private readonly JaegerOptions _jaegerOptions;
        private readonly SwaggerOptions _swaggerOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            _jwtOptions = new();
            Configuration.GetSection("Jwt").Bind(_jwtOptions);

            _otelSamplingOptions = new();
            Configuration.GetSection("OtelSampling").Bind(_otelSamplingOptions);

            _jaegerOptions = new();
            Configuration.GetSection("Jaeger").Bind(_jaegerOptions);

            _swaggerOptions = new();
            Configuration.GetSection("Swagger").Bind(_swaggerOptions);
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // telemetry
            services.AddOpenTelemetryTracing(otel =>
            {
                otel.AddServiceResource(new OtelServiceResource {Namespace = "CecoChat", Name = "Connect", Version = "0.1"});
                otel.AddAspNetCoreInstrumentation(aspnet => aspnet.EnableGrpcAspNetCoreSupport = true);
                otel.ConfigureSampling(_otelSamplingOptions);
                otel.ConfigureJaegerExporter(_jaegerOptions);
            });

            // security
            services.AddJwtAuthentication(_jwtOptions);

            // web
            services
                .AddControllers()
                .AddFluentValidation(fluentValidation =>
                {
                    fluentValidation.DisableDataAnnotationsValidation = true;
                    fluentValidation.RegisterValidatorsFromAssemblyContaining<Startup>();
                });
            services.AddSwaggerServices(_swaggerOptions);

            // required
            services.AddOptions();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // configuration
            builder.RegisterModule(new ConfigDbAutofacModule
            {
                RedisConfiguration = Configuration.GetSection("ConfigDB"),
                RegisterHistory = true,
                RegisterPartitioning = true
            });

            // backplane
            builder.RegisterModule(new PartitionUtilityAutofacModule());
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
}
