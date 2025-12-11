using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Demo.DatabaseSetup.Helpers;
using SmartRAG.Demo.DatabaseSetup.Interfaces;
using SmartRAG.Enums;
using System.Text;

namespace SmartRAG.Demo.DatabaseSetup.Creators;

/// <summary>
/// SQL Server test database creator implementation
/// Domain: Sales & Financial Transactions
/// </summary>
public class SqlServerTestDatabaseCreator : ITestDatabaseCreator
{
    #region Constants

    private const int DefaultMaxRetries = 3;

    #endregion

    #region Fields

    private readonly IConfiguration? _configuration;
    private readonly ILogger<SqlServerTestDatabaseCreator>? _logger;
    private readonly string _server;
    private readonly string _databaseName;

    #endregion

    #region Constructor

    public SqlServerTestDatabaseCreator(IConfiguration? configuration = null, ILogger<SqlServerTestDatabaseCreator>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
        string? server = null;
        string? databaseName = null;

        if (_configuration != null)
        {
            var connectionString = _configuration.GetConnectionString("SalesManagement") ??
                                 _configuration["DatabaseConnections:1:ConnectionString"];

            if (!string.IsNullOrEmpty(connectionString))
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                server = builder.DataSource;
                databaseName = builder.InitialCatalog;
            }
        }

