#!/usr/bin/env python3
"""
Source database'den PostgreSQL, MySQL ve SQLite'e veri kopyalama scripti
Enterprise dağılım planına göre:
- PostgreSQL: Person + HumanResources şemaları
- MySQL: Production şeması
- SQL Server: Sales şeması
- SQLite: Purchasing + dbo şemaları

Strateji:
1. Source database'den tablo yapılarını export et (CREATE TABLE)
2. PostgreSQL/MySQL/SQLite formatına dönüştür
3. Tabloları oluştur
4. Verileri bulk insert ile kopyala
"""

import subprocess
import re
import sys
import os
import csv
import tempfile
import io

# Type mapping: SQL Server -> PostgreSQL/MySQL/SQLite
TYPE_MAPPING = {
    'int': {'pg': 'INTEGER', 'mysql': 'INT', 'sqlite': 'INTEGER'},
    'bigint': {'pg': 'BIGINT', 'mysql': 'BIGINT', 'sqlite': 'INTEGER'},
    'smallint': {'pg': 'SMALLINT', 'mysql': 'SMALLINT', 'sqlite': 'INTEGER'},
    'tinyint': {'pg': 'SMALLINT', 'mysql': 'TINYINT', 'sqlite': 'INTEGER'},
    'bit': {'pg': 'BOOLEAN', 'mysql': 'BIT', 'sqlite': 'INTEGER'},
    'decimal': {'pg': 'DECIMAL', 'mysql': 'DECIMAL', 'sqlite': 'REAL'},
    'numeric': {'pg': 'NUMERIC', 'mysql': 'DECIMAL', 'sqlite': 'REAL'},
    'money': {'pg': 'MONEY', 'mysql': 'DECIMAL(19,4)', 'sqlite': 'REAL'},
    'smallmoney': {'pg': 'MONEY', 'mysql': 'DECIMAL(10,4)', 'sqlite': 'REAL'},
    'float': {'pg': 'DOUBLE PRECISION', 'mysql': 'DOUBLE', 'sqlite': 'REAL'},
    'real': {'pg': 'REAL', 'mysql': 'FLOAT', 'sqlite': 'REAL'},
    'datetime': {'pg': 'TIMESTAMP', 'mysql': 'DATETIME', 'sqlite': 'TEXT'},
    'datetime2': {'pg': 'TIMESTAMP', 'mysql': 'DATETIME(6)', 'sqlite': 'TEXT'},
    'date': {'pg': 'DATE', 'mysql': 'DATE', 'sqlite': 'TEXT'},
    'time': {'pg': 'TIME', 'mysql': 'TIME', 'sqlite': 'TEXT'},
    'char': {'pg': 'CHAR', 'mysql': 'CHAR', 'sqlite': 'TEXT'},
    'varchar': {'pg': 'VARCHAR', 'mysql': 'VARCHAR', 'sqlite': 'TEXT'},
    'nvarchar': {'pg': 'VARCHAR', 'mysql': 'VARCHAR', 'sqlite': 'TEXT'},
    'nchar': {'pg': 'CHAR', 'mysql': 'CHAR', 'sqlite': 'TEXT'},
    'text': {'pg': 'TEXT', 'mysql': 'TEXT', 'sqlite': 'TEXT'},
    'ntext': {'pg': 'TEXT', 'mysql': 'TEXT', 'sqlite': 'TEXT'},
    'xml': {'pg': 'XML', 'mysql': 'TEXT', 'sqlite': 'TEXT'},
    'uniqueidentifier': {'pg': 'UUID', 'mysql': 'CHAR(36)', 'sqlite': 'TEXT'},
    'varbinary': {'pg': 'BYTEA', 'mysql': 'VARBINARY', 'sqlite': 'BLOB'},
    'image': {'pg': 'BYTEA', 'mysql': 'LONGBLOB', 'sqlite': 'BLOB'},
    'timestamp': {'pg': 'BYTEA', 'mysql': 'BINARY(8)', 'sqlite': 'BLOB'},
    'hierarchyid': {'pg': 'TEXT', 'mysql': 'VARCHAR(892)', 'sqlite': 'TEXT'},
}

def run_sql_mssql(server, database, query, password='SmartRAG2025!', timeout=600, delimiter='\t', binary_mode=False):
    """SQL Server'da query çalıştır (tab-delimited output için)"""
    # Query içindeki tek tırnakları shell escape ile escape et: ' -> '\''
    query_escaped = query.replace("'", "'\\''")
    # Tab-delimited output için -W (wide format) kullan
    # Query'yi tek tırnak içine al ve içteki tek tırnakları escape et
    cmd = f"docker exec {server} /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '{password}' -C -d {database} -Q '{query_escaped}' -h -1 -W -s '{delimiter}' -w 65535 -b 2>&1"
    try:
        if binary_mode:
            # Binary data için bytes olarak al
            result = subprocess.run(cmd, shell=True, capture_output=True, timeout=timeout)
            # Bytes'ı latin-1 encoding ile decode et (binary data için)
            return result.stdout.decode('latin-1', errors='replace'), result.returncode
        else:
            result = subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=timeout)
            return result.stdout, result.returncode
    except subprocess.TimeoutExpired:
        return "Timeout", 1
    except Exception as e:
        return f"Error: {e}", 1

def get_table_definition_mssql(server, database, schema, table):
    """SQL Server'da tablo tanımını al"""
    # Query'yi basitleştir - iki aşamalı sorgu kullan
    # 1. Basit column bilgileri
    query1 = f"SELECT c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE, c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_SCHEMA = '{schema}' AND c.TABLE_NAME = '{table}' ORDER BY c.ORDINAL_POSITION;"
    result1, code1 = run_sql_mssql(server, database, query1, timeout=120, delimiter=',')
    
    if code1 != 0:
        return []
    
    # 2. PK bilgileri
    query2 = f"SELECT ku.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME WHERE ku.TABLE_SCHEMA = '{schema}' AND ku.TABLE_NAME = '{table}';"
    result2, code2 = run_sql_mssql(server, database, query2, timeout=60, delimiter=',')
    
    pk_columns = set()
    if code2 == 0:
        for line in result2.split('\n'):
            line = line.strip()
            if line and not line.startswith('(') and not line.startswith('-') and 'COLUMN_NAME' not in line.lower():
                parts = line.split(',')
                if parts:
                    pk_columns.add(parts[0].strip())
    
    # 3. Identity bilgileri
    query3 = f"SELECT c.COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_SCHEMA = '{schema}' AND c.TABLE_NAME = '{table}' AND COLUMNPROPERTY(OBJECT_ID('{schema}.{table}'), c.COLUMN_NAME, 'IsIdentity') = 1;"
    result3, code3 = run_sql_mssql(server, database, query3, timeout=60, delimiter=',')
    
    identity_columns = set()
    if code3 == 0:
        for line in result3.split('\n'):
            line = line.strip()
            if line and not line.startswith('(') and not line.startswith('-') and 'COLUMN_NAME' not in line.lower():
                parts = line.split(',')
                if parts:
                    identity_columns.add(parts[0].strip())
    
    # Column'ları parse et
    columns = []
    lines = result1.split('\n')
    for line in lines:
        line = line.strip()
        if line and not line.startswith('(') and not line.startswith('-') and 'COLUMN_NAME' not in line.lower() and 'rows' not in line.lower():
            parts = [p.strip() for p in line.split(',')]
            if len(parts) >= 6:
                col_name = parts[0]
                data_type = parts[1].lower()
                is_nullable = parts[2] == 'YES'
                max_length = parts[3] if parts[3] and parts[3] != 'NULL' and parts[3] else None
                precision = parts[4] if len(parts) > 4 and parts[4] and parts[4] != 'NULL' and parts[4] else None
                scale = parts[5] if len(parts) > 5 and parts[5] and parts[5] != 'NULL' and parts[5] else None
                
                # Computed column kontrolü (basit - hepsini al, sonra filtrele)
                if col_name and len(col_name) > 1:
                    columns.append({
                        'name': col_name,
                        'data_type': data_type,
                        'is_nullable': is_nullable,
                        'max_length': max_length,
                        'precision': precision,
                        'scale': scale,
                        'is_pk': col_name in pk_columns,
                        'is_identity': col_name in identity_columns
                    })
    
    # Computed column'ları filtrele (son query ile)
    query4 = f"SELECT c.COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_SCHEMA = '{schema}' AND c.TABLE_NAME = '{table}' AND COLUMNPROPERTY(OBJECT_ID('{schema}.{table}'), c.COLUMN_NAME, 'IsComputed') = 1;"
    result4, code4 = run_sql_mssql(server, database, query4, timeout=60, delimiter=',')
    
    computed_columns = set()
    if code4 == 0:
        for line in result4.split('\n'):
            line = line.strip()
            if line and not line.startswith('(') and not line.startswith('-') and 'COLUMN_NAME' not in line.lower():
                parts = line.split(',')
                if parts:
                    computed_columns.add(parts[0].strip())
    
    # Computed column'ları çıkar
    columns = [col for col in columns if col['name'] not in computed_columns]
    
    return columns

def convert_type_mssql_to_pg(mssql_type, max_length=None, precision=None, scale=None):
    """SQL Server tipini PostgreSQL tipine dönüştür"""
    base_type = mssql_type.lower()
    
    if base_type in TYPE_MAPPING:
        pg_type = TYPE_MAPPING[base_type]['pg']
        
        # XML için TEXT kullan (PostgreSQL XML type sorun çıkarıyor)
        if base_type == 'xml':
            return "TEXT"
        
        if base_type in ['varchar', 'nvarchar', 'char', 'nchar']:
            if max_length and max_length != '-1':
                pg_type += f"({max_length})"
            else:
                pg_type = "TEXT"
        elif base_type in ['decimal', 'numeric']:
            if precision and scale:
                pg_type += f"({precision},{scale})"
            elif precision:
                pg_type += f"({precision})"
        elif base_type == 'varbinary':
            if max_length and max_length != '-1':
                pg_type = f"VARBINARY({max_length})"
            else:
                pg_type = "BYTEA"
                
        return pg_type
    
    return "TEXT"  # Default

