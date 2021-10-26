using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.State;
using CecoChat.Jwt;
using CecoChat.Server.Identity;
using CecoChat.Server.State.Clients;
using CecoChat.Server.State.HostedServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Server.State
{
    public class Startup
    {
        private readonly JwtOptions _jwtOptions;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;

            _jwtOptions = new();
            configuration.GetSection("Jwt").Bind(_jwtOptions);
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
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
            builder.RegisterHostedService<InitStateDb>();

            // state
            builder.RegisterModule(new StateDbAutofacModule
            {
                StateDbConfiguration = Configuration.GetSection("StateDB")
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GrpcStateService>();
            });
        }
    }
}