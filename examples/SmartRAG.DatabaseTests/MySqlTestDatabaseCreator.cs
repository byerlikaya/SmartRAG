using MySqlConnector;
using Microsoft.Extensions.Configuration;
using SmartRAG.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRAG.DatabaseTests
{
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
            _password = "2059680";
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

                // 2. Create tables  
                Console.WriteLine("2/3 Creating tables...");
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    CreateTables(connection);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ 3 tables created");
                Console.ResetColor();

                // 3. Insert data
                Console.WriteLine("3/3 Inserting sample data...");
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    InsertSampleData(connection);
                }
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
            var insertDataSql = @"
-- Warehouses
INSERT INTO Warehouses (WarehouseName, Location, Capacity, ManagerName, CreatedDate) VALUES 
    ('Istanbul Central Warehouse', 'Istanbul, Maltepe', 5000, 'Mehmet Yilmaz', '2024-01-15 09:00:00'),
    ('Ankara Distribution Center', 'Ankara, Sincan', 3000, 'Ayse Demir', '2024-02-20 10:30:00'),
    ('Izmir Logistics Hub', 'Izmir, Cigli', 4000, 'Ahmet Kaya', '2024-03-10 11:45:00');

-- Stock (ProductID -> references SQLite Products.ProductID)
INSERT INTO Stock (ProductID, WarehouseID, Quantity, MinimumLevel, MaximumLevel) VALUES 
    -- Istanbul Warehouse
    (1, 1, 45, 10, 100),   -- Laptop
    (2, 1, 78, 15, 150),   -- iPhone
    (3, 1, 32, 10, 80),    -- Samsung
    (4, 1, 156, 30, 300),  -- Headphones
    (5, 1, 234, 50, 500),  -- Jeans
    (6, 1, 189, 40, 400),  -- Dress
    (7, 1, 567, 100, 1000),-- T-shirt
    
    -- Ankara Warehouse
    (1, 2, 23, 5, 50),     -- Laptop
    (2, 2, 34, 8, 100),    -- iPhone
    (8, 2, 12, 3, 20),     -- Sofa Set
    (9, 2, 28, 5, 50),     -- Chandelier
    (10, 2, 15, 5, 30),    -- Carpet
    
    -- Izmir Warehouse
    (3, 3, 56, 10, 100),   -- Samsung
    (4, 3, 123, 25, 250),  -- Headphones
    (11, 3, 89, 20, 150),  -- Novel
    (12, 3, 67, 15, 120),  -- Cooking Book
    (13, 3, 145, 30, 200), -- Science Book
    (14, 3, 234, 50, 400); -- Football

-- Stock Movements (Recent 30 days)
INSERT INTO StockMovements (StockID, MovementType, Quantity, MovementDate, Notes, CreatedBy) VALUES 
    -- Istanbul movements
    (1, 'IN', 50, '2024-10-01 09:00:00', 'New shipment from supplier', 'System'),
    (1, 'OUT', 5, '2024-10-05 14:30:00', 'Sales order fulfillment', 'Warehouse Staff'),
    (2, 'IN', 100, '2024-10-02 10:15:00', 'Bulk order from manufacturer', 'System'),
    (2, 'OUT', 22, '2024-10-08 16:20:00', 'Multiple customer orders', 'Warehouse Staff'),
    (3, 'IN', 40, '2024-10-03 11:30:00', 'Restocking', 'System'),
    (3, 'OUT', 8, '2024-10-09 09:45:00', 'Customer orders', 'Warehouse Staff'),
    (4, 'IN', 200, '2024-10-04 13:00:00', 'Popular item restock', 'System'),
    (4, 'OUT', 44, '2024-10-12 15:30:00', 'High demand sales', 'Warehouse Staff'),
    
    -- Ankara movements
    (8, 'IN', 15, '2024-10-01 08:30:00', 'Furniture delivery', 'System'),
    (8, 'OUT', 3, '2024-10-07 14:00:00', 'Large orders', 'Warehouse Staff'),
    (9, 'IN', 30, '2024-10-02 09:45:00', 'Lighting equipment', 'System'),
    (9, 'OUT', 2, '2024-10-10 11:20:00', 'Customer purchase', 'Warehouse Staff'),
    (10, 'IN', 20, '2024-10-03 10:00:00', 'Home decor items', 'System'),
    (10, 'OUT', 5, '2024-10-14 16:45:00', 'Sales orders', 'Warehouse Staff'),
    
    -- Izmir movements
    (13, 'IN', 150, '2024-10-01 07:30:00', 'Book shipment', 'System'),
    (13, 'OUT', 5, '2024-10-06 13:15:00', 'Online orders', 'Warehouse Staff'),
    (14, 'IN', 250, '2024-10-02 08:45:00', 'Sports equipment', 'System'),
    (14, 'OUT', 16, '2024-10-11 15:00:00', 'Retail sales', 'Warehouse Staff'),
    (15, 'IN', 100, '2024-10-05 09:30:00', 'Book delivery', 'System'),
    (15, 'OUT', 33, '2024-10-13 14:30:00', 'Customer orders', 'Warehouse Staff');";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = insertDataSql;
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
}

