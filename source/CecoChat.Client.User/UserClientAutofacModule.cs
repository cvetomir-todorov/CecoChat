using Autofac;
using CecoChat.Autofac;
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
        builder.RegisterType<UserClient>().As<IUserClient>().SingleInstance();
        builder.RegisterOptions<UserOptions>(_userClientConfiguration);
    }
}