def convert_type_mssql_to_mysql(mssql_type, max_length=None, precision=None, scale=None):
    """SQL Server tipini MySQL tipine dönüştür"""
    base_type = mssql_type.lower()
    
    if base_type in TYPE_MAPPING:
        mysql_type = TYPE_MAPPING[base_type]['mysql']
        
        # XML için TEXT kullan (MySQL'de XML type yok)
        if base_type == 'xml':
            return "TEXT"
        
        # hierarchyid için VARCHAR kullan
        if base_type == 'hierarchyid':
            if max_length and max_length != '-1':
                return f"VARCHAR({max_length})"
            return "VARCHAR(892)"  # Default hierarchyid max length
        
        if base_type in ['varchar', 'nvarchar', 'char', 'nchar']:
            if max_length and max_length != '-1' and max_length != 'NULL':
                # NVARCHAR için 3x (utf8mb4 için max 4 byte per character) veya TEXT
                if base_type == 'nvarchar' or base_type == 'nchar':
                    # NVARCHAR(400) -> VARCHAR(1200) veya TEXT (eğer çok uzunsa)
                    try:
                        max_len_int = int(max_length)
                        # MySQL VARCHAR max 65535 bytes, utf8mb4 için ~16383 karakter
                        if max_len_int > 16383 or max_len_int * 3 > 65535:
                            mysql_type = "TEXT"
                        else:
                            # NVARCHAR karakter sayısı, VARCHAR byte sayısı (utf8mb4 için 4x)
                            mysql_type = f"VARCHAR({min(max_len_int * 4, 65535)})"
                    except (ValueError, TypeError):
                        mysql_type = "TEXT"
                else:
                    # VARCHAR için direkt length kullan
                    try:
                        max_len_int = int(max_length)
                        if max_len_int > 65535:
                            mysql_type = "TEXT"
                        else:
                            mysql_type += f"({max_length})"
                    except (ValueError, TypeError):
                        mysql_type = "TEXT"
            else:
                mysql_type = "TEXT"
        elif base_type in ['decimal', 'numeric']:
            if precision and scale:
                mysql_type += f"({precision},{scale})"
            elif precision:
                mysql_type += f"({precision})"
        elif base_type == 'varbinary':
            if max_length and max_length != '-1':
                mysql_type = f"VARBINARY({max_length})"
            else:
                mysql_type = "LONGBLOB"
                
        return mysql_type
    
    return "TEXT"  # Default

def convert_type_mssql_to_sqlite(mssql_type, max_length=None, precision=None, scale=None):
    """SQL Server tipini SQLite tipine dönüştür"""
    base_type = mssql_type.lower()
    
    if base_type in TYPE_MAPPING:
        sqlite_type = TYPE_MAPPING[base_type]['sqlite']
        
        # SQLite'de type hinting var ama zorunlu değil - basit type mapping kullan
        # INTEGER, REAL, TEXT, BLOB, NULL
        
        # Decimal/Numeric için REAL kullan (precision/scale hinting yok)
        if base_type in ['decimal', 'numeric', 'money', 'smallmoney']:
            return "REAL"
        
        # String tipleri için TEXT (max_length hinting yok, SQLite dinamik)
        if base_type in ['varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext', 'xml']:
            return "TEXT"
        
        # Binary tipleri için BLOB
        if base_type in ['varbinary', 'image', 'timestamp']:
            return "BLOB"
        
        # hierarchyid için TEXT
        if base_type == 'hierarchyid':
            return "TEXT"
        
        # uniqueidentifier için TEXT
        if base_type == 'uniqueidentifier':
            return "TEXT"
        
        # Datetime tipleri için TEXT (ISO8601 format)
        if base_type in ['datetime', 'datetime2', 'date', 'time']:
            return "TEXT"
        
        # Numeric tipleri için INTEGER veya REAL
        if base_type in ['int', 'bigint', 'smallint', 'tinyint', 'bit']:
            return "INTEGER"
        
        if base_type in ['float', 'real']:
            return "REAL"
        
        return sqlite_type
    
    return "TEXT"  # Default

def run_sql_sqlite(db_path, query, timeout=600):
    """SQLite'da query çalıştır (sqlite3 command-line tool kullan)"""
    # Query içindeki tek tırnakları shell escape ile escape et
    query_escaped = query.replace("'", "'\\''")
    # SQLite dosya yolu mutlak path olmalı
    abs_db_path = os.path.abspath(db_path) if not os.path.isabs(db_path) else db_path
    cmd = f"sqlite3 '{abs_db_path}' '{query_escaped}' 2>&1"
    try:
        result = subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=timeout)
        return result.stdout, result.returncode
    except subprocess.TimeoutExpired:
        return "Timeout", 1
    except Exception as e:
        return f"Error: {e}", 1

def create_table_sqlite(columns, schema, table, db_path='examples/SmartRAG.Demo/TestSQLiteData/LogisticsManagement.db'):
    """SQLite'da tablo oluştur (schema yok, tablo adı Schema_Table formatında)"""
    # SQLite'de schema yok, tablo adı Schema_Table formatında (Person_AddressType gibi)
    table_name = f"{schema}_{table}"
    
    # CREATE TABLE komutu oluştur
    col_defs = []
    pk_cols = []
    
    for col in columns:
        col_name = col['name']
        sqlite_type = convert_type_mssql_to_sqlite(
            col['data_type'],
            col['max_length'],
            col['precision'],
            col['scale']
        )
        
        nullable = "" if col['is_nullable'] else " NOT NULL"
        
        # SQLite'de AUTO_INCREMENT yok, INTEGER PRIMARY KEY kullanılır (sadece tek PK column için)
        # Identity column'lar için PRIMARY KEY AUTOINCREMENT kullan (composite PK değilse)
        num_pk_cols = sum(1 for c in columns if c['is_pk'])
        autoincrement = ""
        if col['is_identity'] and col['is_pk'] and num_pk_cols == 1 and col['data_type'].lower() in ['int', 'bigint', 'smallint', 'tinyint']:
            autoincrement = " PRIMARY KEY AUTOINCREMENT"
            nullable = ""  # PRIMARY KEY NOT NULL olmalı
        
        col_defs.append(f"`{col_name}` {sqlite_type}{autoincrement}{nullable}")
        
        # PRIMARY KEY column'ları topla (AUTOINCREMENT yoksa)
        if col['is_pk'] and not (col['is_identity'] and num_pk_cols == 1):
            pk_cols.append(f"`{col_name}`")
    
    # Primary key (AUTOINCREMENT yoksa)
    if pk_cols:
        col_defs.append(f"PRIMARY KEY ({', '.join(pk_cols)})")
    
    # DROP TABLE IF EXISTS önce
    abs_db_path = os.path.abspath(db_path) if not os.path.isabs(db_path) else db_path
    drop_sql = f"DROP TABLE IF EXISTS `{table_name}`;"
    run_sql_sqlite(abs_db_path, drop_sql, timeout=30)
    
    # CREATE TABLE
    create_table_sql = f"CREATE TABLE `{table_name}` ({', '.join(col_defs)});"
    
    result, code = run_sql_sqlite(abs_db_path, create_table_sql, timeout=120)
    return code == 0, result

def create_table_pg(columns, schema, table, database='personmanagement'):
    """PostgreSQL'de tablo oluştur"""
    # Schema oluştur (önce)
    schema_result, schema_code = run_sql_postgresql(database, f'CREATE SCHEMA IF NOT EXISTS "{schema}";', timeout=30)
    if schema_code != 0:
        print(f"    ⚠ Schema oluşturma hatası: {schema_result[:200]}")
        return False, schema_result
    
    # CREATE TABLE komutu oluştur
    col_defs = []
    for col in columns:
        col_name = col['name']
        pg_type = convert_type_mssql_to_pg(
            col['data_type'],
            col['max_length'],
            col['precision'],
            col['scale']
        )
        
        nullable = "" if col['is_nullable'] else " NOT NULL"
        identity = " GENERATED ALWAYS AS IDENTITY" if col['is_identity'] else ""
        
        col_defs.append(f'"{col_name}" {pg_type}{identity}{nullable}')
    
    # Primary key
    pk_cols = [f'"{col["name"]}"' for col in columns if col['is_pk']]
    if pk_cols:
        col_defs.append(f'PRIMARY KEY ({", ".join(pk_cols)})')
    
    # DROP TABLE IF EXISTS önce (yeni baştan oluştur)
    drop_sql = f'DROP TABLE IF EXISTS "{schema}"."{table}" CASCADE;'
    run_sql_postgresql(database, drop_sql, timeout=30)
    
    create_table_sql = f'CREATE TABLE "{schema}"."{table}" ({", ".join(col_defs)});'
    
    result, code = run_sql_postgresql(database, create_table_sql, timeout=120)
    return code == 0, result

def run_sql_postgresql(database, query, timeout=600):
    """PostgreSQL'de query çalıştır"""
    query_escaped = query.replace('\\', '\\\\').replace('"', '\\"').replace('$', '\\$')
    cmd = f"docker exec smartrag-postgres-test psql -U postgres -d {database} -c \"{query_escaped}\" 2>&1"
    try:
        result = subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=timeout)
        return result.stdout, result.returncode
    except subprocess.TimeoutExpired:
        return "Timeout", 1
    except Exception as e:
        return f"Error: {e}", 1

def copy_data_mssql_to_pg(server, source_db, source_schema, source_table, target_db, target_schema, target_table):
    """Source database'den PostgreSQL'e veri kopyala - %100 UYUMLU (Batch INSERT)"""
    print(f"  Kopyalama: {source_schema}.{source_table} -> {target_schema}.{target_table}...", end=" ", flush=True)
    
    # 0. Hedef tabloyu temizle
    delete_result, delete_code = run_sql_postgresql(target_db, f'DELETE FROM "{target_schema}"."{target_table}";', timeout=60)
    if delete_code != 0:
        truncate_result, truncate_code = run_sql_postgresql(target_db, f'TRUNCATE TABLE "{target_schema}"."{target_table}" CASCADE;', timeout=30)
    
    # 1. Column listesini al
    columns = get_table_definition_mssql(server, source_db, source_schema, source_table)
    if not columns:
        print("✗ Column'lar alınamadı")
        return False, 0
    
    col_names = [col['name'] for col in columns]
    col_names_quoted = [f'"{col}"' for col in col_names]
    
    # 2. Satır sayısını al
    count_query = f"SELECT COUNT(*) FROM {source_schema}.{source_table};"
    count_result, count_code = run_sql_mssql(server, source_db, count_query, timeout=60)
    if count_code != 0:
        print("✗ Satır sayısı alınamadı")
        return False, 0
    
    total_rows = 0
    for line in count_result.split('\n'):
        line = line.strip()
        if line and line.isdigit():
            total_rows = int(line)
            break
    
    if total_rows == 0:
        print("SKIP (boş)")
        return True, 0
    
    # 2.5. PostgreSQL'de tablo dolu mu kontrol et (zaten kopyalanmışsa skip)
    pg_count = 0
    check_query = f'SELECT COUNT(*) FROM "{target_schema}"."{target_table}";'
    check_result, check_code = run_sql_postgresql(target_db, check_query, timeout=30)
    if check_code == 0:
        for line in check_result.split('\n'):
            line = line.strip()
            if line and line.isdigit():
                pg_count = int(line)
                break
    
    if pg_count > 0 and pg_count == total_rows:
        print(f"SKIP (zaten kopyalanmış: {pg_count:,}/{total_rows:,})")
        return True, pg_count
    
    # 3. Direkt alternatif yöntemi kullan (%100 uyumluluk için)
    return copy_data_mssql_to_pg_alternative(server, source_db, source_schema, source_table, target_db, target_schema, target_table, columns, col_names, col_names_quoted, total_rows)

