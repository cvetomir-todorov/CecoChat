using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.Configuration;
using CecoChat.Data.History;
using CecoChat.Data.History.Instrumentation;
using CecoChat.History.Server.Clients;
using CecoChat.History.Server.Initialization;
using CecoChat.Jwt;
using CecoChat.Otel;
using CecoChat.Server;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace CecoChat.History.Server
{
    public class Startup
    {
        private readonly IJwtOptions _jwtOptions;
        private readonly IOtelSamplingOptions _otelSamplingOptions;
        private readonly IJaegerOptions _jaegerOptions;

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
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // telemetry
            services.AddOpenTelemetryTracing(otel =>
            {
                otel.AddServiceResource(new OtelServiceResource {Namespace = "CecoChat", Name = "History", Version = "0.1"});
                otel.AddAspNetCoreInstrumentation(aspnet => aspnet.EnableGrpcAspNetCoreSupport = true);
                otel.AddHistoryInstrumentation();
                otel.ConfigureSampling(_otelSamplingOptions);
                otel.ConfigureJaegerExporter(_jaegerOptions);
            });

            // security
            services.AddJwtAuthentication(_jwtOptions);
            services.AddAuthorization();

            // clients
            services.AddGrpc();

            // required
            services.AddOptions();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // ordered hosted services
            builder.RegisterHostedService<ConfigurationHostedService>();
            builder.RegisterHostedService<InitializeDbHostedService>();
            builder.RegisterHostedService<PrepareQueriesHostedService>();

            // configuration
            builder.RegisterModule(new ConfigurationDbModule
            {
                RedisConfiguration = Configuration.GetSection("ConfigurationDB"),
                RegisterHistory = true
            });

            // clients
            builder.RegisterOptions<ClientOptions>(Configuration.GetSection("Clients"));

            // history
            builder.RegisterModule(new HistoryDbModule
            {
                HistoryDbConfiguration = Configuration.GetSection("HistoryDB"),
                RegisterHistory = true
            });

            // shared
            builder.RegisterType<ClientBackendMapper>().As<IClientBackendMapper>().SingleInstance();
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
                endpoints.MapGrpcService<GrpcHistoryService>();
            });
        }
    }
}
