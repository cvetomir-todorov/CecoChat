using CecoChat.Messaging.Server.Clients;
using CecoChat.Messaging.Server.Servers;
using CecoChat.Messaging.Server.Servers.Consumption;
using CecoChat.Messaging.Server.Servers.Production;
using CecoChat.Messaging.Server.Shared;
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
            services.AddGrpc();
            services.AddSingleton<IClientContainer, ClientContainer>();
            services.AddSingleton<IPartitionUtility, PartitionUtility>();
            services.AddSingleton<IClientBackendMapper, ClientBackendMapper>();
            services.AddSingleton<IBackendProducer, KafkaProducer>();
            services.AddSingleton<IBackendConsumer, KafkaConsumer>();
            services.AddHostedService<BackendConsumptionHostedService>();
            services.Configure<KafkaOptions>(Configuration.GetSection("Kafka"));
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
                endpoints.MapGrpcService<GrpcChatService>();
            });
        }
    }
}
