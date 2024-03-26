using Autofac;
using Common.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Client.User;

public sealed class UserClientAutofacModule : Module
{
    private readonly IConfiguration _userClientConfiguration;

    public UserClientAutofacModule(IConfiguration userClientConfiguration)
    {
        _userClientConfiguration = userClientConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterOptions<UserClientOptions>(_userClientConfiguration);

        builder.RegisterType<AuthClient>().As<IAuthClient>().SingleInstance();
        builder.RegisterType<ProfileClient>().As<IProfileClient>().SingleInstance();
        builder.RegisterType<ConnectionClient>().As<IConnectionClient>().SingleInstance();
        builder.RegisterType<FileClient>().As<IFileClient>().SingleInstance();
    }
}
