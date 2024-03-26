using Microsoft.OpenApi.Models;

namespace Common.AspNet.Swagger;

public sealed class SwaggerOptions
{
    public bool UseSwagger { get; init; }

    public bool UseSwaggerUi { get; init; }

    public Uri? Url { get; init; }

    public bool AddAuthorizationHeader { get; init; }

    public bool GroupByApiExplorerGroup { get; init; }

    public OpenApiInfo OpenApiInfo { get; init; } = new();
}
