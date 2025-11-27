using Npgsql;
using Microsoft.Extensions.Configuration;
using SmartRAG.Demo.DatabaseSetup.Helpers;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// PostgreSQL test database creator implementation
/// Domain: Logistics & Distribution
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
            
            // Try to get connection details from configuration first
            string? server = null;
            int port = 5432;
            string? user = null;
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
                    user = builder.Username;
                    password = builder.Password;
                    databaseName = builder.Database;
                }
            }
            
            // Fallback to defaults if not found in config
            _server = server ?? "localhost";
            _port = port;
            _user = user ?? "postgres";
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
            return "PostgreSQL - Logistics & Distribution (OrderID, ProductID, CustomerID, WarehouseID reference other databases)";
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
                // Clear any existing connection pools to ensure no locks are held
                NpgsqlConnection.ClearAllPools();

                // 1. Create database
                Console.WriteLine("1/3 Creating database...");
                CreateDatabase();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"   ✓ {_databaseName} database created");
                Console.ResetColor();

                // Wait for PostgreSQL to complete database creation
                System.Threading.Thread.Sleep(1000);

                // 2. Create tables with retry mechanism
                Console.WriteLine("2/3 Creating tables...");
                ExecuteWithRetry(connectionString, CreateTables, 3);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   ✓ 8 tables created");
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
                Console.WriteLine($"❌ Hata: {ex.Message}");
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
    Address TEXT,
    City VARCHAR(100),
    Country VARCHAR(100),
    Capacity INTEGER NOT NULL,
    OperationalStatus VARCHAR(20) DEFAULT 'Active',
    EstablishedDate TIMESTAMP DEFAULT NOW(),
    ContactInfo TEXT
);

CREATE INDEX idx_facilities_location ON Facilities(LocationCode);
CREATE INDEX idx_facilities_status ON Facilities(OperationalStatus);
CREATE INDEX idx_facilities_city ON Facilities(City);

-- Shipments Table (References SQL Server Orders, SQLite Customers, MySQL Warehouses)
CREATE TABLE Shipments (
    ShipmentID SERIAL PRIMARY KEY,
    OrderID INTEGER NOT NULL, -- References SQL Server Orders.OrderID
    CustomerID INTEGER NOT NULL, -- References SQLite Customers.CustomerID
    OriginWarehouseID INTEGER, -- References MySQL Warehouses.WarehouseID
    DestinationFacilityID INTEGER,
    DestinationAddress TEXT NOT NULL,
    ShipmentWeight DECIMAL(10,2),
    ShipmentValue DECIMAL(12,2),
    StatusCode VARCHAR(30) NOT NULL,
    CarrierID INTEGER, -- References Carriers.CarrierID
    ScheduledDate TIMESTAMP,
    ShippedDate TIMESTAMP,
    DeliveredDate TIMESTAMP,
    TrackingNumber VARCHAR(100) UNIQUE,
    Notes TEXT,
    FOREIGN KEY (DestinationFacilityID) REFERENCES Facilities(FacilityID)
);

CREATE INDEX idx_shipments_order ON Shipments(OrderID);
CREATE INDEX idx_shipments_customer ON Shipments(CustomerID);
CREATE INDEX idx_shipments_warehouse ON Shipments(OriginWarehouseID);
CREATE INDEX idx_shipments_facility ON Shipments(DestinationFacilityID);
CREATE INDEX idx_shipments_status ON Shipments(StatusCode);
CREATE INDEX idx_shipments_scheduled ON Shipments(ScheduledDate);

-- ShipmentItems Table (NEW - Individual items in shipments)
CREATE TABLE ShipmentItems (
    ShipmentItemID SERIAL PRIMARY KEY,
    ShipmentID INTEGER NOT NULL,
    ProductID INTEGER NOT NULL, -- References SQLite Products.ProductID
    Quantity INTEGER NOT NULL DEFAULT 0,
    PackageWeight DECIMAL(10,2),
    PackageDimensions VARCHAR(50),
    HandlingInstructions TEXT,
    FOREIGN KEY (ShipmentID) REFERENCES Shipments(ShipmentID)
);

CREATE INDEX idx_shipmentitems_shipment ON ShipmentItems(ShipmentID);
CREATE INDEX idx_shipmentitems_product ON ShipmentItems(ProductID);

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
    AssignedDriverID INTEGER,
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
CREATE INDEX idx_routes_driver ON Routes(AssignedDriverID);

