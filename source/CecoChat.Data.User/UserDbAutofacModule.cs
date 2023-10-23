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
        builder.RegisterType<DataUtility>().As<IDataUtility>().SingleInstance();

        RegisterProfileRepos(builder);
        RegisterConnectionRepos(builder);
    }

    private static void RegisterProfileRepos(ContainerBuilder builder)
    {
        builder.RegisterType<ProfileCommandRepo>().As<IProfileCommandRepo>().InstancePerLifetimeScope();
        builder
            .RegisterType<ProfileCache>()
            .As<IProfileCache>()
            .WithNamedParameter(typeof(IRedisContext), RedisContextName)
            .SingleInstance();

        builder
            .RegisterType<CachingProfileQueryRepo>()
            .Named<IProfileQueryRepo>("c-pr-repo")
            .InstancePerLifetimeScope();
        builder
            .RegisterType<ProfileQueryRepo>()
            .Named<IProfileQueryRepo>("pr-repo")
            .InstancePerLifetimeScope();
        builder
            .RegisterDecorator<IProfileQueryRepo>(
                decorator: (context, inner) => context.ResolveNamed<IProfileQueryRepo>("c-pr-repo", TypedParameter.From(inner)),
                fromKey: "pr-repo")
            .InstancePerLifetimeScope();
    }

    private static void RegisterConnectionRepos(ContainerBuilder builder)
    {
        builder
            .RegisterType<ConnectionCommandRepo>()
            .As<IConnectionCommandRepo>()
            .InstancePerLifetimeScope();

        builder
            .RegisterType<CachingConnectionQueryRepo>()
            .Named<IConnectionQueryRepo>("c-conn-q-repo")
            .WithNamedParameter(typeof(IRedisContext), RedisContextName)
            .InstancePerLifetimeScope();
        builder
            .RegisterType<ConnectionQueryRepo>()
            .Named<IConnectionQueryRepo>("conn-q-repo")
            .InstancePerLifetimeScope();
        builder
            .RegisterDecorator<IConnectionQueryRepo>(
                decorator: (context, inner) => context.ResolveNamed<IConnectionQueryRepo>("c-conn-q-repo", TypedParameter.From(inner)),
                fromKey: "conn-q-repo")
            .InstancePerLifetimeScope();
    }
}
