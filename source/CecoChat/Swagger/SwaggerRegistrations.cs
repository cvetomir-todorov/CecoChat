using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace CecoChat.Swagger;

public static class SwaggerRegistrations
{
    public static void AddSwaggerServices(this IServiceCollection services, SwaggerOptions options)
    {
        if (options.UseSwagger)
        {
            services.AddSwaggerGen(config =>
            {
                if (options.AddAuthorizationHeader)
                {
                    config.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\""
                    });

                    config.OperationFilter<SecurityRequirementsOperationFilter>();
                }

                config.SwaggerDoc(options.OpenApiInfo.Version, options.OpenApiInfo);
            });
            services.ConfigureSwaggerGen(config =>
            {
                config.CustomSchemaIds(type => type.FullName);
            });
        }
    }

    public static void UseSwaggerMiddlewares(this IApplicationBuilder app, SwaggerOptions options)
    {
        if (options.UseSwagger)
        {
            app.UseSwagger();
        }

        if (options.UseSwaggerUI)
        {
            if (options.Url == null)
            {
                throw new InvalidOperationException("Missing Swagger URL.");
            }

            app.UseSwaggerUI(config => config.SwaggerEndpoint(
                url: options.Url.ToString(),
                name: $"{options.OpenApiInfo.Title} {options.OpenApiInfo.Version}"));
        }
    }
}