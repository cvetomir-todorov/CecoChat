using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.User.Repos;
using CecoChat.Npgsql;
using CecoChat.Redis;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.User;

public class UserDbAutofacModule : Module
{
    private readonly IConfiguration _userCacheConfig;

    public UserDbAutofacModule(IConfiguration userCacheConfig)
    {
        _userCacheConfig = userCacheConfig;
    }

    public static readonly string RedisContextName = "user-cache";

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NpgsqlDbInitializer>().As<INpgsqlDbInitializer>().SingleInstance();
        builder.RegisterModule(new RedisAutofacModule(_userCacheConfig, RedisContextName));
        builder
            .RegisterType<ProfileRepo>()
            .As<IProfileRepo>()
            .WithNamedParameter(typeof(IRedisContext), RedisContextName)
            .InstancePerLifetimeScope();
    }
}
