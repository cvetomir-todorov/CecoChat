using Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Cassandra.Health;

public class CassandraHealthAutofacModule : Module
{
    private readonly IConfiguration _healthConfiguration;

    public CassandraHealthAutofacModule(IConfiguration healthConfiguration)
    {
        _healthConfiguration = healthConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        CassandraAutofacModule<CassandraHealthDbContext, ICassandraHealthDbContext> dbContextModule = new(_healthConfiguration);
        builder.RegisterModule(dbContextModule);
    }
}
