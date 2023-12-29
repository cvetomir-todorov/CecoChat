using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.User.Connections;
using CecoChat.Data.User.Files;
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
        builder.RegisterModule(new RedisAutofacModule(_userCacheStoreConfig, RedisContextName, registerConnectionMultiplexer: true));
        builder.RegisterOptions<UserCacheOptions>(_userCacheConfig);
        builder.RegisterType<DataUtility>().As<IDataUtility>().SingleInstance();

        RegisterProfileRepos(builder);
        RegisterConnectionRepos(builder);
        RegisterFileRepos(builder);
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
            .Named<IProfileQueryRepo>("ca-pr-repo")
            .InstancePerLifetimeScope();
        builder
            .RegisterType<ProfileQueryRepo>()
            .Named<IProfileQueryRepo>("pr-repo")
            .InstancePerLifetimeScope();
        builder
            .RegisterDecorator<IProfileQueryRepo>(
                decorator: (context, inner) => context.ResolveNamed<IProfileQueryRepo>("ca-pr-repo", TypedParameter.From(inner)),
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
            .RegisterType<ConnectionQueryRepo>()
            .As<IConnectionQueryRepo>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<CachingConnectionQueryRepo>()
            .As<ICachingConnectionQueryRepo>()
            .WithNamedParameter(typeof(IRedisContext), RedisContextName)
            .InstancePerLifetimeScope();
    }

    private static void RegisterFileRepos(ContainerBuilder builder)
    {
        builder
            .RegisterType<FileCommandRepo>()
            .As<IFileCommandRepo>()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<FileQueryRepo>()
            .As<IFileQueryRepo>()
            .InstancePerLifetimeScope();
    }
}
