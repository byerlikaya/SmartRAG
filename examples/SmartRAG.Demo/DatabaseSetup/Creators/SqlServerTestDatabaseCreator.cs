using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SmartRAG.Demo.DatabaseSetup.Helpers;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

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
            var random = new Random(42); // Fixed seed for reproducible data
            
            // Generate 100 Orders (CustomerID references SQLite Customers 1-100)
            var ordersSql = new StringBuilder();
            ordersSql.AppendLine("INSERT INTO Orders (CustomerID, OrderDate, TotalAmount, OrderStatus, TrackingNumber) VALUES ");
            
            var statuses = new[] { "Delivered", "Shipped", "Processing", "Pending", "Cancelled" };
            for (int i = 0; i < 100; i++)
            {
                // CRITICAL: CustomerID must reference actual SQLite Customers (1-100)
                var customerId = (i % 100) + 1; // Distributes orders across all 100 customers
                var orderMonth = random.Next(1, 11);
                var orderDay = random.Next(1, 29);
                var orderHour = random.Next(9, 18);
                var orderMinute = random.Next(0, 60);
                var orderDate = $"2025-{orderMonth:00}-{orderDay:00} {orderHour:00}:{orderMinute:00}:00";
                var totalAmount = Math.Round(random.NextDouble() * 50000 + 100, 2);
                var status = statuses[random.Next(statuses.Length)];
                var trackingNumber = status != "Pending" && status != "Processing" 
                    ? $"N'TRK-2025-{1000 + i}'" 
                    : "NULL";

                ordersSql.Append($"    ({customerId}, '{orderDate}', {totalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, N'{status}', {trackingNumber})");
                ordersSql.AppendLine(i < 99 ? "," : ";");
            }

            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = ordersSql.ToString();
                    cmd.ExecuteNonQuery();
                }
                System.Console.WriteLine("   ✓ Orders: 100 rows inserted");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"   ❌ Orders INSERT failed: {ex.Message}");
                throw;
            }

            // Generate 150 Order Details (ProductID references SQLite Products 1-100)
            var orderDetailsSql = new StringBuilder();
            orderDetailsSql.AppendLine("INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice, DiscountRate) VALUES ");
            
            for (int i = 0; i < 150; i++)
            {
                var orderId = (i % 100) + 1; // Reference to Orders 1-100 (some orders have multiple items)
                // CRITICAL: ProductID must reference actual SQLite Products (1-100)
                var productId = random.Next(1, 101); // Random product from SQLite catalog
                var quantity = random.Next(1, 6);
                var unitPrice = Math.Round(random.NextDouble() * 5000 + 50, 2);
                var discountRate = random.Next(0, 21); // 0-20% discount

                orderDetailsSql.Append($"    ({orderId}, {productId}, {quantity}, {unitPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {discountRate})");
                orderDetailsSql.AppendLine(i < 149 ? "," : ";");
            }

            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = orderDetailsSql.ToString();
                    cmd.ExecuteNonQuery();
                }
                System.Console.WriteLine("   ✓ OrderDetails: 150 rows inserted");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"   ❌ OrderDetails INSERT failed: {ex.Message}");
                throw;
            }

            // Generate 100 Payments
            var paymentsSql = new StringBuilder();
            paymentsSql.AppendLine("INSERT INTO Payments (OrderID, PaymentDate, PaymentMethod, PaymentAmount, PaymentStatus) VALUES ");
            
            var paymentMethods = new[] { "Credit Card", "Debit Card", "Bank Transfer", "Cash", "PayPal" };
            var paymentStatuses = new[] { "Completed", "Pending", "Failed", "Refunded" };
            
            for (int i = 0; i < 100; i++)
            {
                var orderId = i + 1; // One payment per order
                var paymentMonth = random.Next(1, 11);
                var paymentDay = random.Next(1, 29);
                var paymentHour = random.Next(9, 18);
                var paymentMinute = random.Next(0, 60);
                var paymentDate = $"2025-{paymentMonth:00}-{paymentDay:00} {paymentHour:00}:{paymentMinute:00}:00";
                var paymentMethod = paymentMethods[random.Next(paymentMethods.Length)];
                var paymentAmount = Math.Round(random.NextDouble() * 50000 + 100, 2);
                var paymentStatus = paymentStatuses[random.Next(paymentStatuses.Length)];

                paymentsSql.Append($"    ({orderId}, '{paymentDate}', N'{paymentMethod}', {paymentAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, N'{paymentStatus}')");
                paymentsSql.AppendLine(i < 99 ? "," : ";");
            }

            try
            {
            using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = paymentsSql.ToString();
                    cmd.ExecuteNonQuery();
                }
                System.Console.WriteLine("   ✓ Payments: 100 rows inserted");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"   ❌ Payments INSERT failed: {ex.Message}");
                throw;
            }

            // Generate 100 Sales Summary records
            var summaryRecords = 100;
            var salesSummarySql = new StringBuilder();
            salesSummarySql.AppendLine("INSERT INTO SalesSummary (SalesDate, TotalSales, TotalOrders, AverageOrderAmount) VALUES ");
            
            for (int i = 0; i < summaryRecords; i++)
            {
                var salesMonth = random.Next(1, 11);
                var salesDay = random.Next(1, 29);
                var salesDate = $"2025-{salesMonth:00}-{salesDay:00}";
                var totalSales = Math.Round(random.NextDouble() * 100000 + 1000, 2);
                var totalOrders = random.Next(1, 20);
                var averageOrderAmount = Math.Round(totalSales / totalOrders, 2);

                salesSummarySql.Append($"    ('{salesDate}', {totalSales.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {totalOrders}, {averageOrderAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
                salesSummarySql.AppendLine(i < summaryRecords - 1 ? "," : ";");
            }

            try
            {
            using (var cmd = connection.CreateCommand())
            {
                    cmd.CommandText = salesSummarySql.ToString();
                cmd.ExecuteNonQuery();
                }
                System.Console.WriteLine("   ✓ SalesSummary: 100 rows inserted");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"   ❌ SalesSummary INSERT failed: {ex.Message}");
                throw;
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