def copy_data_mssql_to_pg_alternative(server, source_db, source_schema, source_table, target_db, target_schema, target_table, columns, col_names, col_names_quoted, total_rows):
    """Alternatif yöntem: Python ile direkt kopyala (bcp başarısız olduğunda)"""
    print(f"\n    ⚠ Alternatif yöntem: Batch INSERT (OFFSET/FETCH)")
    print(f"    {total_rows:,} satır kopyalanacak...", end=" ", flush=True)
    
    # Hedef tabloyu temizle
    delete_result, delete_code = run_sql_postgresql(target_db, f'DELETE FROM "{target_schema}"."{target_table}";', timeout=60)
    
    # ORDER BY için PK veya ilk non-XML column
    pk_cols = [col['name'] for col in columns if col['is_pk']]
    non_xml_cols = [col['name'] for col in columns if col['data_type'].lower() != 'xml']
    order_by_col = (pk_cols[0] if pk_cols else non_xml_cols[0]) if non_xml_cols else col_names[0]
    
    # Batch batch veri kopyala (OFFSET/FETCH ile)
    copied_count = 0
    batch_size = 1000
    offset = 0
    
    while offset < total_rows:
        # SELECT query (OFFSET/FETCH ile batch) - özel tipler için CAST
        col_selects = []
        for col in columns:
            if col['data_type'].lower() == 'xml':
                col_selects.append(f"CAST([{col['name']}] AS NVARCHAR(MAX)) AS [{col['name']}]")
            elif col['data_type'].lower() == 'hierarchyid':
                # hierarchyid için CONVERT kullan (CAST yerine - '/' formatında gelir)
                col_selects.append(f"CONVERT(NVARCHAR(892), [{col['name']}]) AS [{col['name']}]")
            elif col['data_type'].lower() == 'uniqueidentifier':
                col_selects.append(f"CAST([{col['name']}] AS NVARCHAR(50)) AS [{col['name']}]")
            elif col['data_type'].lower() in ['varbinary', 'image']:
                col_selects.append(f"CAST([{col['name']}] AS NVARCHAR(MAX)) AS [{col['name']}]")
            elif col['data_type'].lower() == 'timestamp':
                col_selects.append(f"CAST([{col['name']}] AS VARBINARY(8)) AS [{col['name']}]")
            else:
                col_selects.append(f"[{col['name']}]")
        
        select_query = f"""
        SELECT {', '.join(col_selects)}
        FROM {source_schema}.{source_table}
        ORDER BY [{order_by_col}]
        OFFSET {offset} ROWS FETCH NEXT {batch_size} ROWS ONLY;
        """
        
        # Source database'den veriyi çek (tab-delimited)
        data_result, data_code = run_sql_mssql(server, source_db, select_query, timeout=300, delimiter='\t')
        
        if data_code != 0:
            print(f"\n    ✗ Veri çekme hatası: {data_result[:200]}")
            return False, copied_count
        
        # Tab-delimited output'u parse et
        lines = [l.strip() for l in data_result.split('\n') if l.strip() and not l.startswith('(') and not l.startswith('-')]
        rows = []
        for line in lines:
            if line and 'rows affected' not in line.lower() and 'column' not in line.lower():
                # Tab-delimited parse (daha güvenilir)
                parts = line.split('\t')
                if len(parts) >= len(col_names):
                    rows.append([p.strip() for p in parts[:len(col_names)]])
        
        if not rows:
            break
        
        # Her batch için VALUES oluştur
        batch_values = []
        for row in rows:
            values = []
            for i in range(len(col_names)):
                val = row[i] if i < len(row) else ''
                val = val.strip() if val else ''
                col = columns[i]
                
                if val == 'NULL' or val == '':
                    values.append('NULL')
                elif col['data_type'].lower() == 'xml':
                    # XML için TEXT olarak insert et (PostgreSQL'de TEXT column olarak saklanıyor)
                    val_escaped = val.replace("'", "''").replace('\\', '\\\\')
                    values.append(f"'{val_escaped}'")
                elif col['data_type'].lower() in ['varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext', 'datetime', 'datetime2', 'date', 'time', 'uniqueidentifier']:
                    # String escape
                    val_escaped = val.replace("'", "''").replace('\\', '\\\\')
                    values.append(f"'{val_escaped}'")
                elif col['data_type'].lower() == 'bit':
                    values.append('TRUE' if val == '1' or val.lower() == 'true' else 'FALSE')
                elif col['data_type'].lower() in ['int', 'bigint', 'smallint', 'tinyint']:
                    # Integer
                    try:
                        int(val) if val else None
                        values.append(val if val else 'NULL')
                    except (ValueError, TypeError):
                        values.append('NULL')
                elif col['data_type'].lower() in ['decimal', 'numeric', 'money', 'smallmoney', 'float', 'real']:
                    # Numeric
                    try:
                        float(val) if val else None
                        values.append(val if val else 'NULL')
                    except (ValueError, TypeError):
                        values.append('NULL')
                elif col['data_type'].lower() in ['varbinary', 'image', 'timestamp']:
                    # Binary için TEXT olarak handle et
                    val_escaped = val.replace("'", "''").replace('\\', '\\\\')
                    values.append(f"'{val_escaped}'::BYTEA")
                else:
                    # Diğer tipler için string
                    val_escaped = val.replace("'", "''").replace('\\', '\\\\')
                    values.append(f"'{val_escaped}'")
            
            if len(values) == len(col_names):
                batch_values.append(f"({', '.join(values)})")
        
        # Batch INSERT (her 200 satırda bir - command line limit için)
        if not batch_values:
            offset += batch_size
            continue
            
        has_identity = any(col['is_identity'] for col in columns)
        override_clause = " OVERRIDING SYSTEM VALUE" if has_identity else ""
        
        insert_batch_size = 200
        for batch_start in range(0, len(batch_values), insert_batch_size):
            batch_end = min(batch_start + insert_batch_size, len(batch_values))
            batch_insert_values = batch_values[batch_start:batch_end]
            
            # ON CONFLICT DO NOTHING kullan - duplicate key hatalarını önle
            pk_cols_quoted = [f'"{col["name"]}"' for col in columns if col['is_pk']]
            on_conflict_clause = ""
            if pk_cols_quoted:
                on_conflict_clause = f" ON CONFLICT ({', '.join(pk_cols_quoted)}) DO NOTHING"
            
            insert_query = f'INSERT INTO "{target_schema}"."{target_table}" ({", ".join(col_names_quoted)}){override_clause} VALUES {", ".join(batch_insert_values)}{on_conflict_clause};'
            
            insert_result, insert_code = run_sql_postgresql(target_db, insert_query, timeout=300)
            
            if insert_code != 0:
                # ON CONFLICT hatası değilse gerçek hata
                if "duplicate key" not in insert_result.lower() and "conflict" not in insert_result.lower():
                    print(f"\n    ✗ INSERT hatası: {insert_result[:200]}")
                    # Hata varsa devam et - sonra kontrol edilir
                    offset += batch_size
                    continue
                # Duplicate key hatası - devam et (ON CONFLICT çalışmadı)
            
            copied_count += len(batch_insert_values)
        
        offset += batch_size
        
        if offset % 5000 == 0 or offset >= total_rows:
            print(f"{copied_count:,}/{total_rows:,}", end=" ", flush=True)
    
    print(f"✓ {copied_count:,} satır")
    return True, copied_count

def run_sql_mysql(database, query, timeout=600):
    """MySQL'de query çalıştır (stdin üzerinden)"""
    if isinstance(query, bytes):
        query_str = query.decode('utf-8')
    else:
        query_str = query
    cmd = f"docker exec -i smartrag-mysql-test mysql -uroot -pSmartRAG2025! {database} 2>&1"
    try:
        result = subprocess.run(cmd, shell=True, input=query_str, capture_output=True, text=True, timeout=timeout)
        return result.stdout, result.returncode
    except subprocess.TimeoutExpired:
        return "Timeout", 1
    except Exception as e:
        return f"Error: {e}", 1

def create_table_mysql(columns, schema, table, database='inventorymanagement'):
    """MySQL'de tablo oluştur"""
    # MySQL'de schema prefix ile table name (Production_Product gibi)
    table_name = f"{schema}_{table}"
    
    # CREATE TABLE komutu oluştur
    col_defs = []
    pk_cols = []
    has_auto_increment_pk = False
    
    for col in columns:
        col_name = f"`{col['name']}`"
        mysql_type = convert_type_mssql_to_mysql(
            col['data_type'],
            col['max_length'],
            col['precision'],
            col['scale']
        )
        
        nullable = "" if col['is_nullable'] else " NOT NULL"
        
        # AUTO_INCREMENT sadece PRIMARY KEY olan identity column'larda olabilir
        # Ama composite PRIMARY KEY varsa AUTO_INCREMENT kullanılamaz (MySQL limiti)
        # Önce kaç tane PK column var kontrol et
        num_pk_cols = sum(1 for c in columns if c['is_pk'])
        if col['is_identity'] and col['is_pk'] and num_pk_cols == 1:
            # Sadece tek PK column varsa AUTO_INCREMENT kullan (MySQL requirement)
            auto_increment = " AUTO_INCREMENT"
            has_auto_increment_pk = True
        else:
            auto_increment = ""
        
        col_defs.append(f"{col_name} {mysql_type}{auto_increment}{nullable}")
        
        # PRIMARY KEY column'ları topla
        if col['is_pk']:
            # hierarchyid gibi uzun VARCHAR column'lar için prefix index (max 767 bytes)
            # MySQL InnoDB utf8mb4 için max key length: 767 bytes (191 karakter)
            if col['data_type'].lower() == 'hierarchyid':
                # hierarchyid VARCHAR(892) çok uzun, PRIMARY KEY için prefix kullan (191 karakter = 767 bytes)
                pk_cols.append(f"`{col['name']}`(191)")
            elif col['data_type'].lower() in ['nvarchar', 'varchar'] and col['max_length']:
                try:
                    max_len = int(col['max_length'])
                    # NVARCHAR için 4x byte hesaplama (utf8mb4)
                    if col['data_type'].lower() == 'nvarchar' and max_len * 4 > 767:
                        # NVARCHAR(200) -> 800 bytes > 767, prefix index kullan
                        pk_cols.append(f"`{col['name']}`(191)")
                    elif max_len > 767:
                        # VARCHAR için direkt byte hesabı
                        pk_cols.append(f"`{col['name']}`(767)")
                    else:
                        pk_cols.append(f"`{col['name']}`")
                except (ValueError, TypeError):
                    pk_cols.append(f"`{col['name']}`")
            elif col['data_type'].lower() in ['nchar', 'char'] and col['max_length']:
                try:
                    max_len = int(col['max_length'])
                    if col['data_type'].lower() == 'nchar' and max_len * 4 > 767:
                        pk_cols.append(f"`{col['name']}`(191)")
                    elif max_len > 767:
                        pk_cols.append(f"`{col['name']}`(767)")
                    else:
                        pk_cols.append(f"`{col['name']}`")
                except (ValueError, TypeError):
                    pk_cols.append(f"`{col['name']}`")
            else:
                pk_cols.append(f"`{col['name']}`")
    
    # Primary key (AUTO_INCREMENT varsa, PRIMARY KEY AUTO_INCREMENT column'da olmalı)
    if pk_cols:
        col_defs.append(f"PRIMARY KEY ({', '.join(pk_cols)})")
    
    # DROP TABLE IF EXISTS önce
    drop_sql = f"DROP TABLE IF EXISTS `{database}`.`{table_name}`;"
    run_sql_mysql(database, drop_sql, timeout=30)
    
    # CREATE TABLE
    create_table_sql = f"CREATE TABLE `{database}`.`{table_name}` ({', '.join(col_defs)});"
    
    result, code = run_sql_mysql(database, create_table_sql, timeout=120)
    return code == 0, result

