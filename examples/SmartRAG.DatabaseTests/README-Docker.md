# Multi-Database Docker Setup for SmartRAG DatabaseTests

This guide explains how to run SQL Server, MySQL, and PostgreSQL using Docker for testing the SmartRAG multi-database features.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (usually comes with Docker Desktop)

## Quick Start

### 1. Start All Database Containers

Navigate to the DatabaseTests directory and start all containers:

```bash
cd examples/SmartRAG.DatabaseTests
docker-compose up -d
```

This will start:
- **SQL Server 2022 Express** on port `1433`
- **MySQL 8.0** on port `3306`
- **PostgreSQL 16** on port `5432`

Each database gets its own persistent volume for data storage.

### Start Individual Databases

You can also start databases individually:

```bash
# Start only SQL Server
docker-compose up -d sqlserver

# Start only MySQL
docker-compose up -d mysql

# Start only PostgreSQL
docker-compose up -d postgres
```

### 2. Verify Containers are Running

```bash
docker-compose ps
```

You should see all three containers running with healthy status.

### 3. Check Logs

```bash
# All containers
docker-compose logs

# Specific container
docker-compose logs sqlserver
docker-compose logs mysql
docker-compose logs postgres
```

### 4. Run SmartRAG DatabaseTests

Now you can run the DatabaseTests application:

```bash
dotnet run
```

Create test databases using menu options:
- **6. 🗄️ Create SQL Server Test Database** → SalesManagement
- **7. 🐬 Create MySQL Test Database** → InventoryManagement
- **8. 🐘 Create PostgreSQL Test Database** → LogisticsManagement

## Connection Details

### SQL Server
- **Host:** localhost,1433
- **Username:** sa
- **Password:** SmartRAG@2024
- **Database:** SalesManagement (created by test app)

### MySQL
- **Host:** localhost
- **Port:** 3306
- **Username:** root
- **Password:** mysql123
- **Database:** InventoryManagement (created by test app)

### PostgreSQL
- **Host:** localhost
- **Port:** 5432
- **Username:** postgres
- **Password:** postgres123
- **Database:** LogisticsManagement (created by test app)

## Managing the Containers

### Stop All Databases

```bash
docker-compose stop
```

### Stop Individual Database

```bash
docker-compose stop sqlserver
docker-compose stop mysql
docker-compose stop postgres
```

### Start Databases Again

```bash
docker-compose start
```

### Restart Databases

```bash
docker-compose restart
```

### Remove Containers and Volumes (⚠️ Deletes all data)

```bash
docker-compose down -v
```

### View Container Resource Usage

```bash
docker stats
```

## Connect to Databases Manually (Optional)

### SQL Server (sqlcmd)

```bash
docker exec -it smartrag-sqlserver-test /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P SmartRAG@2024 -C
```

SQL commands:
```sql
-- List databases
SELECT name FROM sys.databases;
GO

-- Use database
USE SalesManagement;
GO

-- List tables
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;
GO

-- Query data
SELECT * FROM Orders;
GO

-- Exit
EXIT
```

### MySQL (mysql client)

```bash
docker exec -it smartrag-mysql-test mysql -u root -pmysql123
```

SQL commands:
```sql
-- List databases
SHOW DATABASES;

-- Use database
USE InventoryManagement;

-- List tables
SHOW TABLES;

-- Query data
SELECT * FROM Stock;

-- Exit
EXIT;
```

### PostgreSQL (psql)

```bash
docker exec -it smartrag-postgres-test psql -U postgres
```

SQL commands:
```sql
-- List databases
\l

-- Connect to database
\c LogisticsManagement

-- List tables
\dt

-- Query data
SELECT * FROM Facilities;
SELECT * FROM Shipments;
SELECT * FROM Routes;

-- Exit
\q
```

## Troubleshooting

### Ports Already in Use

If any port is already in use, you can change it in `docker-compose.yml`:

```yaml
# SQL Server - change 1433
ports:
  - "1434:1433"  # Use 1434 on host, 1433 in container

# MySQL - change 3306
ports:
  - "3307:3306"  # Use 3307 on host, 3306 in container

# PostgreSQL - change 5432
ports:
  - "5433:5432"  # Use 5433 on host, 5432 in container
```

Then update the connection strings in `appsettings.json` and test creator files.

### Container Won't Start

Check if Docker Desktop is running:

```bash
docker --version
docker-compose --version
```

### Data Persistence

Data is stored in Docker volumes. To view volumes:

```bash
docker volume ls

# Inspect specific volume
docker volume inspect smartrag-databasetests_sqlserver-data
docker volume inspect smartrag-databasetests_mysql-data
docker volume inspect smartrag-databasetests_postgres-data
```

## Security Notes

⚠️ **This setup is for local testing only!**

- Default passwords are intentionally simple for testing:
  - SQL Server: `SmartRAG@2024`
  - MySQL: `mysql123`
  - PostgreSQL: `postgres123`
- **Don't use these configurations in production!**
- Containers are not secured for external access
- Data is stored locally in Docker volumes

## Alternative: Manual Installation

If you prefer not to use Docker, you can install databases manually:

### SQL Server
- [Download SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads)
- Use credentials: `sa` / `SmartRAG@2024`

### MySQL
- [Download MySQL Community Server](https://dev.mysql.com/downloads/mysql/)
- Use credentials: `root` / `mysql123`

### PostgreSQL
- [Download PostgreSQL](https://www.postgresql.org/download/)
- Use credentials: `postgres` / `postgres123`

## Contact

For issues or questions about SmartRAG:
- **GitHub:** https://github.com/byerlikaya/SmartRAG
- **LinkedIn:** https://www.linkedin.com/in/barisyerlikaya/
- **Email:** b.yerlikaya@outlook.com