-- Drivers Table (NEW)
CREATE TABLE Drivers (
    DriverID SERIAL PRIMARY KEY,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    LicenseNumber VARCHAR(50) UNIQUE NOT NULL,
    Phone VARCHAR(50),
    Email VARCHAR(100),
    HireDate DATE DEFAULT CURRENT_DATE,
    VehicleType VARCHAR(50),
    Status VARCHAR(20) DEFAULT 'Active',
    Rating DECIMAL(3,2) DEFAULT 5.00
);

CREATE INDEX idx_drivers_status ON Drivers(Status);
CREATE INDEX idx_drivers_license ON Drivers(LicenseNumber);

-- DeliveryEvents Table (NEW - Real-time shipment tracking)
CREATE TABLE DeliveryEvents (
    EventID SERIAL PRIMARY KEY,
    ShipmentID INTEGER NOT NULL,
    EventType VARCHAR(50) NOT NULL, -- 'Picked Up', 'In Transit', 'Out for Delivery', 'Delivered', 'Exception'
    EventDate DATE NOT NULL,
    EventTime TIME NOT NULL,
    Location TEXT,
    Latitude DECIMAL(10,7),
    Longitude DECIMAL(10,7),
    Notes TEXT,
    RecordedBy VARCHAR(100),
    FOREIGN KEY (ShipmentID) REFERENCES Shipments(ShipmentID)
);

CREATE INDEX idx_deliveryevents_shipment ON DeliveryEvents(ShipmentID);
CREATE INDEX idx_deliveryevents_type ON DeliveryEvents(EventType);
CREATE INDEX idx_deliveryevents_date ON DeliveryEvents(EventDate);

-- Carriers Table (NEW - Third-party carrier information)
CREATE TABLE Carriers (
    CarrierID SERIAL PRIMARY KEY,
    CarrierName VARCHAR(100) NOT NULL,
    ServiceType VARCHAR(50), -- 'Air', 'Ground', 'Sea', 'Rail'
    ContactEmail VARCHAR(100),
    ContactPhone VARCHAR(50),
    TrackingURLTemplate TEXT,
    IsActive BOOLEAN DEFAULT TRUE,
    Rating DECIMAL(3,2) DEFAULT 5.00
);

CREATE INDEX idx_carriers_active ON Carriers(IsActive);

-- VehicleFleet Table (NEW)
CREATE TABLE VehicleFleet (
    VehicleID SERIAL PRIMARY KEY,
    VehiclePlate VARCHAR(20) UNIQUE NOT NULL,
    VehicleType VARCHAR(50) NOT NULL, -- 'Van', 'Truck', 'Semi-Truck', 'Motorcycle'
    Brand VARCHAR(50),
    Model VARCHAR(50),
    Year INTEGER,
    Capacity INTEGER, -- in kg
    FuelType VARCHAR(30),
    Status VARCHAR(20) DEFAULT 'Active',
    LastMaintenanceDate DATE,
    NextMaintenanceDate DATE
);

