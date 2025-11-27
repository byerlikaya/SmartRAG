using MySqlConnector;
using Microsoft.Extensions.Configuration;
using SmartRAG.Demo.DatabaseSetup.Helpers;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// MySQL test database creator implementation
/// Domain: Warehouse & Inventory Management
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
            
            // Try to get connection details from configuration first
            string? server = null;
            int port = 3306;
            string? user = null;
            string? password = null;
            string? databaseName = null;
            
            if (_configuration != null)
            {
                var connectionString = _configuration.GetConnectionString("InventoryManagement") ?? 
                                     _configuration["DatabaseConnections:2:ConnectionString"];
                
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var builder = new MySqlConnectionStringBuilder(connectionString);
                    server = builder.Server;
                    port = (int)builder.Port;
                    user = builder.UserID;
                    password = builder.Password;
                    databaseName = builder.Database;
                }
            }
            
            // Fallback to defaults if not found in config
            _server = server ?? "localhost";
            _port = port;
            _user = user ?? "root";
            _databaseName = databaseName ?? "InventoryManagement";
            
            // Fallback to environment variable for password
            if (string.IsNullOrEmpty(password))
            {
                password = Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD");
            }
            
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("MySQL password not found in configuration or environment variables");
            }
            
            _password = password;
        }

        public DatabaseType GetDatabaseType() => DatabaseType.MySQL;

        public string GetDefaultConnectionString()
        {
            return $"Server={_server};Port={_port};Database={_databaseName};User={_user};Password={_password};";
        }

        public string GetDescription()
        {
            return "MySQL - Warehouse & Inventory Management (ProductID and SupplierID reference SQLite)";
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
                Console.WriteLine($"   ✓ {_databaseName} database created");
                Console.ResetColor();

                // Wait for MySQL to complete database creation
                System.Threading.Thread.Sleep(500);

                // 2. Create tables with retry mechanism
                Console.WriteLine("2/3 Creating tables...");
                ExecuteWithRetry(connectionString, CreateTables, 3);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ 7 tables created");
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
                Console.WriteLine($"❌ Hata: {ex.Message}");
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
-- Warehouses Table (Enhanced)
CREATE TABLE Warehouses (
    WarehouseID INT PRIMARY KEY AUTO_INCREMENT,
    WarehouseName VARCHAR(100) NOT NULL,
    Location VARCHAR(200) NOT NULL,
    Capacity INT NOT NULL,
    ManagerName VARCHAR(100),
    ManagerEmail VARCHAR(100),
    ManagerPhone VARCHAR(50),
    WarehouseType VARCHAR(50),
    IsActive TINYINT DEFAULT 1,
    EstablishedDate DATE,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_location (Location),
    INDEX idx_active (IsActive)
);

-- Stock Table (ProductID and SupplierID reference SQLite)
CREATE TABLE Stock (
    StockID INT PRIMARY KEY AUTO_INCREMENT,
    ProductID INT NOT NULL COMMENT 'References SQLite Products.ProductID',
    SupplierID INT COMMENT 'References SQLite Suppliers.SupplierID',
    WarehouseID INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 0,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    MinimumLevel INT DEFAULT 10,
    MaximumLevel INT DEFAULT 1000,
    ReorderPoint INT DEFAULT 20,
    FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),
    INDEX idx_product (ProductID),
    INDEX idx_supplier (SupplierID),
    INDEX idx_warehouse (WarehouseID)
);

-- Stock Movements Table
CREATE TABLE StockMovements (
    MovementID INT PRIMARY KEY AUTO_INCREMENT,
    StockID INT NOT NULL,
    MovementType VARCHAR(20) NOT NULL COMMENT 'IN, OUT, TRANSFER, ADJUSTMENT',
    Quantity INT NOT NULL,
    MovementDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ReferenceNumber VARCHAR(100),
    Notes VARCHAR(500),
    CreatedBy VARCHAR(100),
    FOREIGN KEY (StockID) REFERENCES Stock(StockID),
    INDEX idx_stock (StockID),
    INDEX idx_date (MovementDate),
    INDEX idx_type (MovementType)
);

