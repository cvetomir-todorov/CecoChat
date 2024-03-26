using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Common.AspNet.Swagger;

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
                    config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        Description = "Standard Authorization header using the Bearer scheme"
                    });
                    config.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                }

                if (options.GroupByApiExplorerGroup)
                {
                    config.OperationFilter<TagByApiExplorerSettingsOperationFilter>();
                    config.DocInclusionPredicate((_, api) => !string.IsNullOrWhiteSpace(api.GroupName));
                }

                config.EnableAnnotations();
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

        if (options.UseSwaggerUi)
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
