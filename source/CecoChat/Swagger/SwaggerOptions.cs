using Microsoft.OpenApi.Models;

namespace CecoChat.Swagger
{
    public interface ISwaggerOptions
    {
        bool UseSwagger { get; }

        bool UseSwaggerUI { get; }

        string Url { get; }

        OpenApiInfo OpenApiInfo { get; }
    }

    public sealed class SwaggerOptions : ISwaggerOptions
    {
        public bool UseSwagger { get; set; }

        public bool UseSwaggerUI { get; set; }

        public string Url { get; set; }

        public OpenApiInfo OpenApiInfo { get; set; }
    }
}