-- WarehouseZones Table (NEW)
CREATE TABLE WarehouseZones (
    ZoneID INT PRIMARY KEY AUTO_INCREMENT,
    WarehouseID INT NOT NULL,
    ZoneName VARCHAR(100) NOT NULL,
    ZoneType VARCHAR(50) COMMENT 'Storage, Refrigerated, Hazardous, High-Value',
    Capacity INT NOT NULL,
    CurrentUtilization DECIMAL(5,2) DEFAULT 0.00 COMMENT 'Percentage 0-100',
    Temperature DECIMAL(5,2) COMMENT 'Temperature in Celsius',
    IsClimateControlled TINYINT DEFAULT 0,
    FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),
    INDEX idx_warehouse (WarehouseID),
    INDEX idx_type (ZoneType)
);

-- StockAlerts Table (NEW)
CREATE TABLE StockAlerts (
    AlertID INT PRIMARY KEY AUTO_INCREMENT,
    ProductID INT NOT NULL COMMENT 'References SQLite Products.ProductID',
    WarehouseID INT NOT NULL,
    AlertType VARCHAR(50) NOT NULL COMMENT 'Low Stock, Overstock, Expired, Damaged',
    Threshold INT,
    CurrentLevel INT,
    AlertDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    IsResolved TINYINT DEFAULT 0,
    ResolvedDate DATETIME,
    Notes TEXT,
    FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),
    INDEX idx_product (ProductID),
    INDEX idx_warehouse (WarehouseID),
    INDEX idx_resolved (IsResolved)
);

-- InventoryAudits Table (NEW)
CREATE TABLE InventoryAudits (
    AuditID INT PRIMARY KEY AUTO_INCREMENT,
    WarehouseID INT NOT NULL,
    ProductID INT NOT NULL COMMENT 'References SQLite Products.ProductID',
    AuditDate DATE NOT NULL,
    ExpectedQty INT NOT NULL,
    ActualQty INT NOT NULL,
    Variance INT AS (ActualQty - ExpectedQty) STORED,
    VarianceReason VARCHAR(200),
    AuditorName VARCHAR(100),
    Status VARCHAR(50) DEFAULT 'Completed',
    FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),
    INDEX idx_warehouse (WarehouseID),
    INDEX idx_product (ProductID),
    INDEX idx_date (AuditDate)
);

