using System.Reflection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Common.Npgsql;

public interface INpgsqlDbInitializer
{
    bool Initialize(NpgsqlOptions options, string database, Assembly scriptSource);
}

public sealed class NpgsqlDbInitializer : INpgsqlDbInitializer
{
    private readonly ILogger _logger;

    public NpgsqlDbInitializer(ILogger<NpgsqlDbInitializer> logger)
    {
        _logger = logger;
    }

    public bool Initialize(NpgsqlOptions options, string database, Assembly scriptSource)
    {
        using NpgsqlConnection genericConnection = new(options.ConnectionString);
        genericConnection.Open();

        if (CheckIfDatabaseExists(genericConnection, database))
        {
            _logger.LogInformation("Database {Database} already exists", database);
            return false;
        }

        _logger.LogInformation("Initializing database {Database}...", database);

        List<SqlScript> genericSqlScripts = GetGenericSqlScripts(scriptSource);
        _logger.LogInformation("Loaded {SqlScriptCount} generic SQL scripts for {Database} database initialization", genericSqlScripts.Count, database);
        foreach (SqlScript sql in genericSqlScripts)
        {
            ExecuteSqlScript(genericConnection, sql);
        }

        NpgsqlConnectionStringBuilder builder = new(options.ConnectionString);
        builder.Database = database;
        using NpgsqlConnection dbSpecificConnection = new(builder.ConnectionString);
        dbSpecificConnection.Open();

        List<SqlScript> dbSpecificSqlScripts = GetDbSpecificSqlScripts(scriptSource);
        _logger.LogInformation("Loaded {SqlScriptCount} DB-specific SQL scripts for {Database} database initialization", dbSpecificSqlScripts.Count, database);
        foreach (SqlScript sql in dbSpecificSqlScripts)
        {
            ExecuteSqlScript(dbSpecificConnection, sql);
        }

        _logger.LogInformation("Initialized database {Database}", database);
        return true;
    }

    private static bool CheckIfDatabaseExists(NpgsqlConnection connection, string database)
    {
        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(datname) FROM pg_catalog.pg_database WHERE lower(datname) = lower(@Database);";
        NpgsqlParameter databaseParameter = new("@Database", database);
        command.Parameters.Add(databaseParameter);

        int matchingDbs = Convert.ToInt32(command.ExecuteScalar());
        bool exists = matchingDbs > 0;
        return exists;
    }

    private readonly struct SqlScript
    {
        public string Name { get; init; }
        public string Content { get; init; }
    }

    private static List<SqlScript> GetGenericSqlScripts(Assembly scriptSource)
    {
        string[] resources = scriptSource.GetManifestResourceNames();
        IEnumerable<string> userScripts = resources.Where(name => name.Contains("user-"));
        IEnumerable<string> databaseScripts = resources.Where(name => name.Contains("database-"));

        // put users in front
        List<string> scriptNames = userScripts.Union(databaseScripts).ToList();

        return GetSqlScripts(scriptNames, scriptSource);
    }

    private static List<SqlScript> GetDbSpecificSqlScripts(Assembly scriptSource)
    {
        List<string> scriptNames = scriptSource
            .GetManifestResourceNames()
            .Where(name => name.Contains("table-"))
            .OrderBy(name => name)
            .ToList();

        return GetSqlScripts(scriptNames, scriptSource);
    }

    private static List<SqlScript> GetSqlScripts(List<string> scriptNames, Assembly scriptSource)
    {
        return scriptNames
            .Select(resourceName =>
            {
                using Stream? resourceStream = scriptSource.GetManifestResourceStream(resourceName);
                if (resourceStream == null)
                {
                    throw new InvalidOperationException($"Failed to load SQL script {resourceName}.");
                }

                using StreamReader reader = new(resourceStream);
                string content = reader.ReadToEnd();

                return new SqlScript
                {
                    Name = resourceName,
                    Content = content
                };
            })
            .ToList();
    }

    private void ExecuteSqlScript(NpgsqlConnection connection, SqlScript sql)
    {
        try
        {
            using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = sql.Content;

            command.ExecuteNonQuery();
            _logger.LogInformation("Executed SQL script {SqlScriptName}: {NewLine}{SqlScriptContent}", sql.Name, Environment.NewLine, sql.Content);
        }
        catch (NpgsqlException npgsqlException)
        {
            _logger.LogWarning(npgsqlException, "Creation error: {Error}", npgsqlException.Message);
        }
    }
}