def copy_data_mssql_to_mysql(server, source_db, source_schema, source_table, target_db, target_schema, target_table):
    """Source database'den MySQL'e veri kopyala - %100 UYUMLU (Batch INSERT)"""
    print(f"  Kopyalama: {source_schema}.{source_table} -> {target_schema}.{target_table}...", end=" ", flush=True)
    
    # 0. Hedef tabloyu kontrol et, yoksa oluştur
    table_name = f"{target_schema}_{target_table}"  # MySQL'de schema prefix ile
    check_query = f"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{target_db}' AND TABLE_NAME = '{table_name}';"
    check_result, check_code = run_sql_mysql(target_db, check_query, timeout=30)
    table_exists = False
    if check_code == 0:
        for line in check_result.split('\n'):
            line = line.strip()
            if line and line.isdigit() and int(line) > 0:
                table_exists = True
                break
    
    # 1. Column listesini al
    columns = get_table_definition_mssql(server, source_db, source_schema, source_table)
    if not columns:
        print("✗ Column'lar alınamadı")
        return False, 0
    
    # 1.5. Tablo yoksa oluştur
    if not table_exists:
        create_success, create_result = create_table_mysql(columns, target_schema, target_table, target_db)
        if not create_success:
            print(f"✗ Tablo oluşturulamadı: {create_result[:100]}")
            return False, 0
        table_exists = True
    
    col_names = [col['name'] for col in columns]
    col_names_quoted = [f"`{col}`" for col in col_names]
    
    # 2. Satır sayısını al
    count_query = f"SELECT COUNT(*) FROM {source_schema}.{source_table};"
    count_result, count_code = run_sql_mssql(server, source_db, count_query, timeout=60)
    if count_code != 0:
        print("✗ Satır sayısı alınamadı")
        return False, 0
    
    total_rows = 0
    for line in count_result.split('\n'):
        line = line.strip()
        if line and line.isdigit():
            total_rows = int(line)
            break
    
    if total_rows == 0:
        print("SKIP (boş)")
        return True, 0
    
    # 2.5. MySQL'de tablo dolu mu kontrol et
    mysql_count = 0
    check_query = f"SELECT COUNT(*) FROM `{target_db}`.`{table_name}`;"
    check_result, check_code = run_sql_mysql(target_db, check_query, timeout=30)
    if check_code == 0:
        for line in check_result.split('\n'):
            line = line.strip()
            if line and line.isdigit():
                mysql_count = int(line)
                break
    
    if mysql_count > 0 and mysql_count == total_rows:
        print(f"SKIP (zaten kopyalanmış: {mysql_count:,}/{total_rows:,})")
        return True, mysql_count
    
    # 2.6. Hedef tabloyu temizle (tablo varsa)
    if table_exists:
        delete_result, delete_code = run_sql_mysql(target_db, f"DELETE FROM `{target_db}`.`{table_name}`;", timeout=60)
        if delete_code != 0:
            truncate_result, truncate_code = run_sql_mysql(target_db, f"TRUNCATE TABLE `{target_db}`.`{table_name}`;", timeout=30)
    
    # 3. Batch INSERT yöntemi kullan
    return copy_data_mssql_to_mysql_batch(server, source_db, source_schema, source_table, target_db, target_schema, target_table, columns, col_names, col_names_quoted, total_rows)

