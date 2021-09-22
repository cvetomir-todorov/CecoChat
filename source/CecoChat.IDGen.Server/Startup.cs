using Autofac;
using CecoChat.Autofac;
using CecoChat.IDGen.Server.Generation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.IDGen.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // clients
            services.AddGrpc(rpc => rpc.EnableDetailedErrors = Environment.IsDevelopment());

            // required
            services.AddOptions();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // snowflake
            builder.RegisterType<SnowflakeGenerator>().As<IIdentityGenerator>().SingleInstance();
            builder.RegisterType<FnvHash>().As<INonCryptoHash>().SingleInstance();
            builder.RegisterOptions<SnowflakeOptions>(Configuration.GetSection("Snowflake"));
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
                endpoints.MapGrpcService<GrpcGenerationService>();
            });
        }
    }
}
