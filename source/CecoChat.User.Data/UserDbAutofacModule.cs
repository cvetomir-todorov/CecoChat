using Autofac;
using CecoChat.User.Data.Entities.Connections;
using CecoChat.User.Data.Entities.Files;
using CecoChat.User.Data.Entities.Profiles;
using CecoChat.User.Data.Infra;
using Common.Autofac;
using Common.Npgsql;
using Common.Redis;
using Microsoft.Extensions.Configuration;

namespace CecoChat.User.Data;

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
            .WithNamedParameter(typeof(IRedisContext), RedisContextName)
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
