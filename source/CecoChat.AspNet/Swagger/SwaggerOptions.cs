using Microsoft.OpenApi.Models;

namespace CecoChat.AspNet.Swagger;

public sealed class SwaggerOptions
{
    public bool UseSwagger { get; set; }

    public bool UseSwaggerUi { get; set; }

    public Uri? Url { get; set; }

    public bool AddAuthorizationHeader { get; set; }

    public OpenApiInfo OpenApiInfo { get; set; } = new();
}
