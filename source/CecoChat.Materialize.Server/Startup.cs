using CecoChat.Cassandra;
using CecoChat.Contracts.Backend;
using CecoChat.Data.History;
using CecoChat.DependencyInjection;
using CecoChat.Kafka;
using CecoChat.Kafka.Instrumentation;
using CecoChat.Materialize.Server.Backend;
using CecoChat.Materialize.Server.Initialization;
using CecoChat.Otel;
using CecoChat.Tracing;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Materialize.Server
{
    public class Startup
    {
        private readonly IJaegerOptions _jaegerOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

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
                otel.AddServiceResource(new OtelServiceResource {Namespace = "CecoChat", Service = "Materialize", Version = "0.1"});
                otel.ConfigureJaegerExporter(_jaegerOptions);
                // TODO: add instrumentation for cassandra
                // TODO: set different samplers for debug and production
            });

            // ordered hosted services
            services.AddHostedService<InitializeDbHostedService>();
            services.AddHostedService<PrepareQueriesHostedService>();
            services.AddHostedService<MaterializeMessagesHostedService>();

            // history
            services.AddCassandra<ICecoChatDbContext, CecoChatDbContext>(Configuration.GetSection("HistoryDB"));
            services.AddSingleton<ICecoChatDbInitializer, CecoChatDbInitializer>();
            services.AddSingleton<INewMessageRepository, NewMessageRepository>();
            services.AddSingleton<IDataUtility, DataUtility>();
            services.AddSingleton<IBackendDbMapper, BackendDbMapper>();

            // backend
            services.AddSingleton<IMaterializeMessagesConsumer, MaterializeMessagesConsumer>();
            services.AddFactory<IKafkaConsumer<Null, BackendMessage>, KafkaConsumer<Null, BackendMessage>>();
            services.AddSingleton<IKafkaActivityUtility, KafkaActivityUtility>();
            services.Configure<BackendOptions>(Configuration.GetSection("Backend"));

            // shared
            services.AddSingleton<IActivityUtility, ActivityUtility>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
        }
    }
}