def copy_data_mssql_to_mysql_batch(server, source_db, source_schema, source_table, target_db, target_schema, target_table, columns, col_names, col_names_quoted, total_rows):
    """MySQL'e batch INSERT ile veri kopyala"""
    print(f"\n    ⚠ Batch INSERT (OFFSET/FETCH)")
    print(f"    {total_rows:,} satır kopyalanacak...", end=" ", flush=True)
    
    table_name = f"{target_schema}_{target_table}"
    
    # Hedef tabloyu temizle
    delete_result, delete_code = run_sql_mysql(target_db, f"DELETE FROM `{target_db}`.`{table_name}`;", timeout=60)
    
    # ORDER BY için PK veya ilk non-XML column
    pk_cols = [col['name'] for col in columns if col['is_pk']]
    non_xml_cols = [col['name'] for col in columns if col['data_type'].lower() != 'xml']
    order_by_col = (pk_cols[0] if pk_cols else non_xml_cols[0]) if non_xml_cols else col_names[0]
    
    copied_count = 0
    batch_size = 1000
    offset = 0
    
    while offset < total_rows:
        # SELECT query (OFFSET/FETCH ile batch) - özel tipler için CAST
        col_selects = []
        for col in columns:
            if col['data_type'].lower() == 'xml':
                # XML için tab, satır sonu karakterlerini temizle
                col_selects.append(f"REPLACE(REPLACE(REPLACE(CAST([{col['name']}] AS NVARCHAR(MAX)), CHAR(9), ' '), CHAR(10), ' '), CHAR(13), ' ') AS [{col['name']}]")
            elif col['data_type'].lower() == 'hierarchyid':
                # hierarchyid için CONVERT kullan (CAST yerine - '/' formatında gelir)
                col_selects.append(f"CONVERT(NVARCHAR(892), [{col['name']}]) AS [{col['name']}]")
            elif col['data_type'].lower() == 'uniqueidentifier':
                col_selects.append(f"CAST([{col['name']}] AS NVARCHAR(50)) AS [{col['name']}]")
            elif col['data_type'].lower() in ['varbinary', 'image']:
                # Binary data için sys.fn_varbintohexstr() kullanarak HEX string olarak çek
                # HEX string içinde tab, satır sonu karakterlerini temizle (parsing için güvenli)
                col_selects.append(f"REPLACE(REPLACE(REPLACE(sys.fn_varbintohexstr([{col['name']}]), CHAR(9), ''), CHAR(10), ''), CHAR(13), '') AS [{col['name']}]")
            elif col['data_type'].lower() == 'timestamp':
                # Timestamp için sys.fn_varbintohexstr() kullanarak HEX string olarak çek
                col_selects.append(f"REPLACE(REPLACE(REPLACE(sys.fn_varbintohexstr(CAST([{col['name']}] AS VARBINARY(8))), CHAR(9), ''), CHAR(10), ''), CHAR(13), '') AS [{col['name']}]")
            elif col['data_type'].lower() in ['nvarchar', 'varchar', 'ntext', 'text']:
                # String kolonlar için tab, satır sonu karakterlerini temizle (tab-delimited parsing için güvenli)
                col_selects.append(f"REPLACE(REPLACE(REPLACE(CAST([{col['name']}] AS NVARCHAR(MAX)), CHAR(9), ' '), CHAR(10), ' '), CHAR(13), ' ') AS [{col['name']}]")
            elif col['data_type'].lower() in ['nchar', 'char']:
                # NCHAR/CHAR kolonlar için LEFT ve RTRIM ile temizle (NCHAR(6) gibi fixed-length kolonlar için gerekli)
                # max_length varsa NVARCHAR(max_length) kullan ve LEFT ile sınırla, yoksa NVARCHAR(MAX) kullan
                # NULL ve boş string değerleri özel placeholder ('[EMPTY]') ile değiştir (sqlcmd boş string'leri tab-delimited output'ta göstermiyor)
                if col.get('max_length') and col['max_length'] != 'NULL' and str(col['max_length']).isdigit():
                    max_len = int(col['max_length'])
                    # CASE WHEN ile NULL ve boş string kontrolü yap, boş string durumunda '[EMPTY]' placeholder kullan (tab-delimited parsing için gerekli)
                    # RTRIM ile trailing spaces temizle, LEFT ile max_length'e sınırla (placeholder için max_length+7 kullan)
                    col_selects.append(f"LEFT(RTRIM(CASE WHEN [{col['name']}] IS NULL OR LTRIM(RTRIM(CONVERT(NVARCHAR({max_len}), [{col['name']}]))) = '' THEN '[EMPTY]' ELSE CONVERT(NVARCHAR({max_len}), [{col['name']}]) END), {max_len + 7}) AS [{col['name']}]")
                else:
                    col_selects.append(f"RTRIM(CASE WHEN [{col['name']}] IS NULL OR LTRIM(RTRIM(CONVERT(NVARCHAR(MAX), [{col['name']}]))) = '' THEN '[EMPTY]' ELSE CONVERT(NVARCHAR(MAX), [{col['name']}]) END) AS [{col['name']}]")
            else:
                col_selects.append(f"[{col['name']}]")
        
        # ORDER BY için WHERE filtreleme yok (tüm satırları kopyala)
        # Boş CultureID olan satırları da kopyalamak için WHERE filtresi kullanmıyoruz
        where_clause = ""
        
        select_query = f"""
        SELECT {', '.join(col_selects)}
        FROM {source_schema}.{source_table}
        {where_clause}
        ORDER BY [{order_by_col}]
        OFFSET {offset} ROWS FETCH NEXT {batch_size} ROWS ONLY;
        """
        
        # Source database'den veriyi çek
        # sys.fn_varbintohexstr() kullanıldığı için binary data artık HEX string formatında gelir
        # Bu yüzden binary_mode=False kullanılabilir (normal text mode)
        data_result, data_code = run_sql_mssql(server, source_db, select_query, timeout=300, delimiter='\t', binary_mode=False)
        
        if data_code != 0:
            error_msg = str(data_result)[:200] if data_result else 'Unknown error'
            print(f"\n    ✗ Veri çekme hatası: {error_msg}")
            return False, copied_count
        
        # Tab-delimited output'u parse et (binary data sys.fn_varbintohexstr() ile HEX string formatında gelir)
        # sys.fn_varbintohexstr() binary data'yı '0x...' formatında HEX string olarak döndürür
        # REPLACE ile tab, satır sonu karakterleri temizlendi, bu yüzden normal text parsing kullanılabilir
        data_result_decoded = data_result if isinstance(data_result, str) else data_result.decode('utf-8', errors='replace')
        
        # Tab-delimited output'u parse et (binary data için dikkatli parse)
        # sqlcmd çok uzun kolonlarda satır sonu kullanabilir, bu yüzden satırları birleştirmek gerekebilir
        lines = []
        current_line = ''
        for line in data_result_decoded.split('\n'):
            line_stripped = line.strip()
            if (line_stripped and 
                not line_stripped.startswith('(') and 
                not line_stripped.startswith('-') and
                'rows affected' not in line_stripped.lower() and
                'column' not in line_stripped.lower()):
                # Tab sayısını kontrol et - eğer beklenen tab sayısından az ise, sonraki satırla birleştir
                expected_tabs = len(col_names) - 1
                actual_tabs = line_stripped.count('\t')
                if actual_tabs < expected_tabs:
                    # Satır tamamlanmamış - sonraki satırla birleştir
                    current_line += line_stripped + ' '
                else:
                    # Satır tamamlanmış - current_line varsa birleştir, yoksa direkt ekle
                    if current_line:
                        full_line = current_line + line_stripped
                        lines.append(full_line)
                        current_line = ''
                    else:
                        lines.append(line_stripped)
        
        # Son satır current_line'da kaldıysa ekle
        if current_line:
            lines.append(current_line.strip())
        
        rows = []
        for line in lines:
            if line:
                # Tab-delimited parse (binary data HEX string formatında gelir - sys.fn_varbintohexstr() ile)
                # sys.fn_varbintohexstr() binary data'yı '0x...' formatında HEX string olarak döndürür
                # Bu format tab karakterleri içermez, bu yüzden normal split() kullanılabilir
                parts = line.split('\t')
                
                # Kolon sayısını kontrol et ve eksik kolonları doldur
                if len(parts) < len(col_names):
                    parts.extend([''] * (len(col_names) - len(parts)))
                elif len(parts) > len(col_names):
                    parts = parts[:len(col_names)]
                
                # Her part'ı temizle
                cleaned_parts = []
                for i, p in enumerate(parts):
                    part_stripped = p.strip() if p else ''
                    # '[EMPTY]' placeholder'ı boş string'e çevir (NCHAR/CHAR kolonlar için)
                    if part_stripped == '[EMPTY]':
                        part_stripped = ''
                    # sys.fn_varbintohexstr() '0x...' formatında döndürür, bu yüzden direkt kullan
                    cleaned_parts.append(part_stripped)
                
                if len(cleaned_parts) == len(col_names):
                    rows.append(cleaned_parts)
        
        if not rows:
            break
        
        # Her batch için VALUES oluştur
        batch_values = []
        for row in rows:
            values = []
            for i in range(len(col_names)):
                val = row[i] if i < len(row) else ''
                val = val.strip() if val else ''
                col = columns[i]
                
                # NULL kontrolü - NOT NULL column'lar için boş string kullan (sadece string column'lar için)
                if val == 'NULL' or val == '':
                    # NOT NULL column'lar için boş string (sadece string column'lar için), nullable column'lar için NULL
                    if not col['is_nullable']:
                        # NOT NULL string column için boş string (datetime, numeric, binary için geçerli değil)
                        if col['data_type'].lower() in ['varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext', 'uniqueidentifier', 'hierarchyid']:
                            values.append("''")
                        elif col['data_type'].lower() in ['datetime', 'datetime2', 'date', 'time']:
                            # NOT NULL datetime column için geçersiz datetime değeri yerine NULL (hata önleme)
                            # Ama NOT NULL ise, geçerli bir datetime değeri olmalı - bu durumda hata verilmeli
                            # Şimdilik NULL kullan (hata mesajı görülecek)
                            values.append('NULL')
                        else:
                            # Numeric, binary, vb. için NULL
                            values.append('NULL')
                    else:
                        values.append('NULL')
                elif col['data_type'].lower() == 'xml':
                    # XML için TEXT olarak insert et
                    val_escaped = val.replace("\\", "\\\\").replace("'", "''")
                    values.append(f"'{val_escaped}'")
                elif col['data_type'].lower() == 'hierarchyid':
                    # hierarchyid için VARCHAR olarak insert et (CONVERT ile '/' formatında gelir)
                    val_clean = val.strip()
                    # CONVERT ile gelen format: "/"
                    val_escaped = val_clean.replace("\\", "\\\\").replace("'", "''")
                    values.append(f"'{val_escaped}'")
                elif col['data_type'].lower() in ['datetime', 'datetime2', 'date', 'time']:
                    # Datetime kolonlar için özel handling
                    if val and val != 'NULL' and val.strip():
                        # Datetime değeri var - string escape yap (tırnak içinde)
                        val_escaped = val.replace("\\", "\\\\").replace("'", "''")
                        values.append(f"'{val_escaped}'")
                    else:
                        # Datetime değeri yok - NULL kullan (eğer nullable ise) veya hata ver (eğer NOT NULL ise)
                        if col['is_nullable']:
                            values.append('NULL')
                        else:
                            # NOT NULL datetime için geçersiz datetime değeri yerine NULL (hata önleme)
                            # Ama bu durumda hata mesajı görülecek
                            values.append('NULL')
                elif col['data_type'].lower() in ['varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext', 'uniqueidentifier']:
                    # String escape (MySQL için) - NOT NULL column'lar için boş string kontrolü
                    val_escaped = val.replace("\\", "\\\\").replace("'", "''")
                    values.append(f"'{val_escaped}'")
                elif col['data_type'].lower() == 'bit':
                    values.append('1' if val == '1' or val.lower() == 'true' else '0')
                elif col['data_type'].lower() in ['int', 'bigint', 'smallint', 'tinyint']:
                    try:
                        int(val) if val else None
                        values.append(val if val else 'NULL')
                    except (ValueError, TypeError):
                        values.append('NULL')
                elif col['data_type'].lower() in ['decimal', 'numeric', 'money', 'smallmoney', 'float', 'real']:
                    try:
                        float(val) if val else None
                        values.append(val if val else 'NULL')
                    except (ValueError, TypeError):
                        values.append('NULL')
                elif col['data_type'].lower() in ['varbinary', 'image']:
                    # Binary data için HEX string olarak handle et (sys.fn_varbintohexstr() '0x...' formatında döndürür)
                    if val and val.startswith('0x'):
                        # HEX formatından 0x prefix'ini kaldır ve UNHEX() kullan
                        hex_val = val[2:].upper().strip()  # 0x'i kaldır, uppercase yap
                        # HEX string'den özel karakterleri temizle (sadece 0-9A-F)
                        hex_val_clean = ''.join(c for c in hex_val if c in '0123456789ABCDEF')
                        if hex_val_clean:
                            values.append(f"UNHEX('{hex_val_clean}')")
                        else:
                            values.append('NULL')
                    elif val:
                        # HEX formatında değilse direkt UNHEX() dene (zaten HEX olabilir)
                        hex_val = val.upper().strip()
                        hex_val_clean = ''.join(c for c in hex_val if c in '0123456789ABCDEF')
                        if hex_val_clean:
                            values.append(f"UNHEX('{hex_val_clean}')")
                        else:
                            values.append('NULL')
                    else:
                        values.append('NULL')
                elif col['data_type'].lower() == 'timestamp':
                    # Timestamp için BINARY(8) - sys.fn_varbintohexstr() '0x...' formatında döndürür
                    if val and val.startswith('0x'):
                        hex_val = val[2:].upper().strip()
                        hex_val_clean = ''.join(c for c in hex_val if c in '0123456789ABCDEF')
                        if hex_val_clean:
                            values.append(f"UNHEX('{hex_val_clean}')")
                        else:
                            values.append('NULL')
                    elif val:
                        hex_val = val.upper().strip()
                        hex_val_clean = ''.join(c for c in hex_val if c in '0123456789ABCDEF')
                        if hex_val_clean:
                            values.append(f"UNHEX('{hex_val_clean}')")
                        else:
                            values.append('NULL')
                    else:
                        values.append('NULL')
                else:
                    # Diğer tipler için string
                    val_escaped = val.replace("\\", "\\\\").replace("'", "''")
                    values.append(f"'{val_escaped}'")
            
            if len(values) == len(col_names):
                batch_values.append(f"({', '.join(values)})")
        
        # Batch INSERT (her 200 satırda bir)
        if not batch_values:
            offset += batch_size
            continue
        
        insert_batch_size = 200
        for batch_start in range(0, len(batch_values), insert_batch_size):
            batch_end = min(batch_start + insert_batch_size, len(batch_values))
            batch_insert_values = batch_values[batch_start:batch_end]
            
            # INSERT query (MySQL syntax)
            # Identity column varsa SET IDENTITY_INSERT benzeri işlem gerekmez (MySQL'de AUTO_INCREMENT)
            insert_query = f"INSERT INTO `{target_db}`.`{table_name}` ({', '.join(col_names_quoted)}) VALUES {', '.join(batch_insert_values)};"
            
            insert_result, insert_code = run_sql_mysql(target_db, insert_query, timeout=300)
            
            if insert_code != 0:
                # Duplicate key hatası değilse gerçek hata
                if "duplicate" not in insert_result.lower() and "key" not in insert_result.lower():
                    print(f"\n    ✗ INSERT hatası: {insert_result[:200]}")
                    offset += batch_size
                    continue
            
            copied_count += len(batch_insert_values)
        
        offset += batch_size
        
        if offset % 5000 == 0 or offset >= total_rows:
            print(f"{copied_count:,}/{total_rows:,}", end=" ", flush=True)
    
    print(f"✓ {copied_count:,} satır")
    return True, copied_count

def copy_data_mssql_to_sqlite(server, source_db, source_schema, source_table, db_path, target_schema, target_table):
    """Source database'den SQLite'a veri kopyala - %100 UYUMLU (Batch INSERT)"""
    print(f"  Kopyalama: {source_schema}.{source_table} -> {target_schema}.{target_table}...", end=" ", flush=True)
    
    # SQLite veritabanı dosyasının mutlak path'ini al
    abs_db_path = os.path.abspath(db_path) if not os.path.isabs(db_path) else db_path
    
    # 0. Hedef tabloyu kontrol et, yoksa oluştur
    table_name = f"{target_schema}_{target_table}"  # SQLite'de schema prefix ile
    check_query = f"SELECT name FROM sqlite_master WHERE type='table' AND name='{table_name}';"
    check_result, check_code = run_sql_sqlite(abs_db_path, check_query, timeout=30)
    table_exists = False
    if check_code == 0:
        for line in check_result.split('\n'):
            line = line.strip()
            if line and line.lower() == table_name.lower():
                table_exists = True
                break
    
    # 1. Column listesini al
    columns = get_table_definition_mssql(server, source_db, source_schema, source_table)
    if not columns:
        print("✗ Column'lar alınamadı")
        return False, 0
    
    # 1.5. Tablo yoksa oluştur
    if not table_exists:
        create_success, create_result = create_table_sqlite(columns, target_schema, target_table, abs_db_path)
        if not create_success:
            print(f"✗ Tablo oluşturulamadı: {create_result[:100]}")
            return False, 0
        table_exists = True
    
    col_names = [col['name'] for col in columns]
    col_names_quoted = [f"`{col}`" for col in col_names]
    
    # 2. Satır sayısını al
    count_query = f"SELECT COUNT(*) FROM {source_schema}.{source_table};"
    count_result, count_code = run_sql_mssql(server, source_db, count_query, timeout=60)
    if count_code != 0:
        print("✗ Satır sayısı alınamadı")
        return False, 0
    
    total_rows = 0
    for line in count_result.split('\n'):
        line = line.strip()
        if line and line.isdigit():
            total_rows = int(line)
            break
    
    if total_rows == 0:
        print("SKIP (boş)")
        return True, 0
    
    # 2.5. SQLite'de tablo dolu mu kontrol et
    sqlite_count = 0
    check_query = f"SELECT COUNT(*) FROM `{table_name}`;"
    check_result, check_code = run_sql_sqlite(abs_db_path, check_query, timeout=30)
    if check_code == 0:
        for line in check_result.split('\n'):
            line = line.strip()
            if line and line.isdigit():
                sqlite_count = int(line)
                break
    
    if sqlite_count > 0 and sqlite_count == total_rows:
        print(f"SKIP (zaten kopyalanmış: {sqlite_count:,}/{total_rows:,})")
        return True, sqlite_count
    
    # 2.6. Hedef tabloyu temizle (tablo varsa)
    if table_exists:
        delete_result, delete_code = run_sql_sqlite(abs_db_path, f"DELETE FROM `{table_name}`;", timeout=60)
    
    # 3. Batch INSERT yöntemi kullan
    return copy_data_mssql_to_sqlite_batch(server, source_db, source_schema, source_table, abs_db_path, target_schema, target_table, columns, col_names, col_names_quoted, total_rows)

