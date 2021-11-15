using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.Config;
using CecoChat.Jwt;
using CecoChat.Server.Backplane;
using CecoChat.Server.Bff.HostedServices;
using CecoChat.Swagger;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Server.Bff
{
    public class Startup
    {
        private readonly SwaggerOptions _swaggerOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            _swaggerOptions = new();
            Configuration.GetSection("Swagger").Bind(_swaggerOptions);
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // web
            services
                .AddControllers()
                .AddFluentValidation(fluentValidation =>
                {
                    fluentValidation.DisableDataAnnotationsValidation = true;
                    fluentValidation.RegisterValidatorsFromAssemblyContaining<Startup>();
                });
            services.AddSwaggerServices(_swaggerOptions);

            // required
            services.AddOptions();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // ordered hosted services
            builder.RegisterHostedService<InitDynamicConfig>();

            // configuration
            builder.RegisterModule(new ConfigDbAutofacModule
            {
                RedisConfiguration = Configuration.GetSection("ConfigDB"),
                RegisterPartitioning = true
            });

            // backplane
            builder.RegisterModule(new PartitionUtilityAutofacModule());

            // security
            builder.RegisterOptions<JwtOptions>(Configuration.GetSection("Jwt"));

            // shared
            builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.MapWhen(context => context.Request.Path.StartsWithSegments("/swagger"), _ =>
            {
                app.UseSwaggerMiddlewares(_swaggerOptions);
            });

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}