using CecoChat.Connect.Server.Initialization;
using CecoChat.Data.Configuration;
using CecoChat.Data.Configuration.History;
using CecoChat.Data.Configuration.Partitioning;
using CecoChat.Events;
using CecoChat.Jwt;
using CecoChat.Redis;
using CecoChat.Server.Backend;
using CecoChat.Server.Identity;
using CecoChat.Swagger;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Connect.Server
{
    public class Startup
    {
        private readonly IJwtOptions _jwtOptions;
        private readonly ISwaggerOptions _swaggerOptions;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            JwtOptions jwtOptions = new();
            Configuration.GetSection("Jwt").Bind(jwtOptions);
            _jwtOptions = jwtOptions;

            SwaggerOptions swaggerOptions = new();
            Configuration.GetSection("Swagger").Bind(swaggerOptions);
            _swaggerOptions = swaggerOptions;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // web
            services
                .AddControllers()
                .AddFluentValidation(fluentValidation =>
                {
                    fluentValidation.RunDefaultMvcValidationAfterFluentValidationExecutes = false;
                    fluentValidation.RegisterValidatorsFromAssemblyContaining<Startup>();
                });
            services.AddSwaggerServices(_swaggerOptions);

            // security
            services.AddJwtAuthentication(_jwtOptions);

            // configuration
            services.AddHostedService<ConfigurationHostedService>();
            services.AddSingleton<IPartitioningConfiguration, PartitioningConfiguration>();
            services.AddSingleton<IPartitioningConfigurationRepository, PartitioningConfigurationRepository>();
            services.AddSingleton<IHistoryConfiguration, HistoryConfiguration>();
            services.AddSingleton<IHistoryConfigurationRepository, HistoryConfigurationRepository>();
            services.AddSingleton<IConfigurationUtility, ConfigurationUtility>();
            services.AddEvent<EventSource<PartitionsChangedEventData>, PartitionsChangedEventData>();
            services.AddRedis(Configuration.GetSection("ConfigurationDB"));

            // backend
            services.AddPartitionUtility();
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
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