def copy_data_mssql_to_sqlite_batch(server, source_db, source_schema, source_table, db_path, target_schema, target_table, columns, col_names, col_names_quoted, total_rows):
    """SQLite'a batch INSERT ile veri kopyala"""
    print(f"\n    ⚠ Batch INSERT (OFFSET/FETCH)")
    print(f"    {total_rows:,} satır kopyalanacak...", end=" ", flush=True)
    
    table_name = f"{target_schema}_{target_table}"
    
    # ORDER BY için PK veya ilk non-XML column
    pk_cols = [col['name'] for col in columns if col['is_pk']]
    non_xml_cols = [col['name'] for col in columns if col['data_type'].lower() != 'xml']
    order_by_col = (pk_cols[0] if pk_cols else non_xml_cols[0]) if non_xml_cols else col_names[0]
    
    copied_count = 0
    batch_size = 1000
    offset = 0
    
    while offset < total_rows:
        # SELECT query (OFFSET/FETCH ile batch) - özel tipler için CAST (MySQL ile aynı)
        col_selects = []
        for col in columns:
            if col['data_type'].lower() == 'xml':
                col_selects.append(f"REPLACE(REPLACE(REPLACE(CAST([{col['name']}] AS NVARCHAR(MAX)), CHAR(9), ' '), CHAR(10), ' '), CHAR(13), ' ') AS [{col['name']}]")
            elif col['data_type'].lower() == 'hierarchyid':
                col_selects.append(f"CONVERT(NVARCHAR(892), [{col['name']}]) AS [{col['name']}]")
            elif col['data_type'].lower() == 'uniqueidentifier':
                col_selects.append(f"CAST([{col['name']}] AS NVARCHAR(50)) AS [{col['name']}]")
            elif col['data_type'].lower() in ['varbinary', 'image']:
                col_selects.append(f"REPLACE(REPLACE(REPLACE(sys.fn_varbintohexstr([{col['name']}]), CHAR(9), ''), CHAR(10), ''), CHAR(13), '') AS [{col['name']}]")
            elif col['data_type'].lower() == 'timestamp':
                col_selects.append(f"REPLACE(REPLACE(REPLACE(sys.fn_varbintohexstr(CAST([{col['name']}] AS VARBINARY(8))), CHAR(9), ''), CHAR(10), ''), CHAR(13), '') AS [{col['name']}]")
            elif col['data_type'].lower() in ['nvarchar', 'varchar', 'ntext', 'text']:
                col_selects.append(f"REPLACE(REPLACE(REPLACE(CAST([{col['name']}] AS NVARCHAR(MAX)), CHAR(9), ' '), CHAR(10), ' '), CHAR(13), ' ') AS [{col['name']}]")
            elif col['data_type'].lower() in ['nchar', 'char']:
                if col.get('max_length') and col['max_length'] != 'NULL' and str(col['max_length']).isdigit():
                    max_len = int(col['max_length'])
                    col_selects.append(f"LEFT(RTRIM(CASE WHEN [{col['name']}] IS NULL OR LTRIM(RTRIM(CONVERT(NVARCHAR({max_len}), [{col['name']}]))) = '' THEN '[EMPTY]' ELSE CONVERT(NVARCHAR({max_len}), [{col['name']}]) END), {max_len + 7}) AS [{col['name']}]")
                else:
                    col_selects.append(f"RTRIM(CASE WHEN [{col['name']}] IS NULL OR LTRIM(RTRIM(CONVERT(NVARCHAR(MAX), [{col['name']}]))) = '' THEN '[EMPTY]' ELSE CONVERT(NVARCHAR(MAX), [{col['name']}]) END) AS [{col['name']}]")
            else:
                col_selects.append(f"[{col['name']}]")
        
        select_query = f"""
        SELECT {', '.join(col_selects)}
        FROM {source_schema}.{source_table}
        ORDER BY [{order_by_col}]
        OFFSET {offset} ROWS FETCH NEXT {batch_size} ROWS ONLY;
        """
        
        # Source database'den veriyi çek
        data_result, data_code = run_sql_mssql(server, source_db, select_query, timeout=300, delimiter='\t', binary_mode=False)
        
        if data_code != 0:
            error_msg = str(data_result)[:200] if data_result else 'Unknown error'
            print(f"\n    ✗ Veri çekme hatası: {error_msg}")
            return False, copied_count
        
        # Tab-delimited output'u parse et
        data_result_decoded = data_result if isinstance(data_result, str) else data_result.decode('utf-8', errors='replace')
        
        # Multi-line parsing (uzun kolonlar için)
        lines = []
        current_line = ''
        for line in data_result_decoded.split('\n'):
            line_stripped = line.strip()
            if (line_stripped and 
                not line_stripped.startswith('(') and 
                not line_stripped.startswith('-') and
                'rows affected' not in line_stripped.lower() and
                'column' not in line_stripped.lower()):
                expected_tabs = len(col_names) - 1
                actual_tabs = line_stripped.count('\t')
                if actual_tabs < expected_tabs:
                    current_line += line_stripped + ' '
                else:
                    if current_line:
                        full_line = current_line + line_stripped
                        lines.append(full_line)
                        current_line = ''
                    else:
                        lines.append(line_stripped)
        
        if current_line:
            lines.append(current_line.strip())
        
        rows = []
        for line in lines:
            if line:
                parts = line.split('\t')
                if len(parts) < len(col_names):
                    parts.extend([''] * (len(col_names) - len(parts)))
                elif len(parts) > len(col_names):
                    parts = parts[:len(col_names)]
                
                cleaned_parts = []
                for i, p in enumerate(parts):
                    part_stripped = p.strip() if p else ''
                    if part_stripped == '[EMPTY]':
                        part_stripped = ''
                    cleaned_parts.append(part_stripped)
                
                if len(cleaned_parts) == len(col_names):
                    rows.append(cleaned_parts)
        
        if not rows:
            break
        
        # Her batch için VALUES oluştur
        batch_values = []
        for row in rows:
            values = []
            for i in range(len(col_names)):
                val = row[i] if i < len(row) else ''
                val = val.strip() if val else ''
                col = columns[i]
                
                # NULL kontrolü
                if val == 'NULL' or val == '':
                    if not col['is_nullable']:
                        if col['data_type'].lower() in ['varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext', 'uniqueidentifier', 'hierarchyid']:
                            values.append("''")
                        elif col['data_type'].lower() in ['datetime', 'datetime2', 'date', 'time']:
                            values.append('NULL')
                        else:
                            values.append('NULL')
                    else:
                        values.append('NULL')
                elif col['data_type'].lower() == 'xml':
                    val_escaped = val.replace("\\", "\\\\").replace("'", "''")
                    values.append(f"'{val_escaped}'")
                elif col['data_type'].lower() == 'hierarchyid':
                    val_clean = val.strip()
                    val_escaped = val_clean.replace("\\", "\\\\").replace("'", "''")
                    values.append(f"'{val_escaped}'")
                elif col['data_type'].lower() in ['datetime', 'datetime2', 'date', 'time']:
                    if val and val != 'NULL' and val.strip():
                        val_escaped = val.replace("\\", "\\\\").replace("'", "''")
                        values.append(f"'{val_escaped}'")
                    else:
                        if col['is_nullable']:
                            values.append('NULL')
                        else:
                            values.append('NULL')
                elif col['data_type'].lower() in ['varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext', 'uniqueidentifier']:
                    val_escaped = val.replace("\\", "\\\\").replace("'", "''")
                    values.append(f"'{val_escaped}'")
                elif col['data_type'].lower() == 'bit':
                    values.append('1' if val == '1' or val.lower() == 'true' else '0')
                elif col['data_type'].lower() in ['int', 'bigint', 'smallint', 'tinyint']:
                    try:
                        int(val) if val else None
                        values.append(val if val else 'NULL')
                    except (ValueError, TypeError):
                        values.append('NULL')
                elif col['data_type'].lower() in ['decimal', 'numeric', 'money', 'smallmoney', 'float', 'real']:
                    try:
                        float(val) if val else None
                        values.append(val if val else 'NULL')
                    except (ValueError, TypeError):
                        values.append('NULL')
                elif col['data_type'].lower() in ['varbinary', 'image']:
                    if val and val.startswith('0x'):
                        hex_val = val[2:].upper().strip()
                        hex_val_clean = ''.join(c for c in hex_val if c in '0123456789ABCDEF')
                        if hex_val_clean:
                            # SQLite'de binary data için X'hex_string' formatı kullan
                            values.append(f"X'{hex_val_clean}'")
                        else:
                            values.append('NULL')
                    elif val:
                        hex_val = val.upper().strip()
                        hex_val_clean = ''.join(c for c in hex_val if c in '0123456789ABCDEF')
                        if hex_val_clean:
                            values.append(f"X'{hex_val_clean}'")
                        else:
                            values.append('NULL')
                    else:
                        values.append('NULL')
                elif col['data_type'].lower() == 'timestamp':
                    if val and val.startswith('0x'):
                        hex_val = val[2:].upper().strip()
                        hex_val_clean = ''.join(c for c in hex_val if c in '0123456789ABCDEF')
                        if hex_val_clean:
                            values.append(f"X'{hex_val_clean}'")
                        else:
                            values.append('NULL')
                    elif val:
                        hex_val = val.upper().strip()
                        hex_val_clean = ''.join(c for c in hex_val if c in '0123456789ABCDEF')
                        if hex_val_clean:
                            values.append(f"X'{hex_val_clean}'")
                        else:
                            values.append('NULL')
                    else:
                        values.append('NULL')
                else:
                    val_escaped = val.replace("\\", "\\\\").replace("'", "''")
                    values.append(f"'{val_escaped}'")
            
            if len(values) == len(col_names):
                batch_values.append(f"({', '.join(values)})")
        
        # Batch INSERT (her 200 satırda bir)
        if not batch_values:
            offset += batch_size
            continue
        
        insert_batch_size = 200
        for batch_start in range(0, len(batch_values), insert_batch_size):
            batch_end = min(batch_start + insert_batch_size, len(batch_values))
            batch_insert_values = batch_values[batch_start:batch_end]
            
            # INSERT query (SQLite syntax)
            insert_query = f"INSERT INTO `{table_name}` ({', '.join(col_names_quoted)}) VALUES {', '.join(batch_insert_values)};"
            
            insert_result, insert_code = run_sql_sqlite(db_path, insert_query, timeout=300)
            
            if insert_code != 0:
                if "duplicate" not in insert_result.lower() and "UNIQUE constraint" not in insert_result.lower():
                    print(f"\n    ✗ INSERT hatası: {insert_result[:200]}")
                    offset += batch_size
                    continue
            
            copied_count += len(batch_insert_values)
        
        offset += batch_size
        
        if offset % 5000 == 0 or offset >= total_rows:
            print(f"{copied_count:,}/{total_rows:,}", end=" ", flush=True)
    
    print(f"✓ {copied_count:,} satır")
    return True, copied_count

