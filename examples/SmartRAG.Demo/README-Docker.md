# Docker Setup Guide for SmartRAG Demo

Complete guide for running SmartRAG with all local services using Docker.

## 📦 What Gets Installed

This Docker setup includes **6 services**:

### AI & Storage
- **Ollama** - Local AI models (LLaMA, Phi, Mistral, etc.)
- **Qdrant** - Vector database for embeddings
- **Redis** - Cache and vector storage

### Databases
- **SQL Server 2022 Express** - SalesManagement database
- **MySQL 8.0** - InventoryManagement database
- **PostgreSQL 16** - LogisticsManagement database

## 🚀 Quick Start

### 1. Start All Services

```bash
cd examples/SmartRAG.LocalDemo
docker-compose up -d
```

**Wait 10-15 seconds** for all services to initialize.

### 2. Verify Services

```bash
docker-compose ps
```

You should see all 6 containers running with "healthy" status.

### 3. Setup Ollama Models

```bash
# Download LLaMA 3.2 (main AI model)
docker exec -it smartrag-ollama ollama pull llama3.2

# Download embedding model (required for RAG)
docker exec -it smartrag-ollama ollama pull nomic-embed-text
```

**Note**: First download may take 5-10 minutes depending on your internet speed.

### 4. Run the Application

```bash
dotnet run
```

Select **"1. LOCAL Environment"** when prompted.

## 🔧 Individual Service Management

### Start Individual Services

```bash
# AI Services
docker-compose up -d ollama
docker-compose up -d qdrant
docker-compose up -d redis

# Databases
docker-compose up -d sqlserver
docker-compose up -d mysql
docker-compose up -d postgres
```

### Stop Services

```bash
# Stop all
docker-compose stop

# Stop specific service
docker-compose stop ollama
```

### Restart Services

```bash
# Restart all
docker-compose restart

# Restart specific
docker-compose restart qdrant
```

### View Logs

```bash
# All services
docker-compose logs

# Specific service
docker-compose logs ollama
docker-compose logs qdrant
docker-compose logs redis

# Follow logs (real-time)
docker-compose logs -f ollama
```

## 📊 Connection Details

### Ollama (AI)
- **Endpoint**: http://localhost:11434
- **API**: http://localhost:11434/api/tags
- **Container**: smartrag-ollama
- **Volume**: ollama-data

**Test Connection:**
```bash
curl http://localhost:11434/api/tags
```

### Qdrant (Vector Database)
- **HTTP**: http://localhost:6333
- **gRPC**: localhost:6334
- **Dashboard**: http://localhost:6333/dashboard
- **Container**: smartrag-qdrant
- **Volume**: qdrant-data

**Test Connection:**
```bash
curl http://localhost:6333/health
```

### Redis (Cache)
- **Host**: localhost:6379
- **Container**: smartrag-redis
- **Volume**: redis-data
- **Persistence**: AOF (Append Only File) enabled

**Test Connection:**
```bash
docker exec -it smartrag-redis redis-cli ping
# Should return: PONG
```

### SQL Server
- **Host**: localhost,1433
- **Username**: sa
- **Password**: SmartRAG@2024
- **Database**: SalesManagement (created by app)
- **Container**: smartrag-sqlserver-test
- **Volume**: sqlserver-data

**Test Connection:**
```bash
docker exec -it smartrag-sqlserver-test /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P SmartRAG@2024 -C -Q "SELECT @@VERSION"
```

### MySQL
- **Host**: localhost:3306
- **Username**: root
- **Password**: mysql123
- **Database**: InventoryManagement (created by app)
- **Container**: smartrag-mysql-test
- **Volume**: mysql-data

**Test Connection:**
```bash
docker exec -it smartrag-mysql-test mysql -u root -pmysql123 -e "SELECT VERSION()"
```

### PostgreSQL
- **Host**: localhost:5432
- **Username**: postgres
- **Password**: postgres123
- **Database**: LogisticsManagement (created by app)
- **Container**: smartrag-postgres-test
- **Volume**: postgres-data

**Test Connection:**
```bash
docker exec -it smartrag-postgres-test psql -U postgres -c "SELECT version()"
```

## 🤖 Ollama Model Management

### List Installed Models

```bash
docker exec -it smartrag-ollama ollama list
```

### Pull/Download Models

```bash
# Recommended models
docker exec -it smartrag-ollama ollama pull llama3.2        # Main AI (3.2GB)
docker exec -it smartrag-ollama ollama pull llama3.2:1b     # Lightweight (1.3GB)
docker exec -it smartrag-ollama ollama pull nomic-embed-text # Embeddings (274MB)
docker exec -it smartrag-ollama ollama pull phi3            # Microsoft Phi (2.3GB)
docker exec -it smartrag-ollama ollama pull mistral         # Mistral 7B (4.1GB)
docker exec -it smartrag-ollama ollama pull qwen2.5         # Alibaba Qwen (4.7GB)
```

### Remove Models

```bash
docker exec -it smartrag-ollama ollama rm llama3.2
```

### Check Model Info

```bash
docker exec -it smartrag-ollama ollama show llama3.2
```

