using CecoChat.Cassandra;
using CecoChat.Contracts.Backend;
using CecoChat.Data.Messaging;
using CecoChat.DependencyInjection;
using CecoChat.Kafka;
using CecoChat.Materialize.Server.Backend;
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
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // database
            services.AddCassandra<ICecoChatDbContext, CecoChatDbContext>(Configuration.GetSection("Data.Messaging"));
            services.AddSingleton<ICecoChatDbInitializer, CecoChatDbInitializer>();
            services.AddSingleton<INewMessageRepository, NewMessageRepository>();
            services.AddSingleton<IDataUtility, DataUtility>();
            services.AddSingleton<IBackendDbMapper, BackendDbMapper>();

            // backend
            services.AddSingleton<IBackendConsumer, MaterializeMessagesConsumer>();
            services.AddFactory<IKafkaConsumer<Null, BackendMessage>, KafkaConsumer<Null, BackendMessage>>();
            services.AddHostedService<MaterializeMessagesHostedService>();
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