CREATE INDEX idx_vehiclefleet_status ON VehicleFleet(Status);
CREATE INDEX idx_vehiclefleet_type ON VehicleFleet(VehicleType);
CREATE INDEX idx_vehiclefleet_plate ON VehicleFleet(VehiclePlate);
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
            
            // Generate 40 Facilities
            var facilitiesSql = new StringBuilder("INSERT INTO Facilities (FacilityName, LocationCode, Address, City, Country, Capacity, OperationalStatus, EstablishedDate, ContactInfo) VALUES \n");
            var facilityTypes = new[] { "Hub", "Distribution Center", "Logistics Center", "Operations Center", "Fulfillment Center" };
            var statuses = new[] { "Active", "Maintenance", "Expanding", "Under Construction" };
            
            for (int i = 0; i < 40; i++)
            {
                var city = SampleDataGenerator.GetRandomCity(random);
                var country = SampleDataGenerator.GetRandomCountry(random);
                var facilityType = facilityTypes[random.Next(facilityTypes.Length)];
                var facilityName = $"{city} {facilityType}";
                var locationCode = $"{city.Substring(0, Math.Min(3, city.Length)).ToUpper()}-{i + 1:000}";
                var address = SampleDataGenerator.GenerateAddress(random);
                var capacity = random.Next(5000, 15000);
                var status = statuses[random.Next(statuses.Length)];
                var year = random.Next(2020, 2025);
                var month = random.Next(1, 13);
                var day = random.Next(1, 29);
                var establishedDate = $"{year}-{month:00}-{day:00} 08:00:00";
                var contactInfo = $"Contact: {city.ToLower()}@logistics.com, Phone: {SampleDataGenerator.GeneratePhone(random)}";

                facilitiesSql.Append($"    ('{facilityName}', '{locationCode}', '{address}', '{city}', '{country}', {capacity}, '{status}', '{establishedDate}', '{contactInfo}')");
                facilitiesSql.Append(i < 39 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = facilitiesSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ Facilities: 40 rows inserted");

            // Generate 350 Shipments
            var shipmentsSql = new StringBuilder("INSERT INTO Shipments (OrderID, CustomerID, OriginWarehouseID, DestinationFacilityID, DestinationAddress, ShipmentWeight, ShipmentValue, StatusCode, CarrierID, ScheduledDate, ShippedDate, DeliveredDate, TrackingNumber, Notes) VALUES \n");
            var shipmentStatuses = new[] { "Completed", "In Transit", "Processing", "Scheduled", "Delivered", "Out for Delivery", "Exception" };
            
            for (int i = 0; i < 350; i++)
            {
                // CRITICAL: OrderID references SQL Server Orders (1-300)
                var orderId = (i % 300) + 1;
                // CRITICAL: CustomerID references SQLite Customers (1-150)
                var customerId = (i % 150) + 1;
                // CRITICAL: OriginWarehouseID references MySQL Warehouses (1-35)
                var originWarehouseId = (i % 35) + 1;
                var destinationFacilityId = random.Next(1, 41);
                var city = SampleDataGenerator.GetRandomCity(random);
                var address = SampleDataGenerator.GenerateAddress(random);
                var destinationAddress = $"{address}, {city}";
                var weight = Math.Round(random.NextDouble() * 500 + 10, 2);
                var value = Math.Round(random.NextDouble() * 50000 + 500, 2);
                var status = shipmentStatuses[random.Next(shipmentStatuses.Length)];
                // IMPORTANT: CarrierID references Carriers (1-20)
                var carrierId = random.Next(1, 21);
                var month = random.Next(1, 11);
                var day = random.Next(1, 29);
                var hour = random.Next(6, 20);
                var scheduledDate = $"2025-{month:00}-{day:00} {hour:00}:00:00";
                
                var shippedDate = status != "Scheduled" && status != "Processing" 
                    ? $"'2025-{month:00}-{Math.Min(day + 1, 28):00} {hour:00}:00:00'" 
                    : "NULL";
                
                var deliveredDate = status == "Completed" || status == "Delivered" 
                    ? $"'2025-{month:00}-{Math.Min(day + random.Next(2, 5), 28):00} {Math.Min(hour + random.Next(1, 8), 23):00}:00:00'" 
                    : "NULL";
                
                var trackingNumber = $"TRK-LOG-{10000 + i}";
                var notes = $"Shipment #{i + 1} - {status}";

                shipmentsSql.Append($"    ({orderId}, {customerId}, {originWarehouseId}, {destinationFacilityId}, '{destinationAddress}', {weight.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {value.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{status}', {carrierId}, '{scheduledDate}', {shippedDate}, {deliveredDate}, '{trackingNumber}', '{notes}')");
                shipmentsSql.Append(i < 349 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = shipmentsSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ Shipments: 350 rows inserted");

            // Generate 700 ShipmentItems
            var shipmentItemsSql = new StringBuilder("INSERT INTO ShipmentItems (ShipmentID, ProductID, Quantity, PackageWeight, PackageDimensions, HandlingInstructions) VALUES \n");
            var handlingInstructions = new[] { "Handle with care", "Fragile", "Keep upright", "Temperature sensitive", "Standard handling", "Heavy item" };
            
            for (int i = 0; i < 700; i++)
            {
                var shipmentId = (i % 350) + 1;
                // CRITICAL: ProductID references SQLite Products (1-250)
                var productId = random.Next(1, 251);
                var quantity = random.Next(1, 10);
                var packageWeight = Math.Round(random.NextDouble() * 50 + 1, 2);
                var length = random.Next(10, 100);
                var width = random.Next(10, 100);
                var height = random.Next(10, 100);
                var packageDimensions = $"{length}x{width}x{height} cm";
                var handling = handlingInstructions[random.Next(handlingInstructions.Length)];

                shipmentItemsSql.Append($"    ({shipmentId}, {productId}, {quantity}, {packageWeight.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{packageDimensions}', '{handling}')");
                shipmentItemsSql.Append(i < 699 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = shipmentItemsSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ ShipmentItems: 700 rows inserted");

            // Generate 100 Drivers
            var driversSql = new StringBuilder("INSERT INTO Drivers (FirstName, LastName, LicenseNumber, Phone, Email, HireDate, VehicleType, Status, Rating) VALUES \n");
            var vehicleTypes = new[] { "Van", "Truck", "Semi-Truck", "Motorcycle", "Car" };
            var driverStatuses = new[] { "Active", "On Leave", "Training", "Inactive" };
            
            for (int i = 0; i < 100; i++)
            {
                var firstName = SampleDataGenerator.GetRandomFirstName(random);
                var lastName = SampleDataGenerator.GetRandomLastName(random);
                var licenseNumber = $"DL-{random.Next(100000, 999999)}";
                var phone = SampleDataGenerator.GeneratePhone(random);
                var email = SampleDataGenerator.GenerateEmail(firstName, lastName, random);
                var year = random.Next(2018, 2025);
                var month = random.Next(1, 13);
                var day = random.Next(1, 29);
                var hireDate = $"{year}-{month:00}-{day:00}";
                var vehicleType = vehicleTypes[random.Next(vehicleTypes.Length)];
                var status = driverStatuses[random.Next(driverStatuses.Length)];
                var rating = Math.Round(random.NextDouble() * 2 + 3, 2); // 3.0-5.0

                driversSql.Append($"    ('{firstName}', '{lastName}', '{licenseNumber}', '{phone}', '{email}', '{hireDate}', '{vehicleType}', '{status}', {rating.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
                driversSql.Append(i < 99 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = driversSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ Drivers: 100 rows inserted");

            // Generate 400 Routes
            var routesSql = new StringBuilder("INSERT INTO Routes (ShipmentID, OriginFacilityID, DestinationFacilityID, Distance, EstimatedDuration, ActualDuration, TransportMode, AssignedDriverID, RouteDate, CompletionStatus) VALUES \n");
            var transportModes = new[] { "Truck", "Van", "Heavy Truck", "Express Van", "Container Truck", "Air Freight" };
            var routeStatuses = new[] { "Completed", "In Transit", "Scheduled", "Delayed", "Cancelled" };
            
            for (int i = 0; i < 400; i++)
            {
                var shipmentId = (i % 350) + 1;
                var originFacilityId = random.Next(1, 41);
                var destinationFacilityId = random.Next(1, 41);
                
                while (destinationFacilityId == originFacilityId)
                {
                    destinationFacilityId = random.Next(1, 41);
                }
                
                var distance = Math.Round(random.NextDouble() * 500 + 50, 2);
                var estimatedDuration = (int)(distance * 0.8 + random.Next(30, 120));
                var routeStatus = routeStatuses[random.Next(routeStatuses.Length)];
                var actualDuration = routeStatus == "Completed" 
                    ? (estimatedDuration + random.Next(-30, 60)).ToString() 
                    : "NULL";
                var transportMode = transportModes[random.Next(transportModes.Length)];
                var driverId = random.Next(1, 101);
                var month = random.Next(1, 11);
                var day = random.Next(1, 29);
                var hour = random.Next(6, 20);
                var routeDate = $"2025-{month:00}-{day:00} {hour:00}:00:00";

                routesSql.Append($"    ({shipmentId}, {originFacilityId}, {destinationFacilityId}, {distance.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {estimatedDuration}, {actualDuration}, '{transportMode}', {driverId}, '{routeDate}', '{routeStatus}')");
                routesSql.Append(i < 399 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = routesSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ Routes: 400 rows inserted");

            // Generate 1200 DeliveryEvents
            var eventsSql = new StringBuilder("INSERT INTO DeliveryEvents (ShipmentID, EventType, EventDate, EventTime, Location, Latitude, Longitude, Notes, RecordedBy) VALUES \n");
            var eventTypes = new[] { "Picked Up", "In Transit", "Arrived at Hub", "Out for Delivery", "Delivered", "Exception", "Delayed" };
            var recordedByUsers = new[] { "System", "Driver", "Warehouse Staff", "Customer Service", "Operations Center" };
            
            for (int i = 0; i < 1200; i++)
            {
                var shipmentId = (i % 350) + 1;
                var eventType = eventTypes[random.Next(eventTypes.Length)];
                var month = random.Next(1, 11);
                var day = random.Next(1, 29);
                var eventDate = $"2025-{month:00}-{day:00}";
                var hour = random.Next(0, 24);
                var minute = random.Next(0, 60);
                var eventTime = $"{hour:00}:{minute:00}:00";
                var location = $"{SampleDataGenerator.GetRandomCity(random)}, {SampleDataGenerator.GetRandomCountry(random)}";
                var latitude = Math.Round((random.NextDouble() * 60) + 20, 7); // 20-80 degrees
                var longitude = Math.Round((random.NextDouble() * 60) - 30, 7); // -30 to 30 degrees
                var notes = $"{eventType} event for shipment #{shipmentId}";
                var recordedBy = recordedByUsers[random.Next(recordedByUsers.Length)];

                eventsSql.Append($"    ({shipmentId}, '{eventType}', '{eventDate}', '{eventTime}', '{location}', {latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{notes}', '{recordedBy}')");
                eventsSql.Append(i < 1199 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = eventsSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ DeliveryEvents: 1200 rows inserted");

            // Generate 20 Carriers
            var carriersSql = new StringBuilder("INSERT INTO Carriers (CarrierName, ServiceType, ContactEmail, ContactPhone, TrackingURLTemplate, IsActive, Rating) VALUES \n");
            var serviceTypes = new[] { "Air", "Ground", "Sea", "Rail", "Express" };
            var carrierNames = new[] { "FastShip Express", "Global Logistics", "Prime Carriers", "Swift Transport", "EuroFreight", 
                                      "TransWorld Shipping", "QuickDeliver", "Reliable Cargo", "Premium Express", "Economy Freight" };
            
            for (int i = 0; i < 20; i++)
            {
                var carrierName = i < carrierNames.Length 
                    ? carrierNames[i] 
                    : $"Carrier #{i + 1}";
                var serviceType = serviceTypes[random.Next(serviceTypes.Length)];
                var email = $"contact@{carrierName.ToLower().Replace(" ", "")}.com";
                var phone = SampleDataGenerator.GeneratePhone(random);
                var trackingURL = $"https://track.{carrierName.ToLower().Replace(" ", "")}.com/{{trackingNumber}}";
                var isActive = random.NextDouble() > 0.2 ? "TRUE" : "FALSE";
                var rating = Math.Round(random.NextDouble() * 2 + 3, 2);

                carriersSql.Append($"    ('{carrierName}', '{serviceType}', '{email}', '{phone}', '{trackingURL}', {isActive}, {rating.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
                carriersSql.Append(i < 19 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = carriersSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ Carriers: 20 rows inserted");

            // Generate 120 VehicleFleet
            var vehiclesSql = new StringBuilder("INSERT INTO VehicleFleet (VehiclePlate, VehicleType, Brand, Model, Year, Capacity, FuelType, Status, LastMaintenanceDate, NextMaintenanceDate) VALUES \n");
            var vehicleFleetTypes = new[] { "Van", "Truck", "Semi-Truck", "Motorcycle" };
            var brands = new[] { "Mercedes", "Ford", "Volvo", "MAN", "Scania", "Iveco", "DAF" };
            var fuelTypes = new[] { "Diesel", "Gasoline", "Electric", "Hybrid", "CNG" };
            var vehicleStatuses = new[] { "Active", "Maintenance", "Out of Service", "Reserved" };
            
            for (int i = 0; i < 120; i++)
            {
                var plate = $"{(char)('A' + random.Next(26))}{(char)('A' + random.Next(26))}-{random.Next(100, 999)}-{(char)('A' + random.Next(26))}{(char)('A' + random.Next(26))}";
                var vehicleType = vehicleFleetTypes[random.Next(vehicleFleetTypes.Length)];
                var brand = brands[random.Next(brands.Length)];
                var model = $"{brand} Model {random.Next(100, 999)}";
                var year = random.Next(2015, 2025);
                var capacity = vehicleType == "Semi-Truck" ? random.Next(10000, 25000) 
                             : vehicleType == "Truck" ? random.Next(3000, 10000)
                             : random.Next(500, 3000);
                var fuelType = fuelTypes[random.Next(fuelTypes.Length)];
                var status = vehicleStatuses[random.Next(vehicleStatuses.Length)];
                var lastMaintenanceMonth = random.Next(1, 11);
                var lastMaintenanceDay = random.Next(1, 29);
                var lastMaintenanceDate = $"2025-{lastMaintenanceMonth:00}-{lastMaintenanceDay:00}";
                var nextMaintenanceMonth = Math.Min(lastMaintenanceMonth + random.Next(1, 4), 12);
                var nextMaintenanceDay = random.Next(1, 29);
                var nextMaintenanceDate = $"2025-{nextMaintenanceMonth:00}-{nextMaintenanceDay:00}";

                vehiclesSql.Append($"    ('{plate}', '{vehicleType}', '{brand}', '{model}', {year}, {capacity}, '{fuelType}', '{status}', '{lastMaintenanceDate}', '{nextMaintenanceDate}')");
                vehiclesSql.Append(i < 119 ? ",\n" : ";\n");
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = vehiclesSql.ToString();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("   ✓ VehicleFleet: 120 rows inserted");
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

