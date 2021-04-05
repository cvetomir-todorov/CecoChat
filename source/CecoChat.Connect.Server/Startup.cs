using CecoChat.Connect.Server.Initialization;
using CecoChat.Data.Configuration;
using CecoChat.Jwt;
using CecoChat.Otel;
using CecoChat.Server.Backend;
using CecoChat.Server.Identity;
using CecoChat.Swagger;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace CecoChat.Connect.Server
{
    public class Startup
    {
        private readonly IJwtOptions _jwtOptions;
        private readonly IOtelSamplingOptions _otelSamplingOptions;
        private readonly IJaegerOptions _jaegerOptions;
        private readonly ISwaggerOptions _swaggerOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            JwtOptions jwtOptions = new();
            Configuration.GetSection("Jwt").Bind(jwtOptions);
            _jwtOptions = jwtOptions;

            OtelSamplingOptions otelSamplingOptions = new();
            Configuration.GetSection("OtelSampling").Bind(otelSamplingOptions);
            _otelSamplingOptions = otelSamplingOptions;

            JaegerOptions jaegerOptions = new();
            Configuration.GetSection("Jaeger").Bind(jaegerOptions);
            _jaegerOptions = jaegerOptions;

            SwaggerOptions swaggerOptions = new();
            Configuration.GetSection("Swagger").Bind(swaggerOptions);
            _swaggerOptions = swaggerOptions;
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

            // ordered hosted services
            services.AddHostedService<ConfigurationHostedService>();

            // security
            services.AddJwtAuthentication(_jwtOptions);

            // configuration
            services.AddConfiguration(Configuration.GetSection("ConfigurationDB"), addHistory: true, addPartitioning: true);

            // web
            services
                .AddControllers()
                .AddFluentValidation(fluentValidation =>
                {
                    fluentValidation.RunDefaultMvcValidationAfterFluentValidationExecutes = false;
                    fluentValidation.RegisterValidatorsFromAssemblyContaining<Startup>();
                });
            services.AddSwaggerServices(_swaggerOptions);

            // backend
            services.AddPartitionUtility();
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
