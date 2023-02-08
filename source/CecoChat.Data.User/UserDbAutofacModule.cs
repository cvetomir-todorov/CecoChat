using Autofac;
using CecoChat.Data.User.Repos;

namespace CecoChat.Data.User;

public class UserDbAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ProfileRepo>().As<IProfileRepo>().SingleInstance();
    }
}
