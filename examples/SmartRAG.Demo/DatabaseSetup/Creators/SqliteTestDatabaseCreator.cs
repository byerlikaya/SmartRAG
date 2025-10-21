using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using SmartRAG.Demo.DatabaseSetup.Helpers;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// SQLite test database creator implementation
/// Follows SOLID principles - Single Responsibility Principle
/// </summary>
public class SqliteTestDatabaseCreator : ITestDatabaseCreator
    {
        private readonly IConfiguration? _configuration;

        public SqliteTestDatabaseCreator(IConfiguration? configuration = null)
        {
            _configuration = configuration;
        }

        public DatabaseType GetDatabaseType() => DatabaseType.SQLite;

        public string GetDescription() => "SQLite - Embedded, file-based database (No installation required)";

        public string GetDefaultConnectionString()
        {
            if (_configuration != null)
            {
                // First check SQLite specific path
                var dbPath = _configuration["DatabaseTests:Sqlite:DatabasePath"];
                
                // If SQLite specific path not found, use DefaultDatabasePath
                if (string.IsNullOrEmpty(dbPath))
                {
                    dbPath = _configuration["DatabaseTests:DefaultDatabasePath"];
                }

                if (!string.IsNullOrEmpty(dbPath))
                {
                    // If relative path, add to current directory
                    if (!Path.IsPathRooted(dbPath))
                    {
                        var currentDir = Directory.GetCurrentDirectory();
                        dbPath = Path.Combine(currentDir, dbPath);
                    }

                    // Create connection string
                    var connectionString = $"Data Source={dbPath}";
                    
                    // Add SQLite specific settings
                    var enableForeignKeys = _configuration["DatabaseTests:Sqlite:EnableForeignKeys"];
                    var connectionTimeout = _configuration["DatabaseTests:Sqlite:ConnectionTimeout"];
                    
                    if (!string.IsNullOrEmpty(enableForeignKeys) && bool.Parse(enableForeignKeys))
                    {
                        connectionString += ";Foreign Keys=True";
                    }
                    
                    if (!string.IsNullOrEmpty(connectionTimeout))
                    {
                        connectionString += $";Connection Timeout={connectionTimeout}";
                    }
                    
                    return connectionString;
                }
            }

            // Fallback: Default path
            var fallbackPath = Path.Combine(Directory.GetCurrentDirectory(), "test_company.db");
            return $"Data Source={fallbackPath};Foreign Keys=True";
        }

        public bool ValidateConnectionString(string connectionString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                    return false;

                // Extract file path from connection string
                if (connectionString.Contains("Data Source="))
                {
                    var dataSource = connectionString.Split("Data Source=")[1].Split(';')[0];
                    var directory = Path.GetDirectoryName(dataSource);
                    
                    // Check if directory exists or can be created
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void CreateSampleDatabase(string connectionString)
        {
            Console.WriteLine("🗄️ Creating SQLite Test Database...");
            Console.WriteLine("==========================================");

            try
            {
                // Extract file path from connection string
                var dbPath = ExtractFilePath(connectionString);
                
                // Delete existing database if it exists
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                    Console.WriteLine($"[INFO] Existing database deleted: {dbPath}");
                }

                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                // Create tables
                CreateTables(connection);

                // Insert sample data
                InsertSampleData(connection);

                Console.WriteLine($"[✅ SUCCESS] SQLite database created: {dbPath}");
                Console.WriteLine($"[INFO] File size: {new FileInfo(dbPath).Length / 1024.0:F2} KB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[❌ ERROR] SQLite database creation failed: {ex.Message}");
                throw;
            }
        }

        private string ExtractFilePath(string connectionString)
        {
            if (connectionString.Contains("Data Source="))
            {
                return connectionString.Split("Data Source=")[1].Split(';')[0];
            }
            throw new ArgumentException("Invalid SQLite connection string");
        }

        private void CreateTables(SqliteConnection connection)
        {
            var createTablesSql = @"
                -- Customers table
                CREATE TABLE Customers (
                    CustomerID INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    Email TEXT UNIQUE NOT NULL,
                    Phone TEXT,
                    Address TEXT,
                    City TEXT,
                    Country TEXT DEFAULT 'Turkey',
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                -- Categories table
                CREATE TABLE Categories (
                    CategoryID INTEGER PRIMARY KEY AUTOINCREMENT,
                    CategoryName TEXT NOT NULL,
                    Description TEXT
                );

                -- Products table
                CREATE TABLE Products (
                    ProductID INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductName TEXT NOT NULL,
                    CategoryID INTEGER,
                    UnitPrice DECIMAL(10,2) NOT NULL,
                    StockQuantity INTEGER DEFAULT 0,
                    Description TEXT,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
                );

                -- Orders table
                CREATE TABLE Orders (
                    OrderID INTEGER PRIMARY KEY AUTOINCREMENT,
                    CustomerID INTEGER NOT NULL,
                    OrderDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    TotalAmount DECIMAL(10,2) NOT NULL,
                    Status TEXT DEFAULT 'Pending',
                    ShippingAddress TEXT,
                    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
                );

                -- Order details table
                CREATE TABLE OrderDetails (
                    OrderDetailID INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderID INTEGER NOT NULL,
                    ProductID INTEGER NOT NULL,
                    Quantity INTEGER NOT NULL,
                    UnitPrice DECIMAL(10,2) NOT NULL,
                    TotalPrice DECIMAL(10,2) NOT NULL,
                    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
                    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
                );

                -- Employees table
                CREATE TABLE Employees (
                    EmployeeID INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    Email TEXT UNIQUE NOT NULL,
                    Department TEXT NOT NULL,
                    Position TEXT NOT NULL,
                    Salary DECIMAL(10,2),
                    HireDate DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";

            using var command = new SqliteCommand(createTablesSql, connection);
            command.ExecuteNonQuery();

            Console.WriteLine("[✅ OK] SQLite tables created: Customers, Categories, Products, Orders, OrderDetails, Employees");
        }

        private void InsertSampleData(SqliteConnection connection)
        {
            var random = new Random(42); // Fixed seed for reproducible data
            
            // Categories
            var categoriesSql = @"
                INSERT INTO Categories (CategoryName, Description) VALUES
                ('Electronics', 'Electronic products and accessories'),
                ('Clothing', 'Clothing and textile products'),
                ('Books', 'Books and publications'),
                ('Home & Garden', 'Home and garden products'),
                ('Sports', 'Sports and fitness products');
            ";
            ExecuteSql(connection, categoriesSql, "Categories");

            // Generate 100 Customers with diverse European names
            var customersSql = new StringBuilder("INSERT INTO Customers (FirstName, LastName, Email, Phone, Address, City, Country) VALUES \n");
            for (int i = 0; i < 100; i++)
            {
                var firstName = SampleDataGenerator.GetRandomFirstName(random);
                var lastName = SampleDataGenerator.GetRandomLastName(random);
                var email = SampleDataGenerator.GenerateEmail(firstName, lastName, random);
                var phone = SampleDataGenerator.GeneratePhone(random);
                var address = SampleDataGenerator.GenerateAddress(random);
                var city = SampleDataGenerator.GetRandomCity(random);
                var country = SampleDataGenerator.GetRandomCountry(random);

                customersSql.Append($"('{firstName}', '{lastName}', '{email}', '{phone}', '{address}', '{city}', '{country}')");
                customersSql.Append(i < 99 ? ",\n" : ";\n");
            }
            ExecuteSql(connection, customersSql.ToString(), "Customers");

            // Generate 100 Products with various prices
            var productNames = new[] 
            {
                "Laptop", "Smartphone", "Tablet", "Smartwatch", "Wireless Earbuds", "Monitor", "Keyboard", "Mouse",
                "T-Shirt", "Jeans", "Jacket", "Dress", "Sweater", "Shoes", "Sneakers", "Boots",
                "Programming Guide", "Novel", "Biography", "Cookbook", "Dictionary", "Atlas", "Magazine", "Comic Book",
                "Sofa", "Chair", "Table", "Lamp", "Carpet", "Curtain", "Vase", "Painting",
                "Football", "Basketball", "Tennis Racket", "Yoga Mat", "Dumbbell", "Bicycle", "Helmet", "Water Bottle"
            };
            
            var productsSql = new StringBuilder("INSERT INTO Products (ProductName, CategoryID, UnitPrice, StockQuantity) VALUES \n");
            for (int i = 0; i < 100; i++)
            {
                var categoryId = (i % 5) + 1;
                var baseName = productNames[random.Next(productNames.Length)];
                var productName = $"{baseName} Model {i + 1}";
                var price = Math.Round(random.NextDouble() * 10000 + 50, 2);
                var stock = random.Next(10, 500);

                productsSql.Append($"('{productName}', {categoryId}, {price}, {stock})");
                productsSql.Append(i < 99 ? ",\n" : ";\n");
            }
            ExecuteSql(connection, productsSql.ToString(), "Products");

            // Generate 100 Employees with European names
            var departments = new[] { "IT", "Sales", "Marketing", "Finance", "HR", "Operations", "Support", "R&D" };
            var positions = new[] 
            { 
                "Developer", "Manager", "Specialist", "Analyst", "Coordinator", "Director", 
                "Engineer", "Consultant", "Administrator", "Supervisor" 
            };

            var employeesSql = new StringBuilder("INSERT INTO Employees (FirstName, LastName, Email, Department, Position, Salary, HireDate) VALUES \n");
            for (int i = 0; i < 100; i++)
            {
                var firstName = SampleDataGenerator.GetRandomFirstName(random);
                var lastName = SampleDataGenerator.GetRandomLastName(random);
                var email = SampleDataGenerator.GenerateEmail(firstName, lastName, random);
                var department = departments[random.Next(departments.Length)];
                var position = positions[random.Next(positions.Length)];
                var salary = Math.Round(random.NextDouble() * 15000 + 5000, 2);
                var hireYear = random.Next(2020, 2025);
                var hireMonth = random.Next(1, 13);
                var hireDay = random.Next(1, 29);
                var hireDate = $"{hireYear:0000}-{hireMonth:00}-{hireDay:00}";

                employeesSql.Append($"('{firstName}', '{lastName}', '{email}', '{department}', '{position}', {salary}, '{hireDate}')");
                employeesSql.Append(i < 99 ? ",\n" : ";\n");
            }
            ExecuteSql(connection, employeesSql.ToString(), "Employees");

            // Generate 100 Orders
            var statuses = new[] { "Completed", "Shipped", "Pending", "Processing", "Cancelled" };
            var ordersSql = new StringBuilder("INSERT INTO Orders (CustomerID, OrderDate, TotalAmount, Status, ShippingAddress) VALUES \n");
            for (int i = 0; i < 100; i++)
            {
                var customerId = random.Next(1, 101); // Reference to Customers
                var orderYear = 2025;
                var orderMonth = random.Next(1, 11);
                var orderDay = random.Next(1, 29);
                var orderDate = $"{orderYear}-{orderMonth:00}-{orderDay:00}";
                var totalAmount = Math.Round(random.NextDouble() * 50000 + 100, 2);
                var status = statuses[random.Next(statuses.Length)];
                var city = SampleDataGenerator.GetRandomCity(random);
                var shippingAddress = $"{SampleDataGenerator.GenerateAddress(random)}, {city}";

                ordersSql.Append($"({customerId}, '{orderDate}', {totalAmount}, '{status}', '{shippingAddress}')");
                ordersSql.Append(i < 99 ? ",\n" : ";\n");
            }
            ExecuteSql(connection, ordersSql.ToString(), "Orders");

            // Generate 150 Order Details (multiple items per order)
            var orderDetailsSql = new StringBuilder("INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice, TotalPrice) VALUES \n");
            for (int i = 0; i < 150; i++)
            {
                var orderId = random.Next(1, 101); // Reference to Orders
                var productId = random.Next(1, 101); // Reference to Products
                var quantity = random.Next(1, 6);
                var unitPrice = Math.Round(random.NextDouble() * 5000 + 50, 2);
                var totalPrice = Math.Round(quantity * unitPrice, 2);

                orderDetailsSql.Append($"({orderId}, {productId}, {quantity}, {unitPrice}, {totalPrice})");
                orderDetailsSql.Append(i < 149 ? ",\n" : ";\n");
            }
            ExecuteSql(connection, orderDetailsSql.ToString(), "Order Details");

            Console.WriteLine("[✅ OK] SQLite sample data inserted - Total: 5 categories, 100 customers, 100 products, 100 employees, 100 orders, 150 order details");
        }

        private void ExecuteSql(SqliteConnection connection, string sql, string tableName)
        {
            using var command = new SqliteCommand(sql, connection);
            var rowsAffected = command.ExecuteNonQuery();
            Console.WriteLine($"[✅ OK] {tableName}: {rowsAffected} rows inserted");
        }
}