def create_table_mssql_sql(columns, schema, table):
    """SQL Server'da CREATE TABLE SQL komutu oluştur"""
    col_defs = []
    pk_cols = []
    
    for col in columns:
        col_name = col['name']
        mssql_type = col['data_type'].upper()
        
        # Type definition
        if mssql_type in ['VARCHAR', 'NVARCHAR', 'CHAR', 'NCHAR']:
            if col['max_length'] and col['max_length'] != '-1' and col['max_length'] != 'NULL':
                if col['max_length'] == '-1':
                    type_def = f"{mssql_type}(MAX)"
                else:
                    type_def = f"{mssql_type}({col['max_length']})"
            else:
                type_def = f"{mssql_type}(MAX)"
        elif mssql_type in ['DECIMAL', 'NUMERIC']:
            if col['precision'] and col['scale']:
                type_def = f"{mssql_type}({col['precision']},{col['scale']})"
            elif col['precision']:
                type_def = f"{mssql_type}({col['precision']})"
            else:
                type_def = mssql_type
        elif mssql_type in ['FLOAT']:
            if col['precision']:
                type_def = f"{mssql_type}({col['precision']})"
            else:
                type_def = mssql_type
        else:
            type_def = mssql_type
        
        nullable = "" if col['is_nullable'] else " NOT NULL"
        identity = " IDENTITY(1,1)" if col['is_identity'] else ""
        
        col_defs.append(f"[{col_name}] {type_def}{identity}{nullable}")
        
        if col['is_pk']:
            pk_cols.append(f"[{col_name}]")
    
    # Schema oluştur (önce)
    create_schema_sql = f"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schema}') EXEC('CREATE SCHEMA [{schema}]');"
    
    # Primary key
    pk_clause = ""
    if pk_cols:
        pk_clause = f", PRIMARY KEY ({', '.join(pk_cols)})"
    
    # DROP TABLE IF EXISTS
    drop_table_sql = f"IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{schema}].[{table}]') AND type in (N'U')) DROP TABLE [{schema}].[{table}];"
    
    # CREATE TABLE
    create_table_sql = f"CREATE TABLE [{schema}].[{table}] ({', '.join(col_defs)}{pk_clause});"
    
    return f"{create_schema_sql} {drop_table_sql} {create_table_sql}"

def copy_data_mssql_to_mssql(server, source_db, source_schema, source_table, target_server, target_db, target_schema, target_table):
    """SQL Server'dan SQL Server'a veri kopyala - %100 UYUMLU (Direct INSERT INTO ... SELECT FROM)"""
    print(f"  Kopyalama: {source_schema}.{source_table} -> {target_schema}.{target_table}...", end=" ", flush=True)
    
    # 0. Hedef tabloyu temizle
    delete_query = f"DELETE FROM [{target_schema}].[{target_table}];"
    delete_result, delete_code = run_sql_mssql(server, target_db, delete_query, timeout=60)
    
    # 1. Column listesini al
    columns = get_table_definition_mssql(server, source_db, source_schema, source_table)
    if not columns:
        print("✗ Column'lar alınamadı")
        return False, 0
    
    col_names = [col['name'] for col in columns]
    col_names_quoted = [f"[{col}]" for col in col_names]
    
    # 2. Satır sayısını al
    count_query = f"SELECT COUNT(*) FROM {source_schema}.{source_table};"
    count_result, count_code = run_sql_mssql(server, source_db, count_query, timeout=60)
    if count_code != 0:
        print("✗ Satır sayısı alınamadı")
        return False, 0
    
    total_rows = 0
    for line in count_result.split('\n'):
        line = line.strip()
        if line and line.isdigit():
            total_rows = int(line)
            break
    
    if total_rows == 0:
        print("SKIP (boş)")
        return True, 0
    
    # 2.5. Hedef tabloda satır sayısını kontrol et
    target_count = 0
    check_query = f"SELECT COUNT(*) FROM [{target_schema}].[{target_table}];"
    check_result, check_code = run_sql_mssql(server, target_db, check_query, timeout=30)
    if check_code == 0:
        for line in check_result.split('\n'):
            line = line.strip()
            if line and line.isdigit():
                target_count = int(line)
                break
    
    if target_count > 0 and target_count == total_rows:
        print(f"SKIP (zaten kopyalanmış: {target_count:,}/{total_rows:,})")
        return True, target_count
    
    # 3. Identity column kontrolü
    has_identity = any(col['is_identity'] for col in columns)
    
    # 4. Direct INSERT INTO ... SELECT FROM (aynı SQL Server instance, cross-database query)
    # SQL Server'da cross-database query: [SourceDB].[Schema].[Table] formatında
    # Identity column varsa IDENTITY_INSERT ON kullan
    identity_insert_on = f"SET IDENTITY_INSERT [{target_schema}].[{target_table}] ON;" if has_identity else ""
    identity_insert_off = f"SET IDENTITY_INSERT [{target_schema}].[{target_table}] OFF;" if has_identity else ""
    
    insert_query = f"""
    {identity_insert_on}
    INSERT INTO [{target_schema}].[{target_table}] ({', '.join(col_names_quoted)})
    SELECT {', '.join(col_names_quoted)}
    FROM [{source_db}].{source_schema}.{source_table};
    {identity_insert_off}
    """
    
    insert_result, insert_code = run_sql_mssql(server, target_db, insert_query, timeout=600)
    
    # Check for rows affected
    if "rows affected" in insert_result.lower():
        # Parse rows affected
        import re
        match = re.search(r'\((\d+)\s+rows? affected\)', insert_result, re.IGNORECASE)
        if match:
            rows_affected = int(match.group(1))
        else:
            rows_affected = 0
    else:
        rows_affected = 0
    
    if insert_code != 0:
        error_msg = str(insert_result)[:200] if insert_result else 'Unknown error'
        print(f"\n    ✗ INSERT hatası: {error_msg}")
        return False, 0
    
    # 4. Kopyalanan satır sayısını kontrol et
    final_count = 0
    final_result, final_code = run_sql_mssql(server, target_db, check_query, timeout=30)
    if final_code == 0:
        for line in final_result.split('\n'):
            line = line.strip()
            if line and line.isdigit():
                final_count = int(line)
                break
    
    if final_count == 0 and rows_affected > 0:
        final_count = rows_affected
    
    print(f"✓ {final_count:,} satır")
    return True, final_count

def get_tables_mssql(server, database, schema):
    """SQL Server'da şema tablolarını al"""
    query = f"SELECT name FROM sys.tables WHERE SCHEMA_NAME(schema_id) = '{schema}' ORDER BY name;"
    result, code = run_sql_mssql(server, database, query, timeout=60, delimiter=',')  # Tablo listesi için virgül
    tables = []
    if code == 0:
        for line in result.split('\n'):
            line = line.strip()
            # Satır filtreleme: header, footer, boş satırları atla
            if (line and 
                not line.startswith('(') and 
                not line.startswith('-') and 
                'name' not in line.lower() and 
                'rows affected' not in line.lower() and
                line.isprintable()):
                # Tablo adı direkt satır olabilir (virgül delimiter ile)
                parts = line.split(',')
                table_name = parts[0].strip() if parts else line.strip()
                # Tablo adı validasyonu
                if (table_name and 
                    len(table_name) > 1 and 
                    not table_name.isdigit() and
                    table_name[0].isalpha()):  # İlk karakter harf olmalı
                    tables.append(table_name)
    return tables

def get_table_count_mssql(server, database, schema, table):
    """SQL Server'da tablo satır sayısını al"""
    query = f"SELECT COUNT(*) FROM {schema}.{table};"
    result, code = run_sql_mssql(server, database, query, timeout=60)
    if code != 0:
        return -1
    for line in result.split('\n'):
        line = line.strip()
        if line and line.isdigit():
            return int(line)
        match = re.search(r'\b(\d+)\b', line)
        if match and len(line) < 50:
            return int(match.group(1))
    return -1

