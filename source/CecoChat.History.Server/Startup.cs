using CecoChat.Cassandra;
using CecoChat.Data.Configuration;
using CecoChat.Data.Configuration.History;
using CecoChat.Data.Messaging;
using CecoChat.History.Server.Clients;
using CecoChat.History.Server.Initialization;
using CecoChat.Jwt;
using CecoChat.Redis;
using CecoChat.Server;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.History.Server
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
            services.AddHostedService<PrepareQueriesHostedService>();

            // clients
            services.AddGrpc();
            services.Configure<ClientOptions>(Configuration.GetSection("Clients"));

            // security
            services.AddJwtAuthentication(_jwtOptions);
            services.AddAuthorization();

            // history
            services.AddCassandra<ICecoChatDbContext, CecoChatDbContext>(Configuration.GetSection("HistoryDB"));
            services.AddSingleton<IHistoryRepository, HistoryRepository>();
            services.AddSingleton<IDataUtility, DataUtility>();
            services.AddSingleton<IBackendDbMapper, BackendDbMapper>();

            // configuration
            services.AddRedis(Configuration.GetSection("ConfigurationDB"));
            services.AddSingleton<IHistoryConfiguration, HistoryConfiguration>();
            services.AddSingleton<IHistoryConfigurationRepository, HistoryConfigurationRepository>();
            services.AddSingleton<IConfigurationUtility, ConfigurationUtility>();

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
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GrpcHistoryService>();
            });
        }
    }
}
