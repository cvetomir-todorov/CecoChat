using Autofac;
using CecoChat.Data.User.Repos;
using CecoChat.Npgsql;

namespace CecoChat.Data.User;

public class UserDbAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NpgsqlDbInitializer>().As<INpgsqlDbInitializer>().SingleInstance();
        builder.RegisterType<ProfileRepo>().As<IProfileRepo>().SingleInstance();
    }
}