        // Fallback to defaults if not found in config
        _server = server ?? "localhost,1433";
        _databaseName = databaseName ?? "SalesManagement";
    }

    #endregion

    #region Public Methods

    public DatabaseType GetDatabaseType() => DatabaseType.SqlServer;

    public string GetDefaultConnectionString()
    {
        return $"Server={_server};Database={_databaseName};User Id=sa;Password={GetPassword()};TrustServerCertificate=true;";
    }
    
    private string GetPassword()
    {
        if (_configuration != null)
        {
            var connectionString = _configuration.GetConnectionString("SalesManagement") ??
                                 _configuration["DatabaseConnections:1:ConnectionString"];

            if (!string.IsNullOrEmpty(connectionString))
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                if (!string.IsNullOrEmpty(builder.Password))
                {
                    return builder.Password;
                }
            }
        }

        var envPassword = Environment.GetEnvironmentVariable("SQLSERVER_SA_PASSWORD");
        return string.IsNullOrEmpty(envPassword) 
            ? throw new InvalidOperationException("SQL Server password not found in configuration or environment variables")
            : envPassword;
    }

    public string GetDescription()
    {
        return "SQL Server - Sales & Financial Transactions (CustomerID and ProductID reference SQLite)";
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

    public async Task CreateSampleDatabaseAsync(string connectionString)
    {
        _logger?.LogInformation("Starting SQL Server test database creation");

        try
        {
            _logger?.LogInformation("Step 1/3: Creating database");
            await CreateDatabaseAsync();
            _logger?.LogInformation("Database {DatabaseName} created successfully", _databaseName);

            _logger?.LogInformation("Step 2/3: Creating tables");
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await CreateTablesAsync(connection);
            }
            _logger?.LogInformation("8 tables created successfully");

            _logger?.LogInformation("Step 3/3: Inserting sample data");
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await InsertSampleDataAsync(connection);
            }
            _logger?.LogInformation("Sample data inserted successfully");

            _logger?.LogInformation("SQL Server test database created successfully");

            await VerifyDatabaseAsync(connectionString);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create SQL Server test database");
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
    /// Creates the SQL Server database, dropping it first if it exists
    /// </summary>
    private async Task CreateDatabaseAsync()
    {
        var masterConnectionString = $"Server={_server};Database=master;User Id=sa;Password={GetPassword()};TrustServerCertificate=true;";

        using (var connection = new SqlConnection(masterConnectionString))
        {
            await connection.OpenAsync();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $@"
                    IF EXISTS (SELECT name FROM sys.databases WHERE name = '{_databaseName}')
                    BEGIN
                        ALTER DATABASE {_databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE {_databaseName};
                    END";
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"CREATE DATABASE {_databaseName}";
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// Creates all required tables in the database
    /// </summary>
    /// <param name="connection">Database connection</param>
    private async Task CreateTablesAsync(SqlConnection connection)
    {
        var createTablesSql = @"
-- Orders Table (CustomerID references SQLite Customers.CustomerID)
                CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
                    CustomerID INT NOT NULL, -- References SQLite Customers
    OrderDate DATETIME DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    OrderStatus NVARCHAR(50) DEFAULT N'Pending',
    TrackingNumber NVARCHAR(100),
    OrderChannel NVARCHAR(50),
    CustomerPONumber NVARCHAR(100),
    ShippingMethod NVARCHAR(50),
    BillingAddress NVARCHAR(500)
);

CREATE INDEX idx_orders_customer ON Orders(CustomerID);
CREATE INDEX idx_orders_date ON Orders(OrderDate);
CREATE INDEX idx_orders_status ON Orders(OrderStatus);

-- Order Details (ProductID references SQLite Products.ProductID)
                CREATE TABLE OrderDetails (
    OrderDetailID INT PRIMARY KEY IDENTITY(1,1),
                    OrderID INT NOT NULL,
                    ProductID INT NOT NULL, -- References SQLite Products
                    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountRate DECIMAL(5,2) DEFAULT 0,
    Subtotal AS (Quantity * UnitPrice * (1 - DiscountRate/100)),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

CREATE INDEX idx_orderdetails_order ON OrderDetails(OrderID);
CREATE INDEX idx_orderdetails_product ON OrderDetails(ProductID);

-- Payments
CREATE TABLE Payments (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL,
    PaymentDate DATETIME DEFAULT GETDATE(),
    PaymentMethod NVARCHAR(50) NOT NULL,
    PaymentAmount DECIMAL(18,2) NOT NULL,
    PaymentStatus NVARCHAR(50) DEFAULT N'Completed',
    TransactionID NVARCHAR(100),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

CREATE INDEX idx_payments_order ON Payments(OrderID);

-- Sales Summary (Daily sales reports)
CREATE TABLE SalesSummary (
    SummaryID INT PRIMARY KEY IDENTITY(1,1),
    SalesDate DATE NOT NULL,
    TotalSales DECIMAL(18,2) NOT NULL,
    TotalOrders INT NOT NULL,
    AverageOrderAmount DECIMAL(18,2),
    TotalCustomers INT,
    UNIQUE(SalesDate)
);

CREATE INDEX idx_salessummary_date ON SalesSummary(SalesDate);

-- Invoices (NEW)
CREATE TABLE Invoices (
    InvoiceID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL,
    InvoiceNumber NVARCHAR(50) UNIQUE NOT NULL,
    InvoiceDate DATETIME DEFAULT GETDATE(),
    DueDate DATETIME,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaidAmount DECIMAL(18,2) DEFAULT 0,
    TaxAmount DECIMAL(18,2) DEFAULT 0,
    Status NVARCHAR(50) DEFAULT N'Pending',
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

CREATE INDEX idx_invoices_order ON Invoices(OrderID);
CREATE INDEX idx_invoices_status ON Invoices(Status);

-- CustomerCredits (NEW)
CREATE TABLE CustomerCredits (
    CreditID INT PRIMARY KEY IDENTITY(1,1),
    CustomerID INT NOT NULL, -- References SQLite Customers
    CreditAmount DECIMAL(18,2) NOT NULL,
    ExpiryDate DATE,
    IssuedDate DATETIME DEFAULT GETDATE(),
    UsedAmount DECIMAL(18,2) DEFAULT 0,
    Status NVARCHAR(50) DEFAULT N'Active',
    IssuedReason NVARCHAR(200)
);

CREATE INDEX idx_customercredits_customer ON CustomerCredits(CustomerID);
CREATE INDEX idx_customercredits_status ON CustomerCredits(Status);

-- Promotions (NEW)
CREATE TABLE Promotions (
    PromotionID INT PRIMARY KEY IDENTITY(1,1),
    PromotionCode NVARCHAR(50) UNIQUE NOT NULL,
    Description NVARCHAR(500),
    DiscountType NVARCHAR(20) NOT NULL, -- 'Percentage' or 'Fixed'
    DiscountValue DECIMAL(10,2) NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    MinPurchaseAmount DECIMAL(18,2) DEFAULT 0,
    MaxUsageCount INT DEFAULT 1000,
    CurrentUsageCount INT DEFAULT 0,
    IsActive BIT DEFAULT 1
);

CREATE INDEX idx_promotions_code ON Promotions(PromotionCode);
CREATE INDEX idx_promotions_dates ON Promotions(StartDate, EndDate);

-- RefundRequests (NEW)
CREATE TABLE RefundRequests (
    RefundID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL,
    RequestDate DATETIME DEFAULT GETDATE(),
    Reason NVARCHAR(500),
    RefundAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) DEFAULT N'Pending',
    ProcessedDate DATETIME,
    ProcessedBy NVARCHAR(100),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

CREATE INDEX idx_refundrequests_order ON RefundRequests(OrderID);
CREATE INDEX idx_refundrequests_status ON RefundRequests(Status);
";

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = createTablesSql;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Inserts sample data into all tables
    /// </summary>
    /// <param name="connection">Database connection</param>
    private async Task InsertSampleDataAsync(SqlConnection connection)
    {
        var random = new Random(42); // Fixed seed for reproducible data

        // Generate 300 Orders (CustomerID references SQLite Customers 1-150)
        var ordersSql = new StringBuilder();
        ordersSql.AppendLine("INSERT INTO Orders (CustomerID, OrderDate, TotalAmount, OrderStatus, TrackingNumber, OrderChannel, CustomerPONumber, ShippingMethod, BillingAddress) VALUES ");

        var statuses = new[] { "Delivered", "Shipped", "Processing", "Pending", "Cancelled" };
        var channels = new[] { "Website", "Mobile App", "Phone", "In-Store", "Partner" };
        var shippingMethods = new[] { "Standard", "Express", "Next Day", "International", "Pickup" };
        
        for (int i = 0; i < 300; i++)
        {
            // CRITICAL: CustomerID must reference actual SQLite Customers (1-150)
            var customerId = (i % 150) + 1;
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
            var channel = channels[random.Next(channels.Length)];
            var customerPO = $"N'PO-{random.Next(10000, 99999)}'";
            var shippingMethod = shippingMethods[random.Next(shippingMethods.Length)];
            var billingAddress = $"N'{SampleDataGenerator.GenerateAddress(random)}, {SampleDataGenerator.GetRandomCity(random)}'";

            ordersSql.Append($"    ({customerId}, '{orderDate}', {totalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, N'{status}', {trackingNumber}, N'{channel}', {customerPO}, N'{shippingMethod}', {billingAddress})");
            ordersSql.AppendLine(i < 299 ? "," : ";");
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = ordersSql.ToString();
                await cmd.ExecuteNonQueryAsync();
            }
            _logger?.LogInformation("Orders: 300 rows inserted");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Orders INSERT failed");
            throw;
        }

        // Generate 600 Order Details (ProductID references SQLite Products 1-250)
        var orderDetailsSql = new StringBuilder();
        orderDetailsSql.AppendLine("INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice, DiscountRate) VALUES ");

        for (int i = 0; i < 600; i++)
        {
            var orderId = (i % 300) + 1;
            // CRITICAL: ProductID must reference actual SQLite Products (1-250)
            var productId = random.Next(1, 251);
            var quantity = random.Next(1, 6);
            var unitPrice = Math.Round(random.NextDouble() * 5000 + 50, 2);
            var discountRate = random.Next(0, 21); // 0-20% discount

            orderDetailsSql.Append($"    ({orderId}, {productId}, {quantity}, {unitPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {discountRate})");
            orderDetailsSql.AppendLine(i < 599 ? "," : ";");
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = orderDetailsSql.ToString();
                await cmd.ExecuteNonQueryAsync();
            }
            _logger?.LogInformation("OrderDetails: 600 rows inserted");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "OrderDetails INSERT failed");
            throw;
        }

        // Generate 300 Payments
        var paymentsSql = new StringBuilder();
        paymentsSql.AppendLine("INSERT INTO Payments (OrderID, PaymentDate, PaymentMethod, PaymentAmount, PaymentStatus, TransactionID) VALUES ");

        var paymentMethods = new[] { "Credit Card", "Debit Card", "Bank Transfer", "Cash", "PayPal", "Cryptocurrency" };
        var paymentStatuses = new[] { "Completed", "Pending", "Failed", "Refunded" };

        for (int i = 0; i < 300; i++)
        {
            var orderId = i + 1;
            var paymentMonth = random.Next(1, 11);
            var paymentDay = random.Next(1, 29);
            var paymentHour = random.Next(9, 18);
            var paymentMinute = random.Next(0, 60);
            var paymentDate = $"2025-{paymentMonth:00}-{paymentDay:00} {paymentHour:00}:{paymentMinute:00}:00";
            var paymentMethod = paymentMethods[random.Next(paymentMethods.Length)];
            var paymentAmount = Math.Round(random.NextDouble() * 50000 + 100, 2);
            var paymentStatus = paymentStatuses[random.Next(paymentStatuses.Length)];
            var transactionId = $"N'TXN-{random.Next(100000, 999999)}'";

            paymentsSql.Append($"    ({orderId}, '{paymentDate}', N'{paymentMethod}', {paymentAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, N'{paymentStatus}', {transactionId})");
            paymentsSql.AppendLine(i < 299 ? "," : ";");
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = paymentsSql.ToString();
                await cmd.ExecuteNonQueryAsync();
            }
            _logger?.LogInformation("Payments: 300 rows inserted");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Payments INSERT failed");
            throw;
        }

        // Generate 150 Sales Summary records with unique dates
        var salesSummarySql = new StringBuilder();
        salesSummarySql.AppendLine("INSERT INTO SalesSummary (SalesDate, TotalSales, TotalOrders, AverageOrderAmount, TotalCustomers) VALUES ");
        
        // Start from Jan 1, 2025 and increment by 1 day for each record to ensure unique dates
        var baseDate = new DateTime(2025, 1, 1);
        
        for (int i = 0; i < 150; i++)
        {
            var salesDate = baseDate.AddDays(i).ToString("yyyy-MM-dd");
            var totalSales = Math.Round(random.NextDouble() * 100000 + 1000, 2);
            var totalOrders = random.Next(1, 20);
            var averageOrderAmount = Math.Round(totalSales / totalOrders, 2);
            var totalCustomers = random.Next(1, totalOrders);

            salesSummarySql.Append($"    ('{salesDate}', {totalSales.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {totalOrders}, {averageOrderAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {totalCustomers})");
            salesSummarySql.AppendLine(i < 149 ? "," : ";");
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = salesSummarySql.ToString();
                await cmd.ExecuteNonQueryAsync();
            }
            _logger?.LogInformation("SalesSummary: 150 rows inserted");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SalesSummary INSERT failed");
            throw;
        }

        // Generate 300 Invoices
        var invoicesSql = new StringBuilder();
        invoicesSql.AppendLine("INSERT INTO Invoices (OrderID, InvoiceNumber, InvoiceDate, DueDate, TotalAmount, PaidAmount, TaxAmount, Status) VALUES ");

        var invoiceStatuses = new[] { "Paid", "Pending", "Overdue", "Cancelled" };

        for (int i = 0; i < 300; i++)
        {
            var orderId = i + 1;
            var invoiceNumber = $"N'INV-2025-{10000 + i}'";
            var invoiceMonth = random.Next(1, 11);
            var invoiceDay = random.Next(1, 29);
            var invoiceDate = $"2025-{invoiceMonth:00}-{invoiceDay:00}";
            var dueDay = invoiceDay + random.Next(15, 45);
            if (dueDay > 28) dueDay = 28;
            var dueMonth = invoiceMonth;
            if (dueDay < invoiceDay)
            {
                dueMonth++;
                if (dueMonth > 12) dueMonth = 12;
            }
            var dueDate = $"2025-{dueMonth:00}-{dueDay:00}";
            var totalAmount = Math.Round(random.NextDouble() * 50000 + 100, 2);
            var taxAmount = Math.Round(totalAmount * 0.18, 2); // 18% VAT
            var paidAmount = random.NextDouble() > 0.3 ? totalAmount : 0;
            var status = paidAmount > 0 ? "Paid" : invoiceStatuses[random.Next(invoiceStatuses.Length)];

            invoicesSql.Append($"    ({orderId}, {invoiceNumber}, '{invoiceDate}', '{dueDate}', {totalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {paidAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {taxAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, N'{status}')");
            invoicesSql.AppendLine(i < 299 ? "," : ";");
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = invoicesSql.ToString();
                await cmd.ExecuteNonQueryAsync();
            }
            _logger?.LogInformation("Invoices: 300 rows inserted");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Invoices INSERT failed");
            throw;
        }

        // Generate 100 CustomerCredits
        var creditsSql = new StringBuilder();
        creditsSql.AppendLine("INSERT INTO CustomerCredits (CustomerID, CreditAmount, ExpiryDate, UsedAmount, Status, IssuedReason) VALUES ");

        var creditStatuses = new[] { "Active", "Expired", "Used", "Cancelled" };
        var creditReasons = new[] { "Promotion", "Compensation", "Loyalty Reward", "Referral Bonus", "Return Credit" };

        for (int i = 0; i < 100; i++)
        {
            var customerId = random.Next(1, 151);
            var creditAmount = Math.Round(random.NextDouble() * 1000 + 50, 2);
            var issueMonth = random.Next(1, 11);
            var issueDay = random.Next(1, 29);
            var expiryMonth = issueMonth + random.Next(3, 7);
            if (expiryMonth > 12) expiryMonth = 12;
            var expiryDate = $"2025-{expiryMonth:00}-28";
            var usedAmount = random.NextDouble() > 0.5 ? Math.Round(creditAmount * random.NextDouble(), 2) : 0;
            var status = usedAmount >= creditAmount ? "Used" : creditStatuses[random.Next(creditStatuses.Length)];
            var reason = creditReasons[random.Next(creditReasons.Length)];

            creditsSql.Append($"    ({customerId}, {creditAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{expiryDate}', {usedAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, N'{status}', N'{reason}')");
            creditsSql.AppendLine(i < 99 ? "," : ";");
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = creditsSql.ToString();
                await cmd.ExecuteNonQueryAsync();
            }
            _logger?.LogInformation("CustomerCredits: 100 rows inserted");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CustomerCredits INSERT failed");
            throw;
        }

        // Generate 50 Promotions
        var promotionsSql = new StringBuilder();
        promotionsSql.AppendLine("INSERT INTO Promotions (PromotionCode, Description, DiscountType, DiscountValue, StartDate, EndDate, MinPurchaseAmount, MaxUsageCount, CurrentUsageCount, IsActive) VALUES ");

        var discountTypes = new[] { "Percentage", "Fixed" };

        for (int i = 0; i < 50; i++)
        {
            var promoCode = $"N'PROMO{2025}{(i + 1):000}'";
            var description = $"N'Promosyon #{i + 1} - Özel İndirim'";
            var discountType = discountTypes[random.Next(discountTypes.Length)];
            var discountValue = discountType == "Percentage" 
                ? random.Next(5, 51) 
                : Math.Round(random.NextDouble() * 500 + 50, 2);
            var startMonth = random.Next(1, 11);
            var startDay = random.Next(1, 29);
            var startDate = $"2025-{startMonth:00}-{startDay:00}";
            var endMonth = startMonth + random.Next(1, 4);
            if (endMonth > 12) endMonth = 12;
            var endDay = random.Next(1, 29);
            var endDate = $"2025-{endMonth:00}-{endDay:00}";
            var minPurchase = Math.Round(random.NextDouble() * 500, 2);
            var maxUsage = random.Next(100, 1001);
            var currentUsage = random.Next(0, maxUsage);
            var isActive = currentUsage < maxUsage ? 1 : 0;

            promotionsSql.Append($"    ({promoCode}, {description}, N'{discountType}', {discountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{startDate}', '{endDate}', {minPurchase.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {maxUsage}, {currentUsage}, {isActive})");
            promotionsSql.AppendLine(i < 49 ? "," : ";");
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = promotionsSql.ToString();
                await cmd.ExecuteNonQueryAsync();
            }
            _logger?.LogInformation("Promotions: 50 rows inserted");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Promotions INSERT failed");
            throw;
        }

        // Generate 120 RefundRequests
        var refundsSql = new StringBuilder();
        refundsSql.AppendLine("INSERT INTO RefundRequests (OrderID, RequestDate, Reason, RefundAmount, Status, ProcessedDate, ProcessedBy) VALUES ");

        var refundStatuses = new[] { "Approved", "Pending", "Rejected", "Completed" };
        var refundReasons = new[] { "Defective Product", "Wrong Item", "Customer Changed Mind", "Late Delivery", "Product Not as Described" };

        for (int i = 0; i < 120; i++)
        {
            var orderId = random.Next(1, 301);
            var requestMonth = random.Next(1, 11);
            var requestDay = random.Next(1, 29);
            var requestDate = $"2025-{requestMonth:00}-{requestDay:00}";
            var reason = refundReasons[random.Next(refundReasons.Length)];
            var refundAmount = Math.Round(random.NextDouble() * 5000 + 50, 2);
            var status = refundStatuses[random.Next(refundStatuses.Length)];
            var processedDate = status != "Pending" ? $"'2025-{requestMonth:00}-{Math.Min(requestDay + random.Next(1, 5), 28):00}'" : "NULL";
            var processedBy = status != "Pending" ? "N'Support Team'" : "NULL";

            refundsSql.Append($"    ({orderId}, '{requestDate}', N'{reason}', {refundAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, N'{status}', {processedDate}, {processedBy})");
            refundsSql.AppendLine(i < 119 ? "," : ";");
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = refundsSql.ToString();
                await cmd.ExecuteNonQueryAsync();
            }
            _logger?.LogInformation("RefundRequests: 120 rows inserted");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RefundRequests INSERT failed");
            throw;
        }
    }

    /// <summary>
    /// Verifies the database by querying table row counts
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    private async Task VerifyDatabaseAsync(string connectionString)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

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

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        _logger?.LogInformation("Table {TableName}: {TotalRows} rows", reader["TableName"], reader["TotalRows"]);
                    }
                }
            }
        }
    }

    #endregion
}
