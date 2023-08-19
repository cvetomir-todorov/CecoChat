using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.User.Connections;
using CecoChat.Data.User.Infra;
using CecoChat.Data.User.Profiles;
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
        builder
            .RegisterType<ProfileCache>()
            .As<IProfileCache>()
            .WithNamedParameter(typeof(IRedisContext), RedisContextName)
            .SingleInstance();

        builder
            .RegisterType<CachingProfileQueryRepo>()
            .Named<IProfileQueryRepo>("caching-profile-repo")
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

        builder.RegisterType<ConnectionQueryRepo>().As<IConnectionQueryRepo>().InstancePerLifetimeScope();
        builder.RegisterType<ConnectionCommandRepo>().As<IConnectionCommandRepo>().InstancePerLifetimeScope();

        builder.RegisterType<DataUtility>().As<IDataUtility>().SingleInstance();
    }
}
