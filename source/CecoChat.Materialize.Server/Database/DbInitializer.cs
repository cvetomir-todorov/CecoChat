using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cassandra;
using CecoChat.Cassandra;
using Microsoft.Extensions.Logging;

namespace CecoChat.Materialize.Server.Database
{
    public interface IDbInitializer
    {
        void Initialize();
    }

    public sealed class DbInitializer : IDbInitializer
    {
        private readonly ILogger _logger;
        private readonly ICassandraDbContext _dbContext;

        public DbInitializer(
            ILogger<DbInitializer> logger,
            ICassandraDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public void Initialize()
        {
            const string keyspace = "messaging";
            if (_dbContext.ExistsKeyspace(keyspace))
            {
                _logger.LogInformation("Keyspace {0} already initialized.", keyspace);
                return;
            }

            _logger.LogInformation("Keyspace {0} needs initialization.", keyspace);
            List<CqlScript> cqls = GetCqlScripts(keyspace);
            _logger.LogInformation("Loaded {0} CQL scripts for keyspace {1} initialization.", cqls.Count, keyspace);
            ISession session = _dbContext.GetSession();

            foreach (CqlScript cql in cqls)
            {
                ExecuteCqlScript(session, cql);
            }
        }

        private struct CqlScript
        {
            public string Name { get; init; }
            public string Content { get; init; }
        }

        private static List<CqlScript> GetCqlScripts(string keyspace)
        {
            Assembly targetAssembly = Assembly.GetExecutingAssembly();
            string messagingPrefix = keyspace + "-";
            List<string> allScripts = targetAssembly
                .GetManifestResourceNames()
                .Where(name => name.Contains(messagingPrefix) &&
                               name.EndsWith(".cql"))
                .ToList();

            IEnumerable<string> keyspaces = allScripts.Where(name => name.Contains("keyspace"));
            IEnumerable<string> tables = allScripts.Where(name => name.Contains("table"));
            IEnumerable<string> allOrdered = keyspaces.Union(tables);

            List<CqlScript> cqls = allOrdered
                .Select(resourceName =>
                {
                    using Stream resourceStream = targetAssembly.GetManifestResourceStream(resourceName);
                    using StreamReader reader = new StreamReader(resourceStream);
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
            session.Execute(cql.Content, ConsistencyLevel.All);
            _logger.LogDebug("Executed {0} CQL script: {1}{2}", cql.Name, Environment.NewLine, cql.Content);
        }
    }
}
