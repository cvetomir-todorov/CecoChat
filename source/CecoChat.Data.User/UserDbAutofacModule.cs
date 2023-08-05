using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.User.Repos;
using CecoChat.Data.User.Security;
using CecoChat.Npgsql;
using CecoChat.Redis;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.User;

public class UserDbAutofacModule : Module
{
    private readonly IConfiguration _userCacheConfig;
    private readonly IConfiguration _userCacheStoreConfig;

    public UserDbAutofacModule(IConfiguration userCacheConfig, IConfiguration userCacheStoreConfig)
    {
        _userCacheConfig = userCacheConfig;
        _userCacheStoreConfig = userCacheStoreConfig;
    }

    public static readonly string RedisContextName = "user-cache";

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NpgsqlDbInitializer>().As<INpgsqlDbInitializer>().SingleInstance();
        builder.RegisterModule(new RedisAutofacModule(_userCacheStoreConfig, RedisContextName));
        builder.RegisterOptions<UserCacheOptions>(_userCacheConfig);

        builder.RegisterType<ProfileCommandRepo>().As<IProfileCommandRepo>().InstancePerLifetimeScope();
        builder.RegisterType<PasswordHasher>().As<IPasswordHasher>().SingleInstance();

        builder
            .RegisterType<CachingProfileQueryRepo>()
            .Named<IProfileQueryRepo>("caching-profile-repo")
            .WithNamedParameter(typeof(IRedisContext), RedisContextName)
            .InstancePerLifetimeScope();
        builder
            .RegisterType<ProfileQueryRepo>()
            .Named<IProfileQueryRepo>("profile-repo")
            .InstancePerLifetimeScope();
        builder
            .RegisterDecorator<IProfileQueryRepo>(
                decorator: (context, inner) => context.ResolveNamed<IProfileQueryRepo>("caching-profile-repo", TypedParameter.From(inner)),
                fromKey: "profile-repo")
            .InstancePerLifetimeScope();
    }
}
