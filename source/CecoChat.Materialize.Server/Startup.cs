using CecoChat.Cassandra;
using CecoChat.Data.Messaging;
using CecoChat.Materialize.Server.Backend;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Materialize.Server
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
            // database
            services.AddCassandra<ICecoChatDbContext, CecoChatDbContext>(Configuration.GetSection("Cassandra"));
            services.AddSingleton<ICecoChatDbInitializer, CecoChatDbInitializer>();
            services.AddSingleton<INewMessageRepository, NewMessageRepository>();

            // backend
            services.AddSingleton<IBackendConsumer, KafkaConsumer>();
            services.AddSingleton<IProcessor, CassandraStateProcessor>();
            services.AddHostedService<PersistMessagesHostedService>();
            services.Configure<BackendOptions>(Configuration.GetSection("Backend"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ICecoChatDbInitializer db = app.ApplicationServices.GetRequiredService<ICecoChatDbInitializer>();
            db.Initialize();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
        }
    }
}
