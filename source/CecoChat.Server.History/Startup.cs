using Autofac;
using CecoChat.Autofac;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.Config;
using CecoChat.Data.History;
using CecoChat.Data.History.Instrumentation;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Kafka.Instrumentation;
using CecoChat.Otel;
using CecoChat.Server.History.Backplane;
using CecoChat.Server.History.Clients;
using CecoChat.Server.History.HostedServices;
using CecoChat.Server.Identity;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace CecoChat.Server.History
{
    public class Startup
    {
        private readonly JwtOptions _jwtOptions;
        private readonly OtelSamplingOptions _otelSamplingOptions;
        private readonly JaegerOptions _jaegerOptions;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;

            _jwtOptions = new();
            Configuration.GetSection("Jwt").Bind(_jwtOptions);

            _otelSamplingOptions = new();
            Configuration.GetSection("OtelSampling").Bind(_otelSamplingOptions);

            _jaegerOptions = new();
            Configuration.GetSection("Jaeger").Bind(_jaegerOptions);
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // telemetry
            services.AddOpenTelemetryTracing(otel =>
            {
                otel.AddServiceResource(new OtelServiceResource {Namespace = "CecoChat", Name = "History", Version = "0.1"});
                otel.AddAspNetCoreInstrumentation(aspnet => aspnet.EnableGrpcAspNetCoreSupport = true);
                otel.AddKafkaInstrumentation();
                otel.AddHistoryInstrumentation();
                otel.ConfigureSampling(_otelSamplingOptions);
                otel.ConfigureJaegerExporter(_jaegerOptions);
            });

            // security
            services.AddJwtAuthentication(_jwtOptions);
            services.AddAuthorization();

            // clients
            services.AddGrpc(rpc => rpc.EnableDetailedErrors = Environment.IsDevelopment());

            // required
            services.AddOptions();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // ordered hosted services
            builder.RegisterHostedService<InitDynamicConfig>();
            builder.RegisterHostedService<InitHistoryDb>();
            builder.RegisterHostedService<StartMaterializeMessages>();

            // configuration
            builder.RegisterModule(new ConfigDbAutofacModule
            {
                RedisConfiguration = Configuration.GetSection("ConfigDB"),
                RegisterHistory = true
            });

            // history
            builder.RegisterModule(new HistoryDbAutofacModule
            {
                HistoryDbConfiguration = Configuration.GetSection("HistoryDB"),
            });

            // backplane
            builder.RegisterType<HistoryConsumer>().As<IHistoryConsumer>().SingleInstance();
            builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
            builder.RegisterModule(new KafkaInstrumentationAutofacModule());
            builder.RegisterOptions<BackplaneOptions>(Configuration.GetSection("Backplane"));

            // shared
            builder.RegisterType<ContractDataMapper>().As<IContractDataMapper>().SingleInstance();
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
