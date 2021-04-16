using Autofac;
using CecoChat.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Redis
{
    public sealed class RedisAutofacModule : Module
    {
        public IConfiguration RedisConfiguration { get; init; }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RedisContext>().As<IRedisContext>().SingleInstance();
            builder.RegisterOptions<RedisOptions>(RedisConfiguration);
        }
    }
}
