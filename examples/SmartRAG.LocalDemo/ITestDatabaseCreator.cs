using SmartRAG.Enums;

namespace SmartRAG.LocalDemo
{
    /// <summary>
    /// Interface for creating test databases across different database types
    /// Follows SOLID principles - Single Responsibility and Dependency Inversion
    /// </summary>
    public interface ITestDatabaseCreator
    {
        /// <summary>
        /// Creates a sample database with test data
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        void CreateSampleDatabase(string connectionString);

        /// <summary>
        /// Gets the default connection string for this database type
        /// </summary>
        /// <returns>Default connection string</returns>
        string GetDefaultConnectionString();

        /// <summary>
        /// Gets the database type this creator supports
        /// </summary>
        /// <returns>Database type enum</returns>
        DatabaseType GetDatabaseType();

        /// <summary>
        /// Validates if the connection string is valid for this database type
        /// </summary>
        /// <param name="connectionString">Connection string to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateConnectionString(string connectionString);

        /// <summary>
        /// Gets a human-readable description of this database type
        /// </summary>
        /// <returns>Database type description</returns>
        string GetDescription();
    }
}
