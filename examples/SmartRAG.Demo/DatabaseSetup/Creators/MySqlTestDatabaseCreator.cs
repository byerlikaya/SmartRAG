using MySqlConnector;
using Microsoft.Extensions.Configuration;
using SmartRAG.Demo.DatabaseSetup.Helpers;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// MySQL test database creator implementation
/// Follows SOLID principles - Single Responsibility Principle
/// </summary>
public class MySqlTestDatabaseCreator : ITestDatabaseCreator
    {
        private readonly IConfiguration? _configuration;
        private readonly string _server;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly string _databaseName;

        public MySqlTestDatabaseCreator(IConfiguration? configuration = null)
        {
            _configuration = configuration;
            _server = "localhost";
            _port = 3306;
            _user = "root";
            _password = Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD") ?? "mysql123";
            _databaseName = "InventoryManagement";
        }

        public DatabaseType GetDatabaseType() => DatabaseType.MySQL;

        public string GetDefaultConnectionString()
        {
            return $"Server={_server};Port={_port};Database={_databaseName};User={_user};Password={_password};";
        }

        public string GetDescription()
        {
            return "MySQL test database - Warehouse inventory and stock management (ProductID references SQLite)";
        }

        public bool ValidateConnectionString(string connectionString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                    return false;

                var requiredParts = new[] { "Server=", "Database=" };
                return requiredParts.All(part => connectionString.Contains(part));
            }
            catch
            {
                return false;
            }
        }

        public void CreateSampleDatabase(string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("Creating MySQL Test Database...");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            try
            {
                // 1. Create database
                Console.WriteLine("1/3 Creating database...");
                CreateDatabase();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ InventoryManagement database created");
                Console.ResetColor();

                // Wait for MySQL to complete database creation
                System.Threading.Thread.Sleep(500);

                // 2. Create tables with retry mechanism
                Console.WriteLine("2/3 Creating tables...");
                ExecuteWithRetry(connectionString, CreateTables, 3);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ 3 tables created");
                Console.ResetColor();

                // 3. Insert data with retry mechanism
                Console.WriteLine("3/3 Inserting sample data...");
                ExecuteWithRetry(connectionString, InsertSampleData, 3);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ Sample data inserted");
                Console.ResetColor();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ MySQL test database created successfully!");
                Console.ResetColor();
                
                // Verify
                VerifyDatabase(connectionString);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        private void ExecuteWithRetry(string connectionString, Action<MySqlConnection> action, int maxRetries)
        {
            int retryCount = 0;
            Exception? lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        action(connection);
                        return; // Success, exit
                    }
                }
                catch (MySqlException ex) when (ex.Message.Contains("Lost connection") || 
                                                  ex.Message.Contains("aborted"))
                {
                    lastException = ex;
                    retryCount++;
                    
                    if (retryCount < maxRetries)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"   ⏳ Connection interrupted, retrying ({retryCount}/{maxRetries})...");
                        Console.ResetColor();
                        System.Threading.Thread.Sleep(1000 * retryCount); // Exponential backoff
                    }
                }
                catch (Exception ex)
                {
                    // For other exceptions, don't retry
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    Console.ResetColor();
                    throw;
                }
            }

            // All retries failed
            if (lastException != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Failed after {maxRetries} retries: {lastException.Message}");
                Console.ResetColor();
                throw lastException;
            }
        }

        private void CreateDatabase()
        {
            var masterConnectionString = $"Server={_server};Port={_port};User={_user};Password={_password};";

            using (var connection = new MySqlConnection(masterConnectionString))
            {
                connection.Open();

                // Drop database if exists
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"DROP DATABASE IF EXISTS {_databaseName}";
                    cmd.ExecuteNonQuery();
                }

                // Create database
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"CREATE DATABASE {_databaseName} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CreateTables(MySqlConnection connection)
        {
            var createTablesSql = @"
-- Warehouses Table
CREATE TABLE Warehouses (
    WarehouseID INT PRIMARY KEY AUTO_INCREMENT,
    WarehouseName VARCHAR(100) NOT NULL,
    Location VARCHAR(200) NOT NULL,
    Capacity INT NOT NULL,
    ManagerName VARCHAR(100),
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_location (Location)
);

-- Stock Table (ProductID references SQLite Products.ProductID)
CREATE TABLE Stock (
    StockID INT PRIMARY KEY AUTO_INCREMENT,
    ProductID INT NOT NULL COMMENT 'References ProductCatalog.Products.ProductID',
    WarehouseID INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 0,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    MinimumLevel INT DEFAULT 10,
    MaximumLevel INT DEFAULT 1000,
    FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),
    INDEX idx_product (ProductID),
    INDEX idx_warehouse (WarehouseID)
);

-- Stock Movements Table
CREATE TABLE StockMovements (
    MovementID INT PRIMARY KEY AUTO_INCREMENT,
    StockID INT NOT NULL,
    MovementType VARCHAR(20) NOT NULL COMMENT 'IN or OUT',
    Quantity INT NOT NULL,
    MovementDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    Notes VARCHAR(500),
    CreatedBy VARCHAR(100),
    FOREIGN KEY (StockID) REFERENCES Stock(StockID),
    INDEX idx_stock (StockID),
    INDEX idx_date (MovementDate)
);";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = createTablesSql;
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertSampleData(MySqlConnection connection)
        {
            var random = new Random(42); // Fixed seed for reproducible data
            
            // Generate 15 Warehouses with European names
            var warehousesSql = new StringBuilder("INSERT INTO Warehouses (WarehouseName, Location, Capacity, ManagerName, CreatedDate) VALUES \n");
            
            for (int i = 0; i < 15; i++)
            {
                var city = SampleDataGenerator.GetRandomCity(random);
                var country = SampleDataGenerator.GetRandomCountry(random);
                var warehouseName = $"{city} Distribution Center";
                var location = $"{city}, {country}";
                var capacity = random.Next(2000, 8000);
                var firstName = SampleDataGenerator.GetRandomFirstName(random);
                var lastName = SampleDataGenerator.GetRandomLastName(random);
                var managerName = $"{firstName} {lastName}";
                var year = random.Next(2020, 2025);
                var month = random.Next(1, 13);
                var day = random.Next(1, 29);
                var createdDate = $"{year}-{month:00}-{day:00} 09:00:00";

                warehousesSql.Append($"    ('{warehouseName}', '{location}', {capacity}, '{managerName}', '{createdDate}')");
                warehousesSql.Append(i < 14 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = warehousesSql.ToString();
                cmd.ExecuteNonQuery();
            }

            // Generate 100 Stock entries (ProductID references SQLite Products 1-100)
            var stockSql = new StringBuilder("INSERT INTO Stock (ProductID, WarehouseID, Quantity, MinimumLevel, MaximumLevel) VALUES \n");
            
            for (int i = 0; i < 100; i++)
            {
                // CRITICAL: ProductID must reference actual SQLite Products (1-100)
                // Each product appears once, distributed across warehouses
                var productId = i + 1; // Product 1-100 from SQLite
                var warehouseId = (i % 15) + 1; // Distribute across 15 warehouses
                var quantity = random.Next(10, 500);
                var minimumLevel = random.Next(5, 50);
                var maximumLevel = minimumLevel + random.Next(100, 500);

                stockSql.Append($"    ({productId}, {warehouseId}, {quantity}, {minimumLevel}, {maximumLevel})");
                stockSql.Append(i < 99 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = stockSql.ToString();
                cmd.ExecuteNonQuery();
            }

            // Generate 150 Stock Movements
            var movementsSql = new StringBuilder("INSERT INTO StockMovements (StockID, MovementType, Quantity, MovementDate, Notes, CreatedBy) VALUES \n");
            var movementTypes = new[] { "IN", "OUT", "ADJUSTMENT", "TRANSFER" };
            var createdByUsers = new[] { "System", "Warehouse Staff", "Manager", "Inventory Team" };
            
            for (int i = 0; i < 150; i++)
            {
                var stockId = random.Next(1, 101); // References Stock
                var movementType = movementTypes[random.Next(movementTypes.Length)];
                var quantity = random.Next(1, 100);
                var month = random.Next(1, 11);
                var day = random.Next(1, 29);
                var hour = random.Next(6, 20);
                var minute = random.Next(0, 60);
                var movementDate = $"2025-{month:00}-{day:00} {hour:00}:{minute:00}:00";
                var notes = $"{movementType} movement #{i + 1}";
                var createdBy = createdByUsers[random.Next(createdByUsers.Length)];

                movementsSql.Append($"    ({stockId}, '{movementType}', {quantity}, '{movementDate}', '{notes}', '{createdBy}')");
                movementsSql.Append(i < 149 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = movementsSql.ToString();
                cmd.ExecuteNonQuery();
            }
        }

        private void VerifyDatabase(string connectionString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                Console.WriteLine();
                Console.WriteLine("📊 Database Summary:");
                Console.WriteLine("───────────────────────────────────────");

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            TABLE_NAME as TableName,
                            TABLE_ROWS as TotalRows
                        FROM information_schema.TABLES
                        WHERE TABLE_SCHEMA = @dbName
                        ORDER BY TABLE_NAME";
                    
                    cmd.Parameters.AddWithValue("@dbName", _databaseName);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine($"   • {reader["TableName"]}: {reader["TotalRows"]} rows");
                        }
                    }
                }
            }
        }
}