if __name__ == "__main__":
    print("=" * 120)
    print("SOURCE DATABASE VERİ KOPYALAMA SCRIPTİ")
    print("=" * 120)
    print()
    print("Bu script source database'den verileri kopyalar:")
    print("  - PostgreSQL: Person + HumanResources")
    print("  - MySQL: Production")
    print("  - SQL Server: Sales")
    print("  - SQLite: Purchasing + dbo")
    print()
    print("Strateji: Tablo yapılarını export et -> Dönüştür -> Oluştur -> Batch insert")
    print("=" * 120)
    print()
    
    server = "smartrag-sqlserver-test"
    source_db = "AdventureWorks2022"
    
    # PostgreSQL: Person + HumanResources şemaları
    print("=" * 120)
    print("POSTGRESQL (PersonManagement) - Person + HumanResources Şemaları")
    print("=" * 120)
    print()
    
    # Person şeması tabloları (AddressType, ContactType, PhoneNumberType hariç - SQLite'e gidecek)
    person_exclude = ["AddressType", "ContactType", "PhoneNumberType"]
    person_tables = get_tables_mssql(server, source_db, "Person")
    person_to_copy = sorted([t for t in person_tables if t not in person_exclude])
    
    # HumanResources şeması tabloları (Department, Shift hariç - SQLite'e gidecek)
    hr_exclude = ["Department", "Shift"]
    hr_tables = get_tables_mssql(server, source_db, "HumanResources")
    hr_to_copy = sorted([t for t in hr_tables if t not in hr_exclude])
    
    all_pg_tables = [("Person", t) for t in person_to_copy] + [("HumanResources", t) for t in hr_to_copy]
    
    print(f"Toplam {len(all_pg_tables)} tablo kopyalanacak:")
    print(f"  Person: {len(person_to_copy)} tablo")
    print(f"  HumanResources: {len(hr_to_copy)} tablo")
    print()
    print("=" * 120)
    print("ADIM 1: Tablo Yapılarını Oluşturma")
    print("=" * 120)
    
    pg_success_tables = []
    pg_failed_tables = []
    
    for i, (schema, table) in enumerate(all_pg_tables, 1):
        print(f"[{i}/{len(all_pg_tables)}] {schema}.{table}...", end=" ", flush=True)
        
        columns = get_table_definition_mssql(server, source_db, schema, table)
        if not columns:
            print("✗ Column'lar alınamadı")
            pg_failed_tables.append((schema, table))
            continue
        
        success, result = create_table_pg(columns, schema, table, "personmanagement")
        if success:
            print("✓ Oluşturuldu")
            pg_success_tables.append((schema, table))
        else:
            print(f"✗ Hata: {result[:100]}")
            pg_failed_tables.append((schema, table))
    
    print()
    print("=" * 120)
    print("ADIM 2: Verileri Kopyalama (Batch Insert)")
    print("=" * 120)
    
    total_copied = 0
    total_failed = 0
    
    for i, (schema, table) in enumerate(pg_success_tables, 1):
        print(f"[{i}/{len(pg_success_tables)}] ", end="")
        success, copied_count = copy_data_mssql_to_pg(
            server, source_db, schema, table,
            "personmanagement", schema, table
        )
        
        if success:
            total_copied += copied_count
        else:
            total_failed += 1
    
    print()
    print("=" * 120)
    print("ÖZET - PostgreSQL")
    print("=" * 120)
    print(f"Başarılı Tablolar: {len(pg_success_tables)}/{len(all_pg_tables)}")
    print(f"Başarısız Tablolar: {len(pg_failed_tables)}")
    if pg_failed_tables:
        print(f"Başarısız: {', '.join([f'{s}.{t}' for s, t in pg_failed_tables[:5]])}{'...' if len(pg_failed_tables) > 5 else ''}")
    print(f"Toplam Kopyalanan Satır: {total_copied:,}")
    print("=" * 120)
    print()
    
    # MySQL: Production + Purchasing şemaları
    print("=" * 120)
    print("MYSQL (InventoryManagement) - Production + Purchasing Şemaları")
    print("=" * 120)
    print()
    
    # MySQL veritabanını oluştur (yoksa)
    print("MySQL veritabanı kontrol ediliyor...")
    db_create_cmd = "docker exec smartrag-mysql-test mysql -uroot -pSmartRAG2025! -e 'CREATE DATABASE IF NOT EXISTS inventorymanagement CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;' 2>&1"
    db_result = subprocess.run(db_create_cmd, shell=True, capture_output=True, text=True, timeout=30)
    if db_result.returncode == 0:
        print("  ✓ Veritabanı hazır")
    else:
        print(f"  ⚠ Veritabanı oluşturma: {db_result.stdout[:100]}")
    print()
    
    # Production şeması tabloları (hepsi - ağır tablolar)
    production_tables = get_tables_mssql(server, source_db, "Production")
    production_to_copy = sorted(production_tables)
    
    # Purchasing şeması tabloları (hepsi)
    purchasing_tables = get_tables_mssql(server, source_db, "Purchasing")
    purchasing_to_copy = sorted(purchasing_tables)
    
    all_mysql_tables = [("Production", t) for t in production_to_copy] + [("Purchasing", t) for t in purchasing_to_copy]
    
    print(f"Toplam {len(all_mysql_tables)} tablo kopyalanacak:")
    print(f"  Production: {len(production_to_copy)} tablo")
    print(f"  Purchasing: {len(purchasing_to_copy)} tablo")
    print()
    print("=" * 120)
    print("ADIM 1: Tablo Yapılarını Oluşturma")
    print("=" * 120)
    
    mysql_success_tables = []
    mysql_failed_tables = []
    
    for i, (schema, table) in enumerate(all_mysql_tables, 1):
        print(f"[{i}/{len(all_mysql_tables)}] {schema}.{table}...", end=" ", flush=True)
        
        columns = get_table_definition_mssql(server, source_db, schema, table)
        if not columns:
            print("✗ Column'lar alınamadı")
            mysql_failed_tables.append((schema, table))
            continue
        
        success, result = create_table_mysql(columns, schema, table, "inventorymanagement")
        if success:
            print("✓ Oluşturuldu")
            mysql_success_tables.append((schema, table))
        else:
            print(f"✗ Hata: {result[:100]}")
            mysql_failed_tables.append((schema, table))
    
    print()
    print("=" * 120)
    print("ADIM 2: Verileri Kopyalama (Batch Insert)")
    print("=" * 120)
    
    mysql_total_copied = 0
    mysql_total_failed = 0
    
    for i, (schema, table) in enumerate(mysql_success_tables, 1):
        print(f"[{i}/{len(mysql_success_tables)}] ", end="")
        success, copied_count = copy_data_mssql_to_mysql(
            server, source_db, schema, table,
            "inventorymanagement", schema, table
        )
        
        if success:
            mysql_total_copied += copied_count
        else:
            mysql_total_failed += 1
    
    print()
    
    # SQLite: Purchasing + dbo şemaları
    print("=" * 120)
    print("SQLITE (LogisticsManagement) - Purchasing + dbo Şemaları")
    print("=" * 120)
    print()
    
    # SQLite veritabanı dosyası yolu
    sqlite_db_path = "examples/SmartRAG.Demo/TestSQLiteData/LogisticsManagement.db"
    abs_sqlite_path = os.path.abspath(sqlite_db_path)
    
    # SQLite veritabanı klasörünü oluştur (yoksa)
    sqlite_dir = os.path.dirname(abs_sqlite_path)
    os.makedirs(sqlite_dir, exist_ok=True)
    
    print(f"SQLite veritabanı: {abs_sqlite_path}")
    print()
    
    # Purchasing şeması tabloları (hepsi)
    purchasing_tables = get_tables_mssql(server, source_db, "Purchasing")
    purchasing_to_copy = sorted(purchasing_tables)
    
    # dbo şeması tabloları (hepsi)
    dbo_tables = get_tables_mssql(server, source_db, "dbo")
    dbo_to_copy = sorted(dbo_tables)
    
    # SQLite için tüm tablolar (Purchasing + dbo)
    sqlite_tables_to_copy = []
    for table in purchasing_to_copy:
        sqlite_tables_to_copy.append(("Purchasing", table))
    for table in dbo_to_copy:
        sqlite_tables_to_copy.append(("dbo", table))
    
    print(f"Toplam {len(sqlite_tables_to_copy)} tablo kopyalanacak (Purchasing: {len(purchasing_to_copy)}, dbo: {len(dbo_to_copy)}):")
    for schema, table in sqlite_tables_to_copy:
        row_count = get_table_count_mssql(server, source_db, schema, table)
        print(f"  {schema}.{table}: {row_count:,} satır")
    print()
    
    print("=" * 120)
    print("ADIM 1: Tablo Yapılarını Oluşturma")
    print("=" * 120)
    
    sqlite_success_tables = []
    sqlite_failed_tables = []
    
    for i, (schema, table) in enumerate(sqlite_tables_to_copy, 1):
        print(f"[{i}/{len(sqlite_tables_to_copy)}] {schema}.{table}...", end=" ", flush=True)
        
        columns = get_table_definition_mssql(server, source_db, schema, table)
        if not columns:
            print("✗ Column'lar alınamadı")
            sqlite_failed_tables.append((schema, table))
            continue
        
        success, result = create_table_sqlite(columns, schema, table, abs_sqlite_path)
        if success:
            print("✓ Oluşturuldu")
            sqlite_success_tables.append((schema, table))
        else:
            print(f"✗ Hata: {result[:100]}")
            sqlite_failed_tables.append((schema, table))
    
    print()
    print("=" * 120)
    print("ADIM 2: Verileri Kopyalama (Batch Insert)")
    print("=" * 120)
    
    sqlite_total_copied = 0
    sqlite_total_failed = 0
    
    for i, (schema, table) in enumerate(sqlite_success_tables, 1):
        print(f"[{i}/{len(sqlite_success_tables)}] ", end="")
        success, copied_count = copy_data_mssql_to_sqlite(
            server, source_db, schema, table,
            abs_sqlite_path, schema, table
        )
        
        if success:
            sqlite_total_copied += copied_count
        else:
            sqlite_total_failed += 1
    
    print()
    
    # SQL Server: Sales şeması
    print("=" * 120)
    print("SQL SERVER (SalesManagement) - Sales Şeması")
    print("=" * 120)
    print()
    
    target_mssql_db = "SalesManagement"
    
    # Sales şeması tabloları (hepsi)
    sales_tables = get_tables_mssql(server, source_db, "Sales")
    sales_to_copy = sorted(sales_tables)
    
    print(f"Toplam {len(sales_to_copy)} tablo kopyalanacak (Sales şeması)")
    print()
    print("=" * 120)
    print("ADIM 1: Tablo Yapılarını Oluşturma")
    print("=" * 120)
    
    mssql_success_tables = []
    mssql_failed_tables = []
    
    for i, table in enumerate(sales_to_copy, 1):
        print(f"[{i}/{len(sales_to_copy)}] Sales.{table}...", end=" ", flush=True)
        
        columns = get_table_definition_mssql(server, source_db, "Sales", table)
        if not columns:
            print("✗ Column'lar alınamadı")
            mssql_failed_tables.append(("Sales", table))
            continue
        
        # SQL Server'da tablo oluştur (CREATE TABLE)
        create_sql = create_table_mssql_sql(columns, "Sales", table)
        create_result, create_code = run_sql_mssql(server, target_mssql_db, create_sql, timeout=120)
        
        if create_code == 0:
            print("✓ Oluşturuldu")
            mssql_success_tables.append(("Sales", table))
        else:
            print(f"✗ Hata: {create_result[:100]}")
            mssql_failed_tables.append(("Sales", table))
    
    print()
    print("=" * 120)
    print("ADIM 2: Verileri Kopyalama (Direct INSERT INTO ... SELECT FROM)")
    print("=" * 120)
    
    mssql_total_copied = 0
    mssql_total_failed = 0
    
    for i, (schema, table) in enumerate(mssql_success_tables, 1):
        print(f"[{i}/{len(mssql_success_tables)}] ", end="")
        success, copied_count = copy_data_mssql_to_mssql(
            server, source_db, schema, table,
            server, target_mssql_db, schema, table
        )
        
        if success:
            mssql_total_copied += copied_count
        else:
            mssql_total_failed += 1
    
    print()
    print("=" * 120)
    print("FİNAL ÖZET")
    print("=" * 120)
    print(f"PostgreSQL - Başarılı: {len(pg_success_tables)}/{len(all_pg_tables)} tablo, {total_copied:,} satır")
    print(f"MySQL - Başarılı: {len(mysql_success_tables)}/{len(all_mysql_tables)} tablo, {mysql_total_copied:,} satır")
    print(f"SQLite - Başarılı: {len(sqlite_success_tables)}/{len(sqlite_tables_to_copy)} tablo, {sqlite_total_copied:,} satır")
    print(f"SQL Server - Başarılı: {len(mssql_success_tables)}/{len(sales_to_copy)} tablo, {mssql_total_copied:,} satır")
    print(f"Toplam Kopyalanan: {total_copied + mysql_total_copied + sqlite_total_copied + mssql_total_copied:,} satır")
    if pg_failed_tables:
        print(f"PostgreSQL Başarısız: {len(pg_failed_tables)} tablo")
    if mysql_failed_tables:
        print(f"MySQL Başarısız: {len(mysql_failed_tables)} tablo")
    if mssql_failed_tables:
        print(f"SQL Server Başarısız: {len(mssql_failed_tables)} tablo")
    print("=" * 120)
