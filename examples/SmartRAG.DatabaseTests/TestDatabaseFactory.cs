using Microsoft.Extensions.Configuration;
using SmartRAG.Enums;

namespace SmartRAG.DatabaseTests
{
    /// <summary>
    /// Factory class for creating database creators
    /// Follows SOLID principles - Factory Pattern and Dependency Inversion
    /// </summary>
    public static class TestDatabaseFactory
    {
        /// <summary>
        /// Creates the appropriate database creator based on database type
        /// </summary>
        /// <param name="databaseType">Type of database to create</param>
        /// <param name="configuration">Configuration instance for connection strings</param>
        /// <returns>Database creator instance</returns>
        /// <exception cref="NotSupportedException">Thrown when database type is not supported</exception>
        public static ITestDatabaseCreator GetCreator(DatabaseType databaseType, IConfiguration? configuration = null)
        {
            return databaseType switch
            {
                DatabaseType.SQLite => new SqliteTestDatabaseCreator(configuration),
                DatabaseType.SqlServer => new SqlServerTestDatabaseCreator(configuration),
                DatabaseType.MySQL => new MySqlTestDatabaseCreator(configuration),
                DatabaseType.PostgreSQL => new PostgreSqlTestDatabaseCreator(configuration),
                _ => throw new NotSupportedException($"Database type '{databaseType}' is not supported yet. Supported types: SQLite, SqlServer, MySQL, PostgreSQL")
            };
        }

        /// <summary>
        /// Gets all supported database types with their descriptions
        /// </summary>
        /// <param name="configuration">Configuration instance for connection strings</param>
        /// <returns>Dictionary of supported database types and descriptions</returns>
        public static Dictionary<DatabaseType, string> GetSupportedDatabases(IConfiguration? configuration = null)
        {
            return new Dictionary<DatabaseType, string>
            {
                { DatabaseType.SQLite, new SqliteTestDatabaseCreator(configuration).GetDescription() },
                { DatabaseType.SqlServer, new SqlServerTestDatabaseCreator(configuration).GetDescription() },
                { DatabaseType.MySQL, new MySqlTestDatabaseCreator(configuration).GetDescription() },
                { DatabaseType.PostgreSQL, new PostgreSqlTestDatabaseCreator(configuration).GetDescription() }
            };
        }

        /// <summary>
        /// Validates if a database type is supported
        /// </summary>
        /// <param name="databaseType">Database type to validate</param>
        /// <returns>True if supported, false otherwise</returns>
        public static bool IsSupported(DatabaseType databaseType)
        {
            return databaseType == DatabaseType.SQLite || 
                   databaseType == DatabaseType.SqlServer || 
                   databaseType == DatabaseType.MySQL ||
                   databaseType == DatabaseType.PostgreSQL;
        }

        /// <summary>
        /// Gets the default connection string for a database type
        /// </summary>
        /// <param name="databaseType">Database type</param>
        /// <param name="configuration">Configuration instance for connection strings</param>
        /// <returns>Default connection string</returns>
        public static string GetDefaultConnectionString(DatabaseType databaseType, IConfiguration? configuration = null)
        {
            return GetCreator(databaseType, configuration).GetDefaultConnectionString();
        }
    }
}
