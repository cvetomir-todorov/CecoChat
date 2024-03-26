using Autofac;
using Common.Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Minio;

namespace Common.Minio;

public class MinioAutofacModule : Module
{
    private readonly IConfiguration _minioConfiguration;

    public MinioAutofacModule(IConfiguration minioConfiguration)
    {
        _minioConfiguration = minioConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MinioContext>().As<IMinioContext>().SingleInstance();
        builder.RegisterOptions<MinioOptions>(_minioConfiguration);
        builder
            .Register(context =>
            {
                MinioOptions options = context.Resolve<IOptions<MinioOptions>>().Value;
                return new MinioClient()
                    .WithEndpoint(options.Endpoint)
                    .WithCredentials(options.AccessKey, options.Secret)
                    .Build();
            })
            .As<IMinioClient>()
            .SingleInstance();
    }
}
