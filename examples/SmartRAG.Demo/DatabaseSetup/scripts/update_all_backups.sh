#!/bin/bash
# Script to automatically restore AdventureWorks2022, run Python script, and cleanup
# This script automates the entire backup update process

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEMO_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_ROOT="$(cd "$DEMO_DIR/../.." && pwd)"
BACKUP_DIR="$DEMO_DIR/DatabaseBackups"
CONTAINER_NAME="smartrag-sqlserver-test"
DB_NAME="AdventureWorks2022"
DB_USER="sa"
DB_PASSWORD="${SQLSERVER_SA_PASSWORD:-}"
BACKUP_FILE="$BACKUP_DIR/AdventureWorks2022.bak"

if [ -z "$DB_PASSWORD" ]; then
    echo "❌ Error: SQLSERVER_SA_PASSWORD environment variable is not set"
    echo "   Please set it before running this script:"
    echo "   export SQLSERVER_SA_PASSWORD='your-password'"
    exit 1
fi
PYTHON_SCRIPT="$SCRIPT_DIR/copy_adventureworks_data.py"

echo "═══════════════════════════════════════════════════════════════════"
echo "🔄 BACKUP UPDATE AUTOMATION SCRIPT"
echo "═══════════════════════════════════════════════════════════════════"
echo ""

# Check if backup file exists
if [ ! -f "$BACKUP_FILE" ]; then
    echo "❌ Error: Backup file not found: $BACKUP_FILE"
    exit 1
fi

echo "📋 Steps:"
echo "  1. Copy backup file to SQL Server container"
echo "  2. Restore AdventureWorks2022 database"
echo "  3. Ensure PostgreSQL database exists"
echo "  4. Run Python script to update all backup files"
echo "  5. Drop AdventureWorks2022 database"
echo ""

# Step 1: Copy backup file to container
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 1: Copying backup file to SQL Server container..."
echo "═══════════════════════════════════════════════════════════════════"

if ! docker cp "$BACKUP_FILE" "$CONTAINER_NAME:/var/opt/mssql/backup/" 2>/dev/null; then
    echo "❌ Error: Failed to copy backup file to container"
    exit 1
fi

echo "✅ Backup file copied to container"
echo ""

# Step 2: Restore database
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 2: Restoring AdventureWorks2022 database..."
echo "═══════════════════════════════════════════════════════════════════"

RESTORE_SQL="RESTORE DATABASE [$DB_NAME] FROM DISK = '/var/opt/mssql/backup/AdventureWorks2022.bak' WITH MOVE 'AdventureWorks2022' TO '/var/opt/mssql/data/AdventureWorks2022.mdf', MOVE 'AdventureWorks2022_Log' TO '/var/opt/mssql/data/AdventureWorks2022_Log.ldf', REPLACE;"

if ! docker exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U "$DB_USER" \
    -P "$DB_PASSWORD" \
    -C \
    -Q "$RESTORE_SQL" \
    -h -1 2>&1 | grep -q "successfully processed"; then
    echo "⚠️  Warning: Database restore may have failed or database already exists"
    echo "   Continuing anyway..."
fi

# Wait a bit for database to be ready
sleep 3

# Verify database exists
if docker exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U "$DB_USER" \
    -P "$DB_PASSWORD" \
    -C \
    -Q "SELECT name FROM sys.databases WHERE name = '$DB_NAME'" \
    -h -1 2>/dev/null | grep -q "$DB_NAME"; then
    echo "✅ AdventureWorks2022 database restored successfully"
else
    echo "❌ Error: Failed to verify database restoration"
    exit 1
fi

echo ""

# Step 3: Ensure PostgreSQL database exists
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 3: Ensuring PostgreSQL database exists..."
echo "═══════════════════════════════════════════════════════════════════"

if ! docker exec smartrag-postgres-test psql -U postgres -c "SELECT 1 FROM pg_database WHERE datname = 'personmanagement';" -t 2>&1 | grep -q 1; then
    echo "Creating PostgreSQL database 'personmanagement'..."
    docker exec smartrag-postgres-test psql -U postgres -c 'CREATE DATABASE "personmanagement";' > /dev/null 2>&1
    echo "✅ PostgreSQL database created"
else
    echo "✅ PostgreSQL database already exists"
fi

echo ""

# Step 4: Run Python script
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 4: Running Python script to update backup files..."
echo "═══════════════════════════════════════════════════════════════════"
echo "⚠️  This may take a long time (data copying operations)..."
echo ""

cd "$PROJECT_ROOT"

if ! python3 "$PYTHON_SCRIPT" 2>&1; then
    echo ""
    echo "❌ Error: Python script failed"
    echo "   Database will NOT be dropped (you may need to clean up manually)"
    exit 1
fi

echo ""
echo "✅ Python script completed successfully"
echo ""

# Step 5: Drop database
echo "═══════════════════════════════════════════════════════════════════"
echo "Step 5: Dropping AdventureWorks2022 database..."
echo "═══════════════════════════════════════════════════════════════════"

DROP_SQL="DROP DATABASE IF EXISTS [$DB_NAME];"

if docker exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U "$DB_USER" \
    -P "$DB_PASSWORD" \
    -C \
    -Q "$DROP_SQL" \
    -h -1 2>&1 > /dev/null; then
    echo "✅ AdventureWorks2022 database dropped successfully"
else
    echo "⚠️  Warning: Failed to drop database (may not exist)"
fi

echo ""
echo "═══════════════════════════════════════════════════════════════════"
echo "✅ ALL STEPS COMPLETED SUCCESSFULLY"
echo "═══════════════════════════════════════════════════════════════════"
echo ""
echo "📋 Summary:"
echo "  ✅ AdventureWorks2022 restored"
echo "  ✅ Python script executed"
echo "  ✅ All backup files updated"
echo "  ✅ AdventureWorks2022 dropped"
echo ""
echo "📊 Updated backup files:"
echo "  - logisticsmanagement.backup.db (Purchasing + dbo schemas)"
echo "  - personmanagement.backup.sql"
echo "  - inventorymanagement.backup.sql"
echo "  - salesmanagement.backup.bak"
echo ""
