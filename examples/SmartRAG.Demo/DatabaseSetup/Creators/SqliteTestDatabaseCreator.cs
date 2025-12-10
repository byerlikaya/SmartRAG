using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.DatabaseSetup.Helpers;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// SQLite test database creator implementation
/// Domain: Product Catalog & Customer Master Data
/// Follows SOLID principles - Single Responsibility Principle
/// </summary>
public class SqliteTestDatabaseCreator : ITestDatabaseCreator
{
    #region Fields

    private readonly IConfiguration? _configuration;
    private readonly ILogger<SqliteTestDatabaseCreator>? _logger;

    #endregion

    #region Constructor

    public SqliteTestDatabaseCreator(IConfiguration? configuration = null, ILogger<SqliteTestDatabaseCreator>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    public DatabaseType GetDatabaseType() => DatabaseType.SQLite;

        public string GetDescription() => "SQLite - Product Catalog & Customer Master Data";

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

    public async Task CreateSampleDatabaseAsync(string connectionString)
    {
        _logger?.LogInformation("Starting SQLite test database creation");

        try
        {
            var dbPath = ExtractFilePath(connectionString);
            
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                _logger?.LogInformation("Existing database deleted: {DbPath}", dbPath);
            }

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            await CreateTablesAsync(connection);
            await InsertSampleDataAsync(connection);

            var fileSize = new FileInfo(dbPath).Length / 1024.0;
            _logger?.LogInformation("SQLite database created successfully: {DbPath}, Size: {FileSize:F2} KB", dbPath, fileSize);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SQLite database creation failed");
            throw;
        }
    }

