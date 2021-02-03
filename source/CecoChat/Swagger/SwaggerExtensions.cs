using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Swagger
{
    public static class SwaggerExtensions
    {
        public static void AddSwaggerServices(this IServiceCollection services, ISwaggerOptions options)
        {
            if (options.UseSwagger)
            {
                services.AddSwaggerGen(config =>
                {
                    config.SwaggerDoc(options.OpenApiInfo.Version, options.OpenApiInfo);
                });
                services.ConfigureSwaggerGen(config =>
                {
                    config.CustomSchemaIds(type => type.FullName);
                });
            }
        }

        public static void UseSwaggerMiddlewares(this IApplicationBuilder app, ISwaggerOptions options)
        {
            if (options.UseSwagger)
            {
                app.UseSwagger();
            }

            if (options.UseSwaggerUI)
            {
                app.UseSwaggerUI(config => config.SwaggerEndpoint(
                    url: options.Url,
                    name: $"{options.OpenApiInfo.Title} {options.OpenApiInfo.Version}"));
            }
        }

	}
}
