using Autofac;
using CecoChat.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Cassandra
{
    public class CassandraAutofacModule<TDbContextImplementation, TDbContext> : Module
        where TDbContext : class, ICassandraDbContext
        where TDbContextImplementation : CassandraDbContext, TDbContext
    {
        public IConfiguration CassandraConfiguration { get; init; }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TDbContextImplementation>().As<TDbContext>().SingleInstance();
            builder.RegisterOptions<CassandraOptions>(CassandraConfiguration);
        }
    }
}
