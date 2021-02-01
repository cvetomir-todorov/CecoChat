using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.DependencyInjection;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Backend;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Server;
using CecoChat.Server.Backend;
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
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // clients
            services.AddGrpc();
            services.AddSingleton<IClientContainer, ClientContainer>();
            services.AddFactory<IGrpcStreamer<ListenResponse>, GrpcStreamer<ListenResponse>>();
            services.Configure<ClientOptions>(Configuration.GetSection("Clients"));

            // backend
            services.AddSingleton<IPartitionUtility, PartitionUtility>();
            services.AddSingleton<ITopicPartitionFlyweight, TopicPartitionFlyweight>();
            services.AddSingleton<IBackendProducer, MessagesToBackendProducer>();
            services.AddSingleton<IBackendConsumer, MessagesToReceiversConsumer>();
            services.AddFactory<IKafkaConsumer<Null, BackendMessage>, KafkaConsumer<Null, BackendMessage>>();
            services.AddHostedService<MessagesToReceiversHostedService>();
            services.Configure<BackendOptions>(Configuration.GetSection("Backend"));

            // shared
            services.AddSingleton<IClock, MonotonicClock>();
            services.AddSingleton<INonCryptoHash, FnvHash>();
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
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GrpcListenService>();
                endpoints.MapGrpcService<GrpcSendService>();
            });
        }
    }
}
