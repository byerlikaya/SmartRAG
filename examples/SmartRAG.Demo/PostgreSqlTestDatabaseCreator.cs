using Npgsql;
using Microsoft.Extensions.Configuration;
using SmartRAG.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRAG.Demo
{
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
            _server = "localhost";
            _port = 5432;
            _user = "postgres";
            _password = "postgres123";
            _databaseName = "LogisticsManagement";
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

                // 2. Create tables  
                Console.WriteLine("2/3 Creating tables...");
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    CreateTables(connection);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ 3 tables created");
                Console.ResetColor();

                // 3. Insert data
                Console.WriteLine("3/3 Inserting sample data...");
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    InsertSampleData(connection);
                }
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
            var insertDataSql = @"
-- Facilities (Distribution Centers)
INSERT INTO Facilities (FacilityName, LocationCode, Capacity, OperationalStatus, EstablishedDate, ContactInfo) VALUES 
    ('North Regional Hub', 'NRH-001', 10000, 'Active', '2023-01-15 08:00:00', 'Contact: north@logistics.com'),
    ('South Distribution Center', 'SDC-002', 8000, 'Active', '2023-02-20 09:30:00', 'Contact: south@logistics.com'),
    ('East Logistics Hub', 'ELH-003', 12000, 'Active', '2023-03-10 10:00:00', 'Contact: east@logistics.com'),
    ('West Operations Center', 'WOC-004', 7500, 'Active', '2023-04-05 11:15:00', 'Contact: west@logistics.com');

-- Shipments (ReferenceID links to external databases - ProductID, OrderID, CustomerID)
INSERT INTO Shipments (ReferenceID, FacilityID, Quantity, ShipmentWeight, ShipmentValue, StatusCode, ScheduledDate, CompletedDate, Notes) VALUES 
    -- ReferenceID 1-15 could be ProductIDs, OrderIDs, or CustomerIDs from other databases
    (1, 1, 25, 125.50, 2500.00, 'Completed', '2024-10-01 09:00:00', '2024-10-01 15:30:00', 'Standard delivery'),
    (2, 1, 50, 85.30, 4250.00, 'Completed', '2024-10-02 10:15:00', '2024-10-02 16:45:00', 'Express shipment'),
    (3, 2, 15, 230.75, 3450.00, 'Completed', '2024-10-03 08:30:00', '2024-10-03 14:20:00', 'Fragile items'),
    (4, 2, 100, 45.20, 4520.00, 'In Transit', '2024-10-15 07:00:00', NULL, 'Bulk order'),
    (5, 3, 35, 156.80, 5488.00, 'Completed', '2024-10-05 11:00:00', '2024-10-05 17:30:00', 'Multiple items'),
    (6, 3, 20, 98.40, 1968.00, 'Processing', '2024-10-16 09:30:00', NULL, 'Priority delivery'),
    (7, 1, 75, 320.15, 24011.25, 'Completed', '2024-10-07 06:45:00', '2024-10-07 18:15:00', 'Large shipment'),
    (8, 4, 10, 450.00, 4500.00, 'Scheduled', '2024-10-18 08:00:00', NULL, 'Heavy cargo'),
    (9, 4, 45, 78.90, 3550.50, 'Completed', '2024-10-09 10:30:00', '2024-10-09 16:00:00', 'Regular delivery'),
    (10, 2, 30, 120.60, 3618.00, 'Completed', '2024-10-10 09:00:00', '2024-10-10 15:45:00', 'Standard shipment'),
    (11, 1, 60, 210.45, 12627.00, 'In Transit', '2024-10-17 07:30:00', NULL, 'Multi-destination'),
    (12, 3, 25, 95.30, 2382.50, 'Completed', '2024-10-12 08:15:00', '2024-10-12 14:30:00', 'Express delivery'),
    (13, 4, 80, 340.20, 27216.00, 'Completed', '2024-10-13 06:00:00', '2024-10-13 19:45:00', 'Consolidated shipment'),
    (14, 2, 55, 165.75, 9116.25, 'Processing', '2024-10-18 10:00:00', NULL, 'Priority order'),
    (15, 1, 40, 188.50, 7540.00, 'Completed', '2024-10-14 09:30:00', '2024-10-14 16:15:00', 'Standard delivery');

-- Routes (Delivery routes between facilities)
INSERT INTO Routes (ShipmentID, OriginFacilityID, DestinationFacilityID, Distance, EstimatedDuration, ActualDuration, TransportMode, AssignedDriver, RouteDate, CompletionStatus) VALUES 
    -- Completed routes
    (1, 1, 2, 245.50, 240, 255, 'Truck', 'Driver A', '2024-10-01 09:30:00', 'Completed'),
    (2, 1, 3, 312.30, 300, 310, 'Truck', 'Driver B', '2024-10-02 10:30:00', 'Completed'),
    (3, 2, 1, 245.50, 240, 250, 'Van', 'Driver C', '2024-10-03 09:00:00', 'Completed'),
    (5, 3, 4, 178.20, 180, 175, 'Truck', 'Driver D', '2024-10-05 11:30:00', 'Completed'),
    (7, 1, 4, 420.80, 420, 435, 'Heavy Truck', 'Driver E', '2024-10-07 07:00:00', 'Completed'),
    (9, 4, 2, 289.40, 290, 285, 'Truck', 'Driver F', '2024-10-09 11:00:00', 'Completed'),
    (10, 2, 3, 198.75, 200, 195, 'Van', 'Driver G', '2024-10-10 09:30:00', 'Completed'),
    (12, 3, 1, 312.30, 300, 315, 'Truck', 'Driver H', '2024-10-12 08:45:00', 'Completed'),
    (13, 4, 3, 356.90, 360, 370, 'Heavy Truck', 'Driver I', '2024-10-13 06:30:00', 'Completed'),
    (15, 1, 2, 245.50, 240, 245, 'Truck', 'Driver J', '2024-10-14 10:00:00', 'Completed'),
    
    -- In transit routes
    (4, 2, 4, 334.60, 330, NULL, 'Truck', 'Driver K', '2024-10-15 07:30:00', 'In Transit'),
    (11, 1, 3, 312.30, 300, NULL, 'Truck', 'Driver L', '2024-10-17 08:00:00', 'In Transit'),
    (11, 3, 4, 178.20, 180, NULL, 'Van', 'Driver M', '2024-10-17 14:30:00', 'Scheduled'),
    
    -- Scheduled routes
    (6, 3, 2, 198.75, 200, NULL, 'Van', 'Driver N', '2024-10-16 10:00:00', 'Scheduled'),
    (8, 4, 1, 420.80, 420, NULL, 'Heavy Truck', 'Driver O', '2024-10-18 08:30:00', 'Scheduled'),
    (14, 2, 1, 245.50, 240, NULL, 'Truck', 'Driver P', '2024-10-18 10:30:00', 'Scheduled');
";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = insertDataSql;
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
}

