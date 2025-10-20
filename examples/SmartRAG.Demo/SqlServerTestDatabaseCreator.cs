using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SmartRAG.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRAG.Demo
{
    public class SqlServerTestDatabaseCreator : ITestDatabaseCreator
    {
        private readonly IConfiguration? _configuration;
        private readonly string _server;
        private readonly string _databaseName;

        public SqlServerTestDatabaseCreator(IConfiguration? configuration = null)
        {
            _configuration = configuration;
            _server = "localhost,1433";
            _databaseName = "SalesManagement";
        }

        public DatabaseType GetDatabaseType() => DatabaseType.SqlServer;

        public string GetDefaultConnectionString()
        {
            return $"Server={_server};Database={_databaseName};User Id=sa;Password=SmartRAG@2024;TrustServerCertificate=true;";
        }

        public string GetDescription()
        {
            return "SQL Server test database - Orders, payments and sales data (CustomerID and ProductID reference SQLite)";
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
            Console.WriteLine("Creating SQL Server Test Database...");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            try
            {
                // 1. Create database
                Console.WriteLine("1/3 Creating database...");
                CreateDatabase();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ SalesManagement database created");
                Console.ResetColor();

                // 2. Create tables  
                Console.WriteLine("2/3 Creating tables...");
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    CreateTables(connection);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ 4 tables created");
                Console.ResetColor();

                // 3. Insert data
                Console.WriteLine("3/3 Inserting sample data...");
                using (var connection = new SqlConnection(connectionString))
                {
                connection.Open();
                    InsertSampleData(connection);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ Sample data inserted");
                Console.ResetColor();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ SQL Server test database created successfully!");
                Console.ResetColor();
                
                // Verify
                VerifyDatabase(connectionString);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Hata: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        private void CreateDatabase()
        {
            var masterConnectionString = $"Server={_server};Database=master;User Id=sa;Password=SmartRAG@2024;TrustServerCertificate=true;";

            using (var connection = new SqlConnection(masterConnectionString))
            {
                connection.Open();

                // Drop database if exists
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $@"
                        IF EXISTS (SELECT name FROM sys.databases WHERE name = '{_databaseName}')
                BEGIN
                            ALTER DATABASE {_databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                            DROP DATABASE {_databaseName};
                        END";
                    cmd.ExecuteNonQuery();
                }

                // Create database
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"CREATE DATABASE {_databaseName}";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CreateTables(SqlConnection connection)
        {
            var createTablesSql = @"
-- Orders Table (CustomerID references SQLite Musteriler.MusteriID)
                CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
                    CustomerID INT NOT NULL,
    OrderDate DATETIME DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    OrderStatus NVARCHAR(50) DEFAULT N'Pending',
    TrackingNumber NVARCHAR(100)
);

-- Order Details (ProductID references SQLite Urunler.UrunID)
                CREATE TABLE OrderDetails (
    OrderDetailID INT PRIMARY KEY IDENTITY(1,1),
                    OrderID INT NOT NULL,
                    ProductID INT NOT NULL,
                    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountRate DECIMAL(5,2) DEFAULT 0,
    Subtotal AS (Quantity * UnitPrice * (1 - DiscountRate/100)),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

-- Payments
CREATE TABLE Payments (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL,
    PaymentDate DATETIME DEFAULT GETDATE(),
    PaymentMethod NVARCHAR(50) NOT NULL,
    PaymentAmount DECIMAL(18,2) NOT NULL,
    PaymentStatus NVARCHAR(50) DEFAULT N'Completed',
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

-- Sales Summary (Daily sales reports)
CREATE TABLE SalesSummary (
    SummaryID INT PRIMARY KEY IDENTITY(1,1),
    SalesDate DATE NOT NULL,
    TotalSales DECIMAL(18,2) NOT NULL,
    TotalOrders INT NOT NULL,
    AverageOrderAmount DECIMAL(18,2)
);";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = createTablesSql;
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertSampleData(SqlConnection connection)
        {
            var insertDataSql = @"
-- Orders (CustomerID -> references SQLite Musteriler.MusteriID)
INSERT INTO Orders (CustomerID, OrderDate, TotalAmount, OrderStatus, TrackingNumber) VALUES 
    (1, '2024-10-01 14:30:00', 48500.00, N'Delivered', N'TRK-2024-1001'),
    (2, '2024-10-02 10:15:00', 850.00, N'Delivered', N'TRK-2024-1002'),
    (3, '2024-10-03 16:45:00', 42000.00, N'Shipped', N'TRK-2024-1003'),
    (4, '2024-10-04 11:20:00', 11600.00, N'Delivered', N'TRK-2024-1004'),
    (5, '2024-10-05 09:00:00', 65000.00, N'Processing', NULL),
    (1, '2024-10-06 15:30:00', 3500.00, N'Delivered', N'TRK-2024-1006'),
    (6, '2024-10-07 13:45:00', 25000.00, N'Shipped', N'TRK-2024-1007'),
    (7, '2024-10-08 17:00:00', 1224.00, N'Delivered', N'TRK-2024-1008'),
    (8, '2024-10-09 12:30:00', 4500.00, N'Delivered', N'TRK-2024-1009'),
    (9, '2024-10-10 10:00:00', 900.00, N'Pending', NULL),
    (10, '2024-10-11 14:15:00', 45000.00, N'Processing', NULL),
    (2, '2024-10-12 11:30:00', 250.00, N'Delivered', N'TRK-2024-1012'),
    (3, '2024-10-13 16:00:00', 450.00, N'Delivered', N'TRK-2024-1013'),
    (4, '2024-10-14 09:45:00', 8000.00, N'Shipped', N'TRK-2024-1014'),
    (1, '2024-10-15 10:30:00', 170500.00, N'Delivered', N'TRK-2024-1015');

-- Order Details (ProductID -> references SQLite Urunler.UrunID)
INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice, DiscountRate) VALUES 
    (1, 1, 1, 45000.00, 0),     -- Customer 1: Laptop
    (1, 4, 1, 3500.00, 0),      -- Customer 1: Headphones
    (2, 6, 1, 850.00, 0),       -- Customer 2: Dress
    (3, 3, 1, 42000.00, 0),     -- Customer 3: Samsung
    (4, 10, 1, 8000.00, 5),     -- Customer 4: Carpet (5% discount)
    (4, 9, 1, 4500.00, 20),     -- Customer 4: Chandelier (20% discount)
    (5, 2, 1, 65000.00, 0),     -- Customer 5: iPhone
    (6, 4, 1, 3500.00, 0),      -- Customer 1: Headphones (2nd order)
    (7, 8, 1, 25000.00, 0),     -- Customer 6: Sofa Set
    (8, 5, 2, 450.00, 0),       -- Customer 7: Jeans x2
    (8, 7, 3, 120.00, 10),      -- Customer 7: T-shirt x3
    (9, 9, 1, 4500.00, 0),      -- Customer 8: Chandelier
    (10, 14, 2, 450.00, 0),     -- Customer 9: Football x2
    (11, 1, 1, 45000.00, 0),    -- Customer 10: Laptop
    (12, 13, 1, 250.00, 0),     -- Customer 2: Book (2nd order)
    (13, 14, 1, 450.00, 0),     -- Customer 3: Football (2nd order)
    (14, 10, 1, 8000.00, 0),    -- Customer 4: Carpet (2nd order)
    (15, 2, 2, 65000.00, 0),    -- Customer 1: iPhone x2 (Biggest order!)
    (15, 1, 1, 45000.00, 10),   -- Customer 1: Laptop (10% discount)
    (15, 4, 2, 3500.00, 5);     -- Customer 1: Headphones x2 (5% discount)

-- Payments
INSERT INTO Payments (OrderID, PaymentDate, PaymentMethod, PaymentAmount, PaymentStatus) VALUES 
    (1, '2024-10-01 14:30:00', N'Credit Card', 48500.00, N'Completed'),
    (2, '2024-10-02 10:15:00', N'Cash', 850.00, N'Completed'),
    (3, '2024-10-03 16:45:00', N'Bank Transfer', 42000.00, N'Completed'),
    (4, '2024-10-04 11:20:00', N'Credit Card', 11600.00, N'Completed'),
    (5, '2024-10-05 09:00:00', N'Credit Card', 65000.00, N'Pending'),
    (6, '2024-10-06 15:30:00', N'Credit Card', 3500.00, N'Completed'),
    (7, '2024-10-07 13:45:00', N'Bank Transfer', 25000.00, N'Completed'),
    (8, '2024-10-08 17:00:00', N'Cash', 1224.00, N'Completed'),
    (9, '2024-10-09 12:30:00', N'Credit Card', 4500.00, N'Completed'),
    (10, '2024-10-10 10:00:00', N'Bank Transfer', 900.00, N'Pending'),
    (11, '2024-10-11 14:15:00', N'Credit Card', 45000.00, N'Pending'),
    (12, '2024-10-12 11:30:00', N'Cash', 250.00, N'Completed'),
    (13, '2024-10-13 16:00:00', N'Credit Card', 450.00, N'Completed'),
    (14, '2024-10-14 09:45:00', N'Bank Transfer', 8000.00, N'Completed'),
    (15, '2024-10-15 10:30:00', N'Credit Card', 170500.00, N'Completed');

-- Sales Summary
INSERT INTO SalesSummary (SalesDate, TotalSales, TotalOrders, AverageOrderAmount) VALUES 
    ('2024-10-01', 48500.00, 1, 48500.00),
    ('2024-10-02', 850.00, 1, 850.00),
    ('2024-10-03', 42000.00, 1, 42000.00),
    ('2024-10-04', 11600.00, 1, 11600.00),
    ('2024-10-05', 65000.00, 1, 65000.00),
    ('2024-10-06', 3500.00, 1, 3500.00),
    ('2024-10-07', 25000.00, 1, 25000.00),
    ('2024-10-08', 1224.00, 1, 1224.00),
    ('2024-10-09', 4500.00, 1, 4500.00),
    ('2024-10-10', 900.00, 1, 900.00),
    ('2024-10-11', 45000.00, 1, 45000.00),
    ('2024-10-12', 250.00, 1, 250.00),
    ('2024-10-13', 450.00, 1, 450.00),
    ('2024-10-14', 8000.00, 1, 8000.00),
    ('2024-10-15', 170500.00, 1, 170500.00);";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = insertDataSql;
                cmd.ExecuteNonQuery();
            }
        }

        private void VerifyDatabase(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                Console.WriteLine();
                Console.WriteLine("📊 Database Summary:");
                Console.WriteLine("───────────────────────────────────────");

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            t.name as TableName,
                            SUM(p.rows) as TotalRows
                        FROM sys.tables t
                        INNER JOIN sys.partitions p ON t.object_id = p.object_id
                        WHERE p.index_id IN (0,1)
                        GROUP BY t.name
                        ORDER BY t.name";

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