## 💾 Data Persistence

All data is stored in Docker volumes and persists across container restarts.

### List Volumes

```bash
docker volume ls | grep smartrag
```

### Inspect Volume

```bash
docker volume inspect smartrag-localdemo_ollama-data
docker volume inspect smartrag-localdemo_qdrant-data
docker volume inspect smartrag-localdemo_redis-data
```

### Backup Data

```bash
# Backup Ollama models
docker run --rm -v smartrag-localdemo_ollama-data:/data -v $(pwd):/backup alpine tar czf /backup/ollama-backup.tar.gz -C /data .

# Backup Qdrant
docker run --rm -v smartrag-localdemo_qdrant-data:/data -v $(pwd):/backup alpine tar czf /backup/qdrant-backup.tar.gz -C /data .
```

### Restore Data

```bash
# Restore Ollama
docker run --rm -v smartrag-localdemo_ollama-data:/data -v $(pwd):/backup alpine sh -c "cd /data && tar xzf /backup/ollama-backup.tar.gz"
```

## 🧹 Cleanup

### Remove Containers (Keep Data)

```bash
docker-compose down
```

### Remove Everything (⚠️ Including Data)

```bash
docker-compose down -v
```

### Remove Specific Volume

```bash
docker volume rm smartrag-localdemo_ollama-data
```

### Clean Up Unused Docker Resources

```bash
docker system prune -a
```

## 🔧 Troubleshooting

### Port Already in Use

If a port is occupied, change it in `docker-compose.yml`:

```yaml
# Example: Change Ollama port from 11434 to 11435
ollama:
  ports:
    - "11435:11434"  # Host:Container
```

Then update `appsettings.Development.json`:
```json
{
  "AI": {
    "Custom": {
      "Endpoint": "http://localhost:11435"
    }
  }
}
```

### Container Won't Start

```bash
# Check Docker is running
docker --version

# Check container logs
docker logs smartrag-ollama

# Restart Docker Desktop
# (Sometimes needed on Windows)
```

### Out of Disk Space

```bash
# Check Docker disk usage
docker system df

# Remove unused images
docker image prune -a

# Remove unused volumes
docker volume prune
```

### Health Check Failing

```bash
# Check health status
docker inspect smartrag-ollama | grep -A 10 Health

# Manually test health endpoint
curl http://localhost:11434/api/tags   # Ollama
curl http://localhost:6333/health      # Qdrant
docker exec -it smartrag-redis redis-cli ping  # Redis
```

### Ollama Model Download Stuck

```bash
# Check Ollama logs
docker logs -f smartrag-ollama

# Restart Ollama
docker-compose restart ollama

# Try pulling again
docker exec -it smartrag-ollama ollama pull llama3.2
```

## 📊 Resource Usage

### Typical Resource Consumption

| Service | RAM | Disk |
|---------|-----|------|
| Ollama (idle) | ~200MB | ~500MB base |
| Ollama + llama3.2 | ~2-4GB | ~3.2GB |
| Ollama + multiple models | ~4-8GB | ~10-15GB |
| Qdrant | ~100MB | ~50MB + data |
| Redis | ~50MB | ~10MB + cache |
| SQL Server | ~2GB | ~500MB |
| MySQL | ~300MB | ~200MB |
| PostgreSQL | ~50MB | ~100MB |

**Recommended System:**
- **RAM**: 8GB minimum, 16GB recommended
- **Disk**: 20GB free space
- **CPU**: 4+ cores recommended for AI inference

### Monitor Resource Usage

```bash
# Real-time stats
docker stats

# Check specific container
docker stats smartrag-ollama
```

## 🔒 Security Notes

⚠️ **This setup is for local development/testing only!**

- Default passwords are intentionally simple
- Services are exposed on localhost only
- **NEVER use these configurations in production**
- **NEVER expose these ports to the internet**

For production deployments:
- Use strong, unique passwords
- Enable TLS/SSL encryption
- Use proper authentication
- Run behind a firewall
- Use Docker secrets for sensitive data

## 🌐 Alternative: Manual Installation

If you prefer not to use Docker:

### Ollama
- Download from: https://ollama.ai
- Install locally
- Models stored in: `~/.ollama/models`

### Qdrant
- Download from: https://qdrant.tech/documentation/guides/installation/
- Or use Qdrant Cloud: https://cloud.qdrant.io

### Redis
- Windows: https://github.com/microsoftarchive/redis/releases
- Linux: `sudo apt install redis-server`
- macOS: `brew install redis`

## 📚 Additional Resources

- **Ollama Documentation**: https://ollama.ai/docs
- **Qdrant Documentation**: https://qdrant.tech/documentation/
- **Redis Documentation**: https://redis.io/documentation
- **SmartRAG Documentation**: https://byerlikaya.github.io/SmartRAG/en/

## 🤝 Contact

For issues or questions:
- **GitHub**: https://github.com/byerlikaya/SmartRAG
- **LinkedIn**: https://www.linkedin.com/in/barisyerlikaya/
- **Email**: b.yerlikaya@outlook.com
