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
            .RegisterType<CachingProfileRepo>()
            .Named<IProfileRepo>("caching-profile-repo")
            .WithNamedParameter(typeof(IRedisContext), RedisContextName)
            .InstancePerLifetimeScope();
        builder
            .RegisterType<ProfileRepo>()
            .Named<IProfileRepo>("profile-repo")
            .InstancePerLifetimeScope();
        builder
            .RegisterDecorator<IProfileRepo>(
                decorator: (context, inner) => context.ResolveNamed<IProfileRepo>("caching-profile-repo", TypedParameter.From(inner)),
                fromKey: "profile-repo")
            .InstancePerLifetimeScope();
    }
}
