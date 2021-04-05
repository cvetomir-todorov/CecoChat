using CecoChat.Cassandra;
using CecoChat.Data.Configuration;
using CecoChat.Data.History;
using CecoChat.Data.History.Instrumentation;
using CecoChat.History.Server.Clients;
using CecoChat.History.Server.Initialization;
using CecoChat.Jwt;
using CecoChat.Otel;
using CecoChat.Server;
using CecoChat.Server.Identity;
using CecoChat.Tracing;
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
        private readonly IJaegerOptions _jaegerOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            JwtOptions jwtOptions = new();
            Configuration.GetSection("Jwt").Bind(jwtOptions);
            _jwtOptions = jwtOptions;

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
                otel.AddServiceResource(new OtelServiceResource { Namespace = "CecoChat", Service = "History", Version = "0.1" });
                otel.AddAspNetCoreInstrumentation(aspnet => aspnet.EnableGrpcAspNetCoreSupport = true);
                otel.AddHistoryInstrumentation();
                otel.ConfigureJaegerExporter(_jaegerOptions);
            });

            // ordered hosted services
            services.AddHostedService<ConfigurationHostedService>();
            services.AddHostedService<InitializeDbHostedService>();
            services.AddHostedService<PrepareQueriesHostedService>();

            // security
            services.AddJwtAuthentication(_jwtOptions);
            services.AddAuthorization();

            // configuration
            services.AddConfiguration(Configuration.GetSection("ConfigurationDB"), addHistory: true);

            // clients
            services.AddGrpc();
            services.Configure<ClientOptions>(Configuration.GetSection("Clients"));

            // history
            services.AddCassandra<ICecoChatDbContext, CecoChatDbContext>(Configuration.GetSection("HistoryDB"));
            services.AddSingleton<ICecoChatDbInitializer, CecoChatDbInitializer>();
            services.AddSingleton<IHistoryRepository, HistoryRepository>();
            services.AddSingleton<IDataUtility, DataUtility>();
            services.AddSingleton<IBackendDbMapper, BackendDbMapper>();

            // shared
            services.AddSingleton<IActivityUtility, ActivityUtility>();
            services.AddSingleton<IClientBackendMapper, ClientBackendMapper>();
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
