using SmartRAG.Enums;

namespace SmartRAG.Services.Database.Strategies;


public class MySqlDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.MySQL;
}