-- StockReservations Table (NEW)
CREATE TABLE StockReservations (
    ReservationID INT PRIMARY KEY AUTO_INCREMENT,
    ProductID INT NOT NULL COMMENT 'References SQLite Products.ProductID',
    WarehouseID INT NOT NULL,
    OrderID INT NOT NULL COMMENT 'References SQL Server Orders.OrderID',
    ReservedQty INT NOT NULL,
    ReservationDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ExpiryDate DATETIME,
    Status VARCHAR(50) DEFAULT 'Active',
    FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),
    INDEX idx_product (ProductID),
    INDEX idx_warehouse (WarehouseID),
    INDEX idx_order (OrderID),
    INDEX idx_status (Status)
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
            
            // Generate 35 Warehouses
            var warehousesSql = new StringBuilder("INSERT INTO Warehouses (WarehouseName, Location, Capacity, ManagerName, ManagerEmail, ManagerPhone, WarehouseType, IsActive, EstablishedDate, CreatedDate) VALUES \n");
            var warehouseTypes = new[] { "Distribution Center", "Fulfillment Center", "Regional Hub", "Cold Storage", "Hazmat Facility" };
            
            for (int i = 0; i < 35; i++)
            {
                var city = SampleDataGenerator.GetRandomCity(random);
                var country = SampleDataGenerator.GetRandomCountry(random);
                var warehouseName = $"{city} {warehouseTypes[random.Next(warehouseTypes.Length)]}";
                var location = $"{city}, {country}";
                var capacity = random.Next(2000, 8000);
                var firstName = SampleDataGenerator.GetRandomFirstName(random);
                var lastName = SampleDataGenerator.GetRandomLastName(random);
                var managerName = $"{firstName} {lastName}";
                var managerEmail = SampleDataGenerator.GenerateEmail(firstName, lastName, random);
                var managerPhone = SampleDataGenerator.GeneratePhone(random);
                var warehouseType = warehouseTypes[random.Next(warehouseTypes.Length)];
                var isActive = random.NextDouble() > 0.1 ? 1 : 0;
                var year = random.Next(2020, 2025);
                var month = random.Next(1, 13);
                var day = random.Next(1, 29);
                var establishedDate = $"{year}-{month:00}-{day:00}";
                var createdDate = $"{year}-{month:00}-{day:00} 09:00:00";

                warehousesSql.Append($"    ('{warehouseName}', '{location}', {capacity}, '{managerName}', '{managerEmail}', '{managerPhone}', '{warehouseType}', {isActive}, '{establishedDate}', '{createdDate}')");
                warehousesSql.Append(i < 34 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = warehousesSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ Warehouses: 35 rows inserted");

            // Generate 500 Stock entries (ProductID 1-250 from SQLite, SupplierID 1-30 from SQLite)
            var stockSql = new StringBuilder("INSERT INTO Stock (ProductID, SupplierID, WarehouseID, Quantity, MinimumLevel, MaximumLevel, ReorderPoint) VALUES \n");
            
            for (int i = 0; i < 500; i++)
            {
                // CRITICAL: ProductID must reference actual SQLite Products (1-250)
                var productId = (i % 250) + 1;
                // CRITICAL: SupplierID must reference actual SQLite Suppliers (1-30)
                var supplierId = (i % 30) + 1;
                var warehouseId = (i % 35) + 1;
                var quantity = random.Next(10, 500);
                var minimumLevel = random.Next(5, 50);
                var maximumLevel = minimumLevel + random.Next(100, 500);
                var reorderPoint = minimumLevel + (int)((maximumLevel - minimumLevel) * 0.2);

                stockSql.Append($"    ({productId}, {supplierId}, {warehouseId}, {quantity}, {minimumLevel}, {maximumLevel}, {reorderPoint})");
                stockSql.Append(i < 499 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = stockSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ Stock: 500 rows inserted");

            // Generate 800 Stock Movements
            var movementsSql = new StringBuilder("INSERT INTO StockMovements (StockID, MovementType, Quantity, MovementDate, ReferenceNumber, Notes, CreatedBy) VALUES \n");
            var movementTypes = new[] { "IN", "OUT", "ADJUSTMENT", "TRANSFER" };
            var createdByUsers = new[] { "System", "Warehouse Staff", "Manager", "Inventory Team", "Auto-Reorder System" };
            
            for (int i = 0; i < 800; i++)
            {
                var stockId = random.Next(1, 501);
                var movementType = movementTypes[random.Next(movementTypes.Length)];
                var quantity = random.Next(1, 100);
                var month = random.Next(1, 11);
                var day = random.Next(1, 29);
                var hour = random.Next(6, 20);
                var minute = random.Next(0, 60);
                var movementDate = $"2025-{month:00}-{day:00} {hour:00}:{minute:00}:00";
                var refNumber = $"REF-{random.Next(10000, 99999)}";
                var notes = $"{movementType} movement #{i + 1}";
                var createdBy = createdByUsers[random.Next(createdByUsers.Length)];

                movementsSql.Append($"    ({stockId}, '{movementType}', {quantity}, '{movementDate}', '{refNumber}', '{notes}', '{createdBy}')");
                movementsSql.Append(i < 799 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = movementsSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ StockMovements: 800 rows inserted");

            // Generate 140 WarehouseZones
            var zonesSql = new StringBuilder("INSERT INTO WarehouseZones (WarehouseID, ZoneName, ZoneType, Capacity, CurrentUtilization, Temperature, IsClimateControlled) VALUES \n");
            var zoneTypes = new[] { "Storage", "Refrigerated", "Hazardous", "High-Value", "Bulk Storage" };
            
            for (int i = 0; i < 140; i++)
            {
                var warehouseId = (i % 35) + 1;
                var zoneLetter = (char)('A' + (i % 26));
                var zoneName = $"Zone {zoneLetter}-{(i / 26) + 1}";
                var zoneType = zoneTypes[random.Next(zoneTypes.Length)];
                var capacity = random.Next(100, 1000);
                var utilization = Math.Round(random.NextDouble() * 100, 2);
                var temperature = zoneType == "Refrigerated" ? Math.Round(random.NextDouble() * 10 - 5, 2) : (double?)null;
                var isClimateControlled = zoneType == "Refrigerated" || zoneType == "High-Value" ? 1 : 0;
                var tempValue = temperature.HasValue ? temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "NULL";

                zonesSql.Append($"    ({warehouseId}, '{zoneName}', '{zoneType}', {capacity}, {utilization.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {tempValue}, {isClimateControlled})");
                zonesSql.Append(i < 139 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = zonesSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ WarehouseZones: 140 rows inserted");

            // Generate 150 StockAlerts
            var alertsSql = new StringBuilder("INSERT INTO StockAlerts (ProductID, WarehouseID, AlertType, Threshold, CurrentLevel, AlertDate, IsResolved, ResolvedDate, Notes) VALUES \n");
            var alertTypes = new[] { "Low Stock", "Overstock", "Expired", "Damaged", "Missing" };
            
            for (int i = 0; i < 150; i++)
            {
                var productId = random.Next(1, 251);
                var warehouseId = random.Next(1, 36);
                var alertType = alertTypes[random.Next(alertTypes.Length)];
                var threshold = random.Next(10, 100);
                var currentLevel = alertType == "Low Stock" ? random.Next(0, threshold) : random.Next(threshold, threshold * 3);
                var month = random.Next(1, 11);
                var day = random.Next(1, 29);
                var alertDate = $"2025-{month:00}-{day:00} 10:00:00";
                var isResolved = random.NextDouble() > 0.4 ? 1 : 0;
                var resolvedDate = isResolved == 1 ? $"'2025-{month:00}-{Math.Min(day + random.Next(1, 5), 28):00} 15:00:00'" : "NULL";
                var notes = $"Alert for {alertType} - Product {productId}";

                alertsSql.Append($"    ({productId}, {warehouseId}, '{alertType}', {threshold}, {currentLevel}, '{alertDate}', {isResolved}, {resolvedDate}, '{notes}')");
                alertsSql.Append(i < 149 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = alertsSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ StockAlerts: 150 rows inserted");

            // Generate 200 InventoryAudits
            var auditsSql = new StringBuilder("INSERT INTO InventoryAudits (WarehouseID, ProductID, AuditDate, ExpectedQty, ActualQty, VarianceReason, AuditorName, Status) VALUES \n");
            var varianceReasons = new[] { "Theft", "Damage", "Counting Error", "System Error", "Spoilage", "Returns" };
            var auditorNames = new[] { "John Auditor", "Jane Inspector", "Mike Counter", "Sarah Checker", "Tom Analyst" };
            var auditStatuses = new[] { "Completed", "In Progress", "Pending Review" };
            
            for (int i = 0; i < 200; i++)
            {
                var warehouseId = random.Next(1, 36);
                var productId = random.Next(1, 251);
                var month = random.Next(1, 11);
                var day = random.Next(1, 29);
                var auditDate = $"2025-{month:00}-{day:00}";
                var expectedQty = random.Next(50, 500);
                var actualQty = expectedQty + random.Next(-50, 51);
                var variance = actualQty - expectedQty;
                var varianceReason = variance != 0 ? varianceReasons[random.Next(varianceReasons.Length)] : "No Variance";
                var auditorName = auditorNames[random.Next(auditorNames.Length)];
                var status = auditStatuses[random.Next(auditStatuses.Length)];

                auditsSql.Append($"    ({warehouseId}, {productId}, '{auditDate}', {expectedQty}, {actualQty}, '{varianceReason}', '{auditorName}', '{status}')");
                auditsSql.Append(i < 199 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = auditsSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ InventoryAudits: 200 rows inserted");

            // Generate 250 StockReservations
            var reservationsSql = new StringBuilder("INSERT INTO StockReservations (ProductID, WarehouseID, OrderID, ReservedQty, ReservationDate, ExpiryDate, Status) VALUES \n");
            var reservationStatuses = new[] { "Active", "Expired", "Fulfilled", "Cancelled" };
            
            for (int i = 0; i < 250; i++)
            {
                var productId = random.Next(1, 251);
                var warehouseId = random.Next(1, 36);
                // CRITICAL: OrderID must reference actual SQL Server Orders (1-300)
                var orderId = random.Next(1, 301);
                var reservedQty = random.Next(1, 20);
                var month = random.Next(1, 11);
                var day = random.Next(1, 29);
                var hour = random.Next(9, 18);
                var reservationDate = $"2025-{month:00}-{day:00} {hour:00}:00:00";
                var expiryDay = Math.Min(day + random.Next(3, 8), 28);
                var expiryDate = $"2025-{month:00}-{expiryDay:00} 23:59:59";
                var status = reservationStatuses[random.Next(reservationStatuses.Length)];

                reservationsSql.Append($"    ({productId}, {warehouseId}, {orderId}, {reservedQty}, '{reservationDate}', '{expiryDate}', '{status}')");
                reservationsSql.Append(i < 249 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = reservationsSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ StockReservations: 250 rows inserted");
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

