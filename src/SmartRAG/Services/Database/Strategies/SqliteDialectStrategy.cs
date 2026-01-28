using SmartRAG.Enums;

namespace SmartRAG.Services.Database.Strategies
{
    public class SqliteDialectStrategy : BaseSqlDialectStrategy
    {
        public override DatabaseType DatabaseType => DatabaseType.SQLite;
    }
}
