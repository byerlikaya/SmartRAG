using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;

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
            // Categories
            var categoriesSql = @"
                INSERT INTO Categories (CategoryName, Description) VALUES
                ('Electronics', 'Electronic products and accessories'),
                ('Clothing', 'Clothing and textile products'),
                ('Books', 'Books and publications'),
                ('Home & Garden', 'Home and garden products'),
                ('Sports', 'Sports and fitness products');
            ";

            // Customers
            var customersSql = @"
                INSERT INTO Customers (FirstName, LastName, Email, Phone, Address, City) VALUES
                ('Ahmet', 'Yılmaz', 'ahmet.yilmaz@email.com', '+90 532 123 4567', 'Atatürk Cad. No:123', 'İstanbul'),
                ('Ayşe', 'Demir', 'ayse.demir@email.com', '+90 533 234 5678', 'Cumhuriyet Mah. No:45', 'Ankara'),
                ('Mehmet', 'Kaya', 'mehmet.kaya@email.com', '+90 534 345 6789', 'Özgürlük Sok. No:67', 'İzmir'),
                ('Fatma', 'Özkan', 'fatma.ozkan@email.com', '+90 535 456 7890', 'Barış Bulvarı No:89', 'Bursa'),
                ('Ali', 'Çelik', 'ali.celik@email.com', '+90 536 567 8901', 'İstiklal Cad. No:101', 'Antalya'),
                ('Zeynep', 'Şahin', 'zeynep.sahin@email.com', '+90 537 678 9012', 'Gazi Mah. No:23', 'Gaziantep'),
                ('Mustafa', 'Aydın', 'mustafa.aydin@email.com', '+90 538 789 0123', 'Kemal Atatürk Cad. No:45', 'Konya'),
                ('Elif', 'Türk', 'elif.turk@email.com', '+90 539 890 1234', 'Cumhuriyet Mah. No:67', 'Adana');
            ";

            // Products
            var productsSql = @"
                INSERT INTO Products (ProductName, CategoryID, UnitPrice, StockQuantity, Description) VALUES
                ('iPhone 15 Pro', 1, 45000.00, 50, 'Apple iPhone 15 Pro 256GB'),
                ('Samsung Galaxy S24', 1, 35000.00, 75, 'Samsung Galaxy S24 128GB'),
                ('MacBook Air M2', 1, 25000.00, 25, 'Apple MacBook Air 13 inch M2 chip'),
                ('Men T-Shirt', 2, 150.00, 200, 'Cotton mens t-shirt various colors'),
                ('Jeans', 2, 450.00, 150, 'Classic fit jeans'),
                ('Winter Jacket', 2, 850.00, 80, 'Waterproof winter jacket'),
                ('Programming Book', 3, 120.00, 300, 'C# Programming Language Fundamentals'),
                ('Novel Book', 3, 85.00, 500, 'Bestseller novel book'),
                ('Garden Set', 4, 250.00, 60, '6-piece garden tools set'),
                ('Lawn Mower', 4, 1200.00, 15, 'Electric lawn mower'),
                ('Fitness Ball', 5, 180.00, 100, 'Training fitness ball'),
                ('Running Shoes', 5, 650.00, 120, 'Professional running shoes');
            ";

            // Employees
            var employeesSql = @"
                INSERT INTO Employees (FirstName, LastName, Email, Department, Position, Salary, HireDate) VALUES
                ('Barış', 'Yerlikaya', 'baris.yerlikaya@company.com', 'IT', 'Software Developer', 15000.00, '2023-01-15'),
                ('Selin', 'Aktaş', 'selin.aktas@company.com', 'IT', 'DevOps Engineer', 16000.00, '2023-03-20'),
                ('Emre', 'Doğan', 'emre.dogan@company.com', 'Sales', 'Sales Manager', 12000.00, '2022-11-10'),
                ('Gamze', 'Öztürk', 'gamze.ozturk@company.com', 'Marketing', 'Marketing Specialist', 10000.00, '2023-05-08'),
                ('Burak', 'Kurt', 'burak.kurt@company.com', 'Finance', 'Financial Analyst', 13000.00, '2023-02-28'),
                ('Ceren', 'Polat', 'ceren.polat@company.com', 'HR', 'HR Specialist', 9500.00, '2023-07-12');
            ";

            // Orders
            var ordersSql = @"
                INSERT INTO Orders (CustomerID, OrderDate, TotalAmount, Status, ShippingAddress) VALUES
                (1, '2024-01-15', 45000.00, 'Completed', 'Atatürk Cad. No:123, İstanbul'),
                (2, '2024-01-16', 570.00, 'Shipped', 'Cumhuriyet Mah. No:45, Ankara'),
                (3, '2024-01-17', 120.00, 'Pending', 'Özgürlük Sok. No:67, İzmir'),
                (4, '2024-01-18', 1350.00, 'Completed', 'Barış Bulvarı No:89, Bursa'),
                (1, '2024-01-19', 650.00, 'Processing', 'Atatürk Cad. No:123, İstanbul');
            ";

            // Order details
            var orderDetailsSql = @"
                INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice, TotalPrice) VALUES
                (1, 1, 1, 45000.00, 45000.00),
                (2, 4, 2, 150.00, 300.00),
                (2, 5, 1, 450.00, 450.00),
                (3, 7, 1, 120.00, 120.00),
                (4, 6, 1, 850.00, 850.00),
                (4, 9, 2, 250.00, 500.00),
                (5, 12, 1, 650.00, 650.00);
            ";

            // Insert data
            ExecuteSql(connection, categoriesSql, "Categories");
            ExecuteSql(connection, customersSql, "Customers");
            ExecuteSql(connection, productsSql, "Products");
            ExecuteSql(connection, employeesSql, "Employees");
            ExecuteSql(connection, ordersSql, "Orders");
            ExecuteSql(connection, orderDetailsSql, "Order Details");

            Console.WriteLine("[✅ OK] SQLite sample data inserted - Total: 5 categories, 8 customers, 12 products, 6 employees, 5 orders");
        }

        private void ExecuteSql(SqliteConnection connection, string sql, string tableName)
        {
            using var command = new SqliteCommand(sql, connection);
            var rowsAffected = command.ExecuteNonQuery();
            Console.WriteLine($"[✅ OK] {tableName}: {rowsAffected} rows inserted");
        }
}
