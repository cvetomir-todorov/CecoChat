using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.Data.Configuration;
using CecoChat.Data.Configuration.Partitioning;
using CecoChat.DependencyInjection;
using CecoChat.Events;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Backend;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Messaging.Server.Initialization;
using CecoChat.Redis;
using CecoChat.Server;
using CecoChat.Server.Backend;
using CecoChat.Server.Identity;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Messaging.Server
{
    public class Startup
    {
        private readonly IJwtOptions _jwtOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            JwtOptions jwtOptions = new();
            Configuration.GetSection("Jwt").Bind(jwtOptions);
            _jwtOptions = jwtOptions;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // ordered hosted services
            services.AddHostedService<ConfigurationHostedService>();
            services.AddHostedService<MessagesToReceiversHostedService>();

            // clients
            services.AddGrpc();
            services.AddSingleton<IClientContainer, ClientContainer>();
            services.AddFactory<IGrpcStreamer<ListenResponse>, GrpcStreamer<ListenResponse>>();
            services.Configure<ClientOptions>(Configuration.GetSection("Clients"));

            // security
            services.AddJwtAuthentication(_jwtOptions);
            services.AddAuthorization();

            // backend
            services.AddPartitionUtility();
            services.AddSingleton<ITopicPartitionFlyweight, TopicPartitionFlyweight>();
            services.AddSingleton<IBackendProducer, MessagesToBackendProducer>();
            services.AddSingleton<IBackendConsumer, MessagesToReceiversConsumer>();
            services.AddFactory<IKafkaConsumer<Null, BackendMessage>, KafkaConsumer<Null, BackendMessage>>();
            services.Configure<BackendOptions>(Configuration.GetSection("Backend"));

            // configuration
            services.AddConfiguration(Configuration.GetSection("ConfigurationDB"), addPartitioning: true);

            // shared
            services.AddSingleton<IClock, MonotonicClock>();
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
                endpoints.MapGrpcService<GrpcListenService>();
                endpoints.MapGrpcService<GrpcSendService>();
            });
        }
    }
}
