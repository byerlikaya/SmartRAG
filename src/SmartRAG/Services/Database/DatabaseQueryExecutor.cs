
namespace SmartRAG.Services.Database;


/// <summary>
/// Executes queries across multiple databases
/// </summary>
public class DatabaseQueryExecutor : IDatabaseQueryExecutor
{
    private const int DefaultMaxRows = 100;

    private readonly IDatabaseConnectionManager _connectionManager;
    private readonly IDatabaseParserService _databaseParser;
    private readonly ILogger<DatabaseQueryExecutor> _logger;

    public DatabaseQueryExecutor(
        IDatabaseConnectionManager connectionManager,
        IDatabaseParserService databaseParser,
        ILogger<DatabaseQueryExecutor> logger)
    {
        _connectionManager = connectionManager;
        _databaseParser = databaseParser;
        _logger = logger;
    }

    /// <summary>
    /// [DB Query] Executes queries across multiple databases based on query intent
    /// </summary>
    public async Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MultiDatabaseQueryResult
        {
            Success = true
        };

        var maxResultsOverride = queryIntent.MaxResults > 0 ? queryIntent.MaxResults : (int?)null;

        var tasks = queryIntent.DatabaseQueries.Select(async dbQuery =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dbResult = await ExecuteSingleDatabaseQueryAsync(dbQuery, maxResultsOverride, cancellationToken);
            return (dbQuery.DatabaseId, dbResult);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (databaseId, dbResult) in results)
        {
            result.DatabaseResults[databaseId] = dbResult;

            if (!dbResult.Success)
            {
                result.Success = false;
                result.Errors.Add($"Database {databaseId}: {dbResult.ErrorMessage}");
            }
        }

        stopwatch.Stop();
        result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        return result;
    }

    private async Task<DatabaseQueryResult> ExecuteSingleDatabaseQueryAsync(DatabaseQueryIntent dbQuery, int? maxResultsOverride, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new DatabaseQueryResult
        {
            DatabaseId = dbQuery.DatabaseId,
            DatabaseName = dbQuery.DatabaseName,
            ExecutedQuery = dbQuery.GeneratedQuery
        };

        try
        {
            var connection = await _connectionManager.GetConnectionAsync(dbQuery.DatabaseId, cancellationToken);
            if (connection == null)
            {
                result.Success = false;
                result.ErrorMessage = "Database connection not found";
                return result;
            }

            if (string.IsNullOrEmpty(dbQuery.GeneratedQuery))
            {
                result.Success = false;
                result.ErrorMessage = "No query generated";
                return result;
            }

            _logger.LogDebug("Executing SQL for database {DatabaseName}", dbQuery.DatabaseName);

            var configMaxRows = connection.MaxRowsPerQuery > 0 ? connection.MaxRowsPerQuery : DefaultMaxRows;
            var maxRows = maxResultsOverride.HasValue ? Math.Min(maxResultsOverride.Value, configMaxRows) : configMaxRows;
            var queryResult = await _databaseParser.ExecuteQueryAsync(
                connection.ConnectionString,
                dbQuery.GeneratedQuery,
                connection.DatabaseType,
                maxRows,
                cancellationToken);

            result.ResultData = queryResult;
            result.RowCount = CountRowsInResult(queryResult);
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query on database {DatabaseName}", dbQuery.DatabaseName);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    private int CountRowsInResult(string resultData)
    {
        if (string.IsNullOrEmpty(resultData))
        {
            return 0;
        }

        var lines = resultData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.StartsWith("Rows extracted:"))
                continue;
            if (int.TryParse(line["Rows extracted:".Length..].Trim(), out var count))
            {
                return count;
            }
        }

        var dataRows = 0;
        var headerFound = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("===") || line.StartsWith("Query:") || line.StartsWith("Rows"))
                continue;

            if (!headerFound)
            {
                headerFound = true;
                continue;
            }

            dataRows++;
        }

        return dataRows;
    }
}


