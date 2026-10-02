using Amazon.Runtime;
using Amazon.S3;
using Autofac;
using Common.Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Common.Seaweed;

public class SeaweedAutofacModule : Module
{
    private readonly IConfiguration _seaweedConfiguration;

    public SeaweedAutofacModule(IConfiguration seaweedConfiguration)
    {
        _seaweedConfiguration = seaweedConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SeaweedContext>().As<ISeaweedContext>().SingleInstance();
        builder.RegisterOptions<SeaweedOptions>(_seaweedConfiguration);
        builder
            .Register(context =>
            {
                SeaweedOptions options = context.Resolve<IOptions<SeaweedOptions>>().Value;

                AWSCredentials credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
                AmazonS3Config config = new()
                {
                    ServiceURL = options.Endpoint.ToString(),
                    // below options are required for SeaweedFS
                    AuthenticationRegion = "us-east-1", // obtained from the URL, if not provided, not all servers need it
                    ForcePathStyle = true,
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED, // by default the client sends checksum, but not all servers accept them
                    ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED  // same as above
                };

                return new AmazonS3Client(credentials, config);
            })
            .As<IAmazonS3>()
            .SingleInstance();
    }
}
