using Npgsql;
using Microsoft.Extensions.Configuration;
using SmartRAG.Demo.DatabaseSetup.Helpers;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// PostgreSQL test database creator implementation
/// </summary>
public class PostgreSqlTestDatabaseCreator : ITestDatabaseCreator
    {
        #region Fields

        private readonly IConfiguration? _configuration;
        private readonly string _server;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly string _databaseName;

        #endregion

        #region Constructor

        public PostgreSqlTestDatabaseCreator(IConfiguration? configuration = null)
        {
            _configuration = configuration;
            _user = "postgres";
            
            // Try to get connection details from configuration first
            string? server = null;
            int port = 5432;
            string? password = null;
            string? databaseName = null;
            
            if (_configuration != null)
            {
                var connectionString = _configuration.GetConnectionString("LogisticsManagement") ?? 
                                     _configuration["DatabaseConnections:3:ConnectionString"];
                
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var builder = new NpgsqlConnectionStringBuilder(connectionString);
                    server = builder.Host;
                    port = builder.Port;
                    password = builder.Password;
                    databaseName = builder.Database;
                }
            }
            
            // Fallback to defaults if not found in config
            _server = server ?? "localhost";
            _port = port;
            _databaseName = databaseName ?? "LogisticsManagement";
            
            // Fallback to environment variable for password
            if (string.IsNullOrEmpty(password))
            {
                password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
            }
            
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("PostgreSQL password not found in configuration or environment variables");
            }
            
            _password = password;
        }

        #endregion

        #region Public Methods

        public DatabaseType GetDatabaseType() => DatabaseType.PostgreSQL;

        public string GetDefaultConnectionString()
        {
            return $"Server={_server};Port={_port};Database={_databaseName};User Id={_user};Password={_password};";
        }

        public string GetDescription()
        {
            return "PostgreSQL test database - Logistics operations and route management (references other databases)";
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
            Console.WriteLine("Creating PostgreSQL Test Database...");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            try
            {
                // 1. Create database
                Console.WriteLine("1/3 Creating database...");
                CreateDatabase();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ LogisticsManagement database created");
                Console.ResetColor();

                // Wait for PostgreSQL to complete database creation
                System.Threading.Thread.Sleep(1000);

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
                Console.WriteLine("✅ PostgreSQL test database created successfully!");
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

        #endregion

        #region Private Methods

        private void ExecuteWithRetry(string connectionString, Action<NpgsqlConnection> action, int maxRetries)
        {
            int retryCount = 0;
            Exception? lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        connection.Open();
                        action(connection);
                        return; // Success, exit
                    }
                }
                catch (NpgsqlException ex) when (ex.Message.Contains("terminating connection") || 
                                                   ex.Message.Contains("57P01"))
                {
                    lastException = ex;
                    retryCount++;
                    
                    if (retryCount < maxRetries)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"   ⏳ Connection interrupted, retrying ({retryCount}/{maxRetries})...");
                        Console.ResetColor();
                        System.Threading.Thread.Sleep(2000 * retryCount); // Exponential backoff
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
            var masterConnectionString = $"Server={_server};Port={_port};User Id={_user};Password={_password};Database=postgres;";

            using (var connection = new NpgsqlConnection(masterConnectionString))
            {
                connection.Open();

                // Check if database exists
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{_databaseName}'";
                    var exists = cmd.ExecuteScalar() != null;

                    if (exists)
                    {
                        // Terminate existing connections
                        using (var terminateCmd = connection.CreateCommand())
                        {
                            terminateCmd.CommandText = $@"
                                SELECT pg_terminate_backend(pg_stat_activity.pid)
                                FROM pg_stat_activity
                                WHERE pg_stat_activity.datname = '{_databaseName}'
                                AND pid <> pg_backend_pid();";
                            terminateCmd.ExecuteNonQuery();
                        }

                        // Drop database
                        using (var dropCmd = connection.CreateCommand())
                        {
                            dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
                            dropCmd.ExecuteNonQuery();
                        }
                    }
                }

                // Create database
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\" WITH ENCODING='UTF8'";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CreateTables(NpgsqlConnection connection)
        {
            var createTablesSql = @"
-- Facilities Table (Distribution centers, hubs)
CREATE TABLE Facilities (
    FacilityID SERIAL PRIMARY KEY,
    FacilityName VARCHAR(100) NOT NULL,
    LocationCode VARCHAR(50) NOT NULL,
    Capacity INTEGER NOT NULL,
    OperationalStatus VARCHAR(20) DEFAULT 'Active',
    EstablishedDate TIMESTAMP DEFAULT NOW(),
    ContactInfo TEXT
);

CREATE INDEX idx_facilities_location ON Facilities(LocationCode);
CREATE INDEX idx_facilities_status ON Facilities(OperationalStatus);

-- Shipments Table (References external database items)
CREATE TABLE Shipments (
    ShipmentID SERIAL PRIMARY KEY,
    ReferenceID INTEGER NOT NULL, -- References external database items (ProductID/OrderID/CustomerID)
    FacilityID INTEGER NOT NULL,
    Quantity INTEGER NOT NULL DEFAULT 0,
    ShipmentWeight DECIMAL(10,2),
    ShipmentValue DECIMAL(12,2),
    StatusCode VARCHAR(30) NOT NULL,
    ScheduledDate TIMESTAMP,
    CompletedDate TIMESTAMP,
    Notes TEXT,
    FOREIGN KEY (FacilityID) REFERENCES Facilities(FacilityID)
);

CREATE INDEX idx_shipments_reference ON Shipments(ReferenceID);
CREATE INDEX idx_shipments_facility ON Shipments(FacilityID);
CREATE INDEX idx_shipments_status ON Shipments(StatusCode);
CREATE INDEX idx_shipments_scheduled ON Shipments(ScheduledDate);

-- Routes Table (Delivery routes and assignments)
CREATE TABLE Routes (
    RouteID SERIAL PRIMARY KEY,
    ShipmentID INTEGER NOT NULL,
    OriginFacilityID INTEGER NOT NULL,
    DestinationFacilityID INTEGER NOT NULL,
    Distance DECIMAL(8,2),
    EstimatedDuration INTEGER, -- in minutes
    ActualDuration INTEGER,
    TransportMode VARCHAR(30),
    AssignedDriver VARCHAR(100),
    RouteDate TIMESTAMP DEFAULT NOW(),
    CompletionStatus VARCHAR(20),
    FOREIGN KEY (ShipmentID) REFERENCES Shipments(ShipmentID),
    FOREIGN KEY (OriginFacilityID) REFERENCES Facilities(FacilityID),
    FOREIGN KEY (DestinationFacilityID) REFERENCES Facilities(FacilityID)
);

CREATE INDEX idx_routes_shipment ON Routes(ShipmentID);
CREATE INDEX idx_routes_origin ON Routes(OriginFacilityID);
CREATE INDEX idx_routes_destination ON Routes(DestinationFacilityID);
CREATE INDEX idx_routes_date ON Routes(RouteDate);
CREATE INDEX idx_routes_status ON Routes(CompletionStatus);
";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = createTablesSql;
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertSampleData(NpgsqlConnection connection)
        {
            var random = new Random(42); // Fixed seed for reproducible data
            
            // Generate 20 Facilities
            var facilitiesSql = new StringBuilder("INSERT INTO Facilities (FacilityName, LocationCode, Capacity, OperationalStatus, EstablishedDate, ContactInfo) VALUES \n");
            var facilityTypes = new[] { "Hub", "Distribution Center", "Logistics Center", "Operations Center", "Warehouse" };
            var statuses = new[] { "Active", "Maintenance", "Expanding" };
            
            for (int i = 0; i < 20; i++)
            {
                var city = SampleDataGenerator.GetRandomCity(random);
                var facilityType = facilityTypes[random.Next(facilityTypes.Length)];
                var facilityName = $"{city} {facilityType}";
                var locationCode = $"{city.Substring(0, Math.Min(3, city.Length)).ToUpper()}-{i + 1:000}";
                var capacity = random.Next(5000, 15000);
                var status = statuses[random.Next(statuses.Length)];
                var year = random.Next(2020, 2025);
                var month = random.Next(1, 13);
                var day = random.Next(1, 29);
                var establishedDate = $"{year}-{month:00}-{day:00} 08:00:00";
                var contactInfo = $"Contact: {city.ToLower()}@logistics.com";

                facilitiesSql.Append($"    ('{facilityName}', '{locationCode}', {capacity}, '{status}', '{establishedDate}', '{contactInfo}')");
                facilitiesSql.Append(i < 19 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = facilitiesSql.ToString();
                cmd.ExecuteNonQuery();
            }

            // Generate 100 Shipments (ReferenceID links to external databases)
            var shipmentsSql = new StringBuilder("INSERT INTO Shipments (ReferenceID, FacilityID, Quantity, ShipmentWeight, ShipmentValue, StatusCode, ScheduledDate, CompletedDate, Notes) VALUES \n");
            var shipmentStatuses = new[] { "Completed", "In Transit", "Processing", "Scheduled", "Delivered" };
            
            for (int i = 0; i < 100; i++)
            {
                // CRITICAL: ReferenceID must reference actual records from other databases
                // Mix: Products (SQLite), Orders (SQL Server), Customers (SQLite)
                int referenceId;
                if (i < 50)
                    referenceId = (i % 100) + 1; // Products 1-100 (SQLite)
                else if (i < 80)
                    referenceId = ((i - 50) % 100) + 1; // Orders 1-100 (SQL Server)
                else
                    referenceId = ((i - 80) % 100) + 1; // Customers 1-100 (SQLite)
                var facilityId = random.Next(1, 21);
                var quantity = random.Next(10, 200);
                var weight = Math.Round(random.NextDouble() * 500 + 10, 2);
                var value = Math.Round(random.NextDouble() * 50000 + 500, 2);
                var status = shipmentStatuses[random.Next(shipmentStatuses.Length)];
                var month = random.Next(1, 11);
                var day = random.Next(1, 29);
                var hour = random.Next(6, 20);
                var scheduledDate = $"2025-{month:00}-{day:00} {hour:00}:00:00";
                var completedHour = hour + random.Next(4, 10);
                var completedDay = day;
                if (completedHour >= 24)
                {
                    completedHour -= 24;
                    completedDay++;
                    if (completedDay > 28) completedDay = 28; // Keep within month range
                }
                var completedDate = status == "Completed" || status == "Delivered" 
                    ? $"'2025-{month:00}-{completedDay:00} {completedHour:00}:00:00'" 
                    : "NULL";
                var notes = $"Shipment #{i + 1} - {status}";

                shipmentsSql.Append($"    ({referenceId}, {facilityId}, {quantity}, {weight.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {value.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{status}', '{scheduledDate}', {completedDate}, '{notes}')");
                shipmentsSql.Append(i < 99 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = shipmentsSql.ToString();
                cmd.ExecuteNonQuery();
            }

            // Generate 150 Routes
            var routesSql = new StringBuilder("INSERT INTO Routes (ShipmentID, OriginFacilityID, DestinationFacilityID, Distance, EstimatedDuration, ActualDuration, TransportMode, AssignedDriver, RouteDate, CompletionStatus) VALUES \n");
            var transportModes = new[] { "Truck", "Van", "Heavy Truck", "Express Van", "Container Truck" };
            var routeStatuses = new[] { "Completed", "In Transit", "Scheduled", "Delayed" };
            
            for (int i = 0; i < 150; i++)
            {
                var shipmentId = random.Next(1, 101);
                var originFacilityId = random.Next(1, 21);
                var destinationFacilityId = random.Next(1, 21);
                
                while (destinationFacilityId == originFacilityId)
                {
                    destinationFacilityId = random.Next(1, 21);
                }
                
                var distance = Math.Round(random.NextDouble() * 500 + 50, 2);
                var estimatedDuration = (int)(distance * 0.8 + random.Next(30, 120));
                var routeStatus = routeStatuses[random.Next(routeStatuses.Length)];
                var actualDuration = routeStatus == "Completed" 
                    ? (estimatedDuration + random.Next(-30, 60)).ToString() 
                    : "NULL";
                var transportMode = transportModes[random.Next(transportModes.Length)];
                var firstName = SampleDataGenerator.GetRandomFirstName(random);
                var lastName = SampleDataGenerator.GetRandomLastName(random);
                var assignedDriver = $"{firstName} {lastName}";
                var month = random.Next(1, 11);
                var day = random.Next(1, 29);
                var hour = random.Next(6, 20);
                var routeDate = $"2025-{month:00}-{day:00} {hour:00}:00:00";
                var completionStatus = routeStatus;

                routesSql.Append($"    ({shipmentId}, {originFacilityId}, {destinationFacilityId}, {distance.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {estimatedDuration}, {actualDuration}, '{transportMode}', '{assignedDriver}', '{routeDate}', '{completionStatus}')");
                routesSql.Append(i < 149 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = routesSql.ToString();
                cmd.ExecuteNonQuery();
            }
        }

        private void VerifyDatabase(string connectionString)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                Console.WriteLine();
                Console.WriteLine("📊 Database Summary:");
                Console.WriteLine("───────────────────────────────────────");

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            table_name as TableName,
                            (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = t.table_name) as TotalColumns
                        FROM information_schema.tables t
                        WHERE table_schema = 'public'
                        AND table_type = 'BASE TABLE'
                        ORDER BY table_name";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var tableName = reader["TableName"].ToString();
                            Console.Write($"   • {tableName}: ");
                            
                            // Get row count for each table
                            using (var countConn = new NpgsqlConnection(connectionString))
                            {
                                countConn.Open();
                                using (var countCmd = countConn.CreateCommand())
                                {
                                    countCmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"";
                                    var rowCount = countCmd.ExecuteScalar();
                                    Console.WriteLine($"{rowCount} rows");
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion
}

