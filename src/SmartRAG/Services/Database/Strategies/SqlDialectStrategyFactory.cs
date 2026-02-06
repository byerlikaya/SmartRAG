using SmartRAG.Enums;
using SmartRAG.Interfaces.Database.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRAG.Services.Database.Strategies;


public interface ISqlDialectStrategyFactory
{
    ISqlDialectStrategy GetStrategy(DatabaseType databaseType);
}

public class SqlDialectStrategyFactory : ISqlDialectStrategyFactory
{
    private readonly IEnumerable<ISqlDialectStrategy> _strategies;

    public SqlDialectStrategyFactory(IEnumerable<ISqlDialectStrategy> strategies)
    {
        _strategies = strategies;
    }

    public ISqlDialectStrategy GetStrategy(DatabaseType databaseType)
    {
        var strategy = _strategies.FirstOrDefault(s => s.DatabaseType == databaseType) ?? throw new NotSupportedException($"No SQL dialect strategy found for database type: {databaseType}");
        return strategy;
    }
}

