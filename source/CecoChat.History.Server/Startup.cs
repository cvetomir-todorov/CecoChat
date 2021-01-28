using CecoChat.Cassandra;
using CecoChat.Data.Messaging;
using CecoChat.History.Server.Clients;
using CecoChat.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.History.Server
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
            services.Configure<ClientOptions>(Configuration.GetSection("Clients"));

            // database
            services.AddCassandra<ICecoChatDbContext, CecoChatDbContext>(Configuration.GetSection("Data.Messaging"));
            services.AddSingleton<IHistoryRepository, HistoryRepository>();
            services.AddSingleton<IDataUtility, DataUtility>();

            // shared
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
                endpoints.MapGrpcService<GrpcHistoryService>();
            });
        }
    }
}
