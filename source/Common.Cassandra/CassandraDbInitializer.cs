using System.Reflection;
using Cassandra;
using Microsoft.Extensions.Logging;

namespace Common.Cassandra;

public interface ICassandraDbInitializer
{
    void Initialize(string keyspace, Assembly scriptSource);
}

public sealed class CassandraDbInitializer : ICassandraDbInitializer
{
    private readonly ILogger _logger;
    private readonly ICassandraDbContext _dbContext;

    public CassandraDbInitializer(
        ILogger<CassandraDbInitializer> logger,
        ICassandraDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public void Initialize(string keyspace, Assembly scriptSource)
    {
        if (_dbContext.ExistsKeyspace(keyspace))
        {
            _logger.LogInformation("Keyspace {Keyspace} already initialized", keyspace);
            return;
        }

        _logger.LogInformation("Keyspace {Keyspace} needs initialization", keyspace);
        List<CqlScript> cqls = GetCqlScripts(keyspace, scriptSource);
        _logger.LogInformation("Loaded {CqlScriptCount} CQL scripts for keyspace {Keyspace} initialization", cqls.Count, keyspace);
        ISession session = _dbContext.GetSession();

        foreach (CqlScript cql in cqls)
        {
            ExecuteCqlScript(session, cql);
        }
    }

    private readonly struct CqlScript
    {
        public string Name { get; init; }
        public string Content { get; init; }
    }

    private static List<CqlScript> GetCqlScripts(string keyspace, Assembly scriptSource)
    {
        string keyspacePrefix = keyspace + "-";
        List<string> allScripts = scriptSource
            .GetManifestResourceNames()
            .Where(name => name.Contains(keyspacePrefix) &&
                           name.EndsWith(".cql"))
            .ToList();

        IEnumerable<string> keyspaces = allScripts.Where(name => name.EndsWith("-keyspace.cql"));
        IEnumerable<string> types = allScripts.Where(name => name.EndsWith("-type.cql"));
        IEnumerable<string> tables = allScripts.Where(name => name.EndsWith("-table.cql"));
        IEnumerable<string> allOrdered = keyspaces.Union(types).Union(tables);

        List<CqlScript> cqls = allOrdered
            .Select(resourceName =>
            {
                using Stream? resourceStream = scriptSource.GetManifestResourceStream(resourceName);
                if (resourceStream == null)
                {
                    throw new InvalidOperationException($"Failed to load CQL script {resourceName}.");
                }

                using StreamReader reader = new(resourceStream);
                string cql = reader.ReadToEnd();

                return new CqlScript
                {
                    Name = resourceName,
                    Content = cql
                };
            })
            .ToList();

        return cqls;
    }

    private void ExecuteCqlScript(ISession session, CqlScript cql)
    {
        try
        {
            session.Execute(cql.Content, ConsistencyLevel.All);
            _logger.LogInformation("Executed CQL script {CqlScriptName}: {NewLine}{CqlScriptContent}", cql.Name, Environment.NewLine, cql.Content);
        }
        catch (AlreadyExistsException alreadyExistsException)
        {
            _logger.LogWarning(alreadyExistsException, "Creation error: {Error}", alreadyExistsException.Message);
        }
    }
}