    public void CreateSampleDatabase(string connectionString)
    {
        CreateSampleDatabaseAsync(connectionString).GetAwaiter().GetResult();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Extracts the file path from SQLite connection string
    /// </summary>
    /// <param name="connectionString">SQLite connection string</param>
    /// <returns>Database file path</returns>
    private string ExtractFilePath(string connectionString)
    {
        if (connectionString.Contains("Data Source="))
        {
            return connectionString.Split("Data Source=")[1].Split(';')[0];
        }
        throw new ArgumentException("Invalid SQLite connection string");
    }

    /// <summary>
    /// Creates all required tables in the database
    /// </summary>
    /// <param name="connection">Database connection</param>
    private async Task CreateTablesAsync(SqliteConnection connection)
        {
            var createTablesSql = @"
                -- Customers table (Master customer data)
                CREATE TABLE Customers (
                    CustomerID INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    Email TEXT UNIQUE NOT NULL,
                    Phone TEXT,
                    Address TEXT,
                    City TEXT,
                    Country TEXT DEFAULT 'Turkey',
                    CustomerSegment TEXT DEFAULT 'Regular',
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                -- Categories table (Hierarchical product categories)
                CREATE TABLE Categories (
                    CategoryID INTEGER PRIMARY KEY AUTOINCREMENT,
                    CategoryName TEXT NOT NULL,
                    ParentCategoryID INTEGER,
                    Description TEXT,
                    IsActive INTEGER DEFAULT 1,
                    FOREIGN KEY (ParentCategoryID) REFERENCES Categories(CategoryID)
                );

                -- Suppliers table (NEW)
                CREATE TABLE Suppliers (
                    SupplierID INTEGER PRIMARY KEY AUTOINCREMENT,
                    SupplierName TEXT NOT NULL,
                    ContactPerson TEXT,
                    Email TEXT,
                    Phone TEXT,
                    Country TEXT,
                    Rating DECIMAL(3,2) DEFAULT 5.0,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                -- Products table (Enhanced with SupplierID)
                CREATE TABLE Products (
                    ProductID INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductName TEXT NOT NULL,
                    CategoryID INTEGER,
                    SupplierID INTEGER,
                    UnitPrice DECIMAL(10,2) NOT NULL,
                    StockQuantity INTEGER DEFAULT 0,
                    Description TEXT,
                    SKU TEXT UNIQUE,
                    IsActive INTEGER DEFAULT 1,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
                    FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID)
                );

                -- ProductPriceHistory table (NEW - Historical pricing)
                CREATE TABLE ProductPriceHistory (
                    PriceHistoryID INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductID INTEGER NOT NULL,
                    EffectiveDate DATETIME NOT NULL,
                    OldPrice DECIMAL(10,2),
                    NewPrice DECIMAL(10,2) NOT NULL,
                    ChangeReason TEXT,
                    ChangedBy TEXT,
                    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
                );

                -- EmployeesReference table (Renamed from Employees for clarity)
                CREATE TABLE EmployeesReference (
                    EmployeeID INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    Email TEXT UNIQUE NOT NULL,
                    Department TEXT NOT NULL,
                    Position TEXT NOT NULL,
                    Salary DECIMAL(10,2),
                    HireDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IsActive INTEGER DEFAULT 1
                );
            ";

        using var command = new SqliteCommand(createTablesSql, connection);
        await command.ExecuteNonQueryAsync();

        _logger?.LogInformation("SQLite tables created: Customers, Categories, Suppliers, Products, ProductPriceHistory, EmployeesReference");
    }

    /// <summary>
    /// Inserts sample data into all tables
    /// </summary>
    /// <param name="connection">Database connection</param>
    private async Task InsertSampleDataAsync(SqliteConnection connection)
        {
            var random = new Random(42); // Fixed seed for reproducible data
            
            // Insert Categories (15 rows - hierarchical)
            var categoriesSql = @"
                INSERT INTO Categories (CategoryName, ParentCategoryID, Description, IsActive) VALUES
                ('Electronics', NULL, 'Electronic products and accessories', 1),
                ('Clothing', NULL, 'Clothing and textile products', 1),
                ('Books', NULL, 'Books and publications', 1),
                ('Home & Garden', NULL, 'Home and garden products', 1),
                ('Sports', NULL, 'Sports and fitness products', 1),
                ('Computers', 1, 'Desktop and laptop computers', 1),
                ('Smartphones', 1, 'Mobile phones and tablets', 1),
                ('Audio', 1, 'Headphones, speakers, and audio equipment', 1),
                ('Men Clothing', 2, 'Clothing for men', 1),
                ('Women Clothing', 2, 'Clothing for women', 1),
                ('Fiction', 3, 'Fiction books and novels', 1),
                ('Non-Fiction', 3, 'Educational and reference books', 1),
                ('Furniture', 4, 'Indoor and outdoor furniture', 1),
                ('Garden Tools', 4, 'Gardening equipment and tools', 1),
                ('Fitness Equipment', 5, 'Exercise and fitness gear', 1);
            ";
        await ExecuteSqlAsync(connection, categoriesSql, "Categories");

            // Insert Suppliers (30 rows)
            var suppliersSql = new StringBuilder("INSERT INTO Suppliers (SupplierName, ContactPerson, Email, Phone, Country, Rating) VALUES \n");
            var supplierCompanies = new[] { "Tech Solutions", "Global Traders", "Euro Supplies", "Prime Wholesale", "Quality Imports" };
            
            for (int i = 0; i < 30; i++)
            {
                var companyName = $"{supplierCompanies[random.Next(supplierCompanies.Length)]} #{i + 1}";
                var firstName = SampleDataGenerator.GetRandomFirstName(random);
                var lastName = SampleDataGenerator.GetRandomLastName(random);
                var contactPerson = $"{firstName} {lastName}";
                var email = SampleDataGenerator.GenerateEmail(firstName, lastName, random);
                var phone = SampleDataGenerator.GeneratePhone(random);
                var country = SampleDataGenerator.GetRandomCountry(random);
                var rating = Math.Round(random.NextDouble() * 2 + 3, 2); // 3.0-5.0 rating

                suppliersSql.Append($"('{companyName}', '{contactPerson}', '{email}', '{phone}', '{country}', {rating.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
                suppliersSql.Append(i < 29 ? ",\n" : ";\n");
            }
        await ExecuteSqlAsync(connection, suppliersSql.ToString(), "Suppliers");

            // Generate 150 Customers
            var customersSql = new StringBuilder("INSERT INTO Customers (FirstName, LastName, Email, Phone, Address, City, Country, CustomerSegment) VALUES \n");
            var segments = new[] { "Regular", "Premium", "VIP", "Enterprise" };
            
            for (int i = 0; i < 150; i++)
            {
                var firstName = SampleDataGenerator.GetRandomFirstName(random);
                var lastName = SampleDataGenerator.GetRandomLastName(random);
                var email = SampleDataGenerator.GenerateEmail(firstName, lastName, random);
                var phone = SampleDataGenerator.GeneratePhone(random);
                var address = SampleDataGenerator.GenerateAddress(random);
                var city = SampleDataGenerator.GetRandomCity(random);
                var country = SampleDataGenerator.GetRandomCountry(random);
                var segment = segments[random.Next(segments.Length)];

                customersSql.Append($"('{firstName}', '{lastName}', '{email}', '{phone}', '{address}', '{city}', '{country}', '{segment}')");
                customersSql.Append(i < 149 ? ",\n" : ";\n");
            }
        await ExecuteSqlAsync(connection, customersSql.ToString(), "Customers");

            // Generate 250 Products
            var productNames = new[] 
            {
                "Laptop", "Smartphone", "Tablet", "Smartwatch", "Wireless Earbuds", "Monitor", "Keyboard", "Mouse",
                "Desktop PC", "Gaming Console", "Camera", "Printer", "Router", "Smart TV", "Projector", "Scanner",
                "T-Shirt", "Jeans", "Jacket", "Dress", "Sweater", "Shoes", "Sneakers", "Boots",
                "Coat", "Shorts", "Skirt", "Hat", "Scarf", "Gloves", "Socks", "Belt",
                "Programming Guide", "Novel", "Biography", "Cookbook", "Dictionary", "Atlas", "Magazine", "Comic Book",
                "Textbook", "Journal", "Encyclopedia", "Manual", "Workbook", "Reference Book", "eBook Reader", "Audiobook",
                "Sofa", "Chair", "Table", "Lamp", "Carpet", "Curtain", "Vase", "Painting",
                "Bed", "Wardrobe", "Shelf", "Clock", "Mirror", "Plant Pot", "Candle", "Picture Frame",
                "Football", "Basketball", "Tennis Racket", "Yoga Mat", "Dumbbell", "Bicycle", "Helmet", "Water Bottle",
                "Treadmill", "Exercise Bike", "Jump Rope", "Boxing Gloves", "Swimming Goggles", "Running Shoes", "Tennis Ball", "Golf Club"
            };
            
            var productsSql = new StringBuilder("INSERT INTO Products (ProductName, CategoryID, SupplierID, UnitPrice, StockQuantity, SKU, IsActive) VALUES \n");
            for (int i = 0; i < 250; i++)
            {
                var categoryId = (i % 15) + 1;
                var supplierId = (i % 30) + 1;
                var baseName = productNames[random.Next(productNames.Length)];
                var productName = $"{baseName} Model {i + 1}";
                var price = Math.Round(random.NextDouble() * 10000 + 50, 2);
                var stock = random.Next(10, 500);
                var sku = $"SKU-{1000 + i}";

                productsSql.Append($"('{productName}', {categoryId}, {supplierId}, {price.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {stock}, '{sku}', 1)");
                productsSql.Append(i < 249 ? ",\n" : ";\n");
            }
        await ExecuteSqlAsync(connection, productsSql.ToString(), "Products");

            // Generate 500 ProductPriceHistory records
            var priceHistorySql = new StringBuilder("INSERT INTO ProductPriceHistory (ProductID, EffectiveDate, OldPrice, NewPrice, ChangeReason, ChangedBy) VALUES \n");
            var changeReasons = new[] { "Market adjustment", "Supplier price change", "Promotion", "Cost increase", "Seasonal discount", "Competitor pricing" };
            var changedByUsers = new[] { "System", "Product Manager", "Sales Team", "Admin", "Pricing Automation" };
            
            for (int i = 0; i < 500; i++)
            {
                var productId = (i % 250) + 1;
                var year = random.Next(2023, 2026);
                var month = random.Next(1, 13);
                var day = random.Next(1, 29);
                var effectiveDate = $"{year}-{month:00}-{day:00}";
                var oldPrice = Math.Round(random.NextDouble() * 10000 + 50, 2);
                var newPrice = Math.Round(oldPrice * (0.85 + random.NextDouble() * 0.30), 2); // -15% to +15%
                var reason = changeReasons[random.Next(changeReasons.Length)];
                var changedBy = changedByUsers[random.Next(changedByUsers.Length)];

                priceHistorySql.Append($"({productId}, '{effectiveDate}', {oldPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {newPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{reason}', '{changedBy}')");
                priceHistorySql.Append(i < 499 ? ",\n" : ";\n");
            }
        await ExecuteSqlAsync(connection, priceHistorySql.ToString(), "ProductPriceHistory");

            // Generate 100 Employees
            var departments = new[] { "IT", "Sales", "Marketing", "Finance", "HR", "Operations", "Support", "R&D" };
            var positions = new[] 
            { 
                "Developer", "Manager", "Specialist", "Analyst", "Coordinator", "Director", 
                "Engineer", "Consultant", "Administrator", "Supervisor" 
            };

            var employeesSql = new StringBuilder("INSERT INTO EmployeesReference (FirstName, LastName, Email, Department, Position, Salary, HireDate, IsActive) VALUES \n");
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

                employeesSql.Append($"('{firstName}', '{lastName}', '{email}', '{department}', '{position}', {salary.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{hireDate}', 1)");
                employeesSql.Append(i < 99 ? ",\n" : ";\n");
            }
        await ExecuteSqlAsync(connection, employeesSql.ToString(), "EmployeesReference");

        _logger?.LogInformation("SQLite sample data inserted - Total: 15 categories, 30 suppliers, 150 customers, 250 products, 500 price history records, 100 employees");
    }

    /// <summary>
    /// Executes SQL command and logs the result
    /// </summary>
    /// <param name="connection">Database connection</param>
    /// <param name="sql">SQL command to execute</param>
    /// <param name="tableName">Table name for logging</param>
    private async Task ExecuteSqlAsync(SqliteConnection connection, string sql, string tableName)
    {
        using var command = new SqliteCommand(sql, connection);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        _logger?.LogInformation("{TableName}: {RowsAffected} rows inserted", tableName, rowsAffected);
    }

    #endregion
}

