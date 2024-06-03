using Autofac;
using Common.Autofac;
using Microsoft.Extensions.Configuration;

namespace Common.Cassandra;

public class CassandraAutofacModule<TDbContextImplementation, TDbContext> : Module
    where TDbContext : class, ICassandraDbContext
    where TDbContextImplementation : CassandraDbContext, TDbContext
{
    private readonly IConfiguration _cassandraConfiguration;
    private readonly string _dbContextName;

    public CassandraAutofacModule(IConfiguration cassandraConfiguration, string? dbContextName = null)
    {
        _cassandraConfiguration = cassandraConfiguration;

        dbContextName ??= typeof(TDbContext).Name;
        _dbContextName = dbContextName;
    }

    public string DbContextName => _dbContextName;

    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<TDbContextImplementation>()
            .As<TDbContext>()
            .Named<ICassandraDbContext>(_dbContextName)
            .SingleInstance();
        builder.RegisterOptions<CassandraOptions<TDbContextImplementation>>(_cassandraConfiguration);
    }
}
