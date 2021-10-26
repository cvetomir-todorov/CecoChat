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

        public string DbContextName { get; init; } = typeof(TDbContext).Name;

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TDbContextImplementation>().As<TDbContext>()
                .Named<ICassandraDbContext>(DbContextName)
                .SingleInstance();
            builder.RegisterOptions<CassandraOptions>(CassandraConfiguration);
        }
    }
}
