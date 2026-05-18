# Hub Deployment Guide

This guide covers deploying the EdgeGuard Platform Hub component in Docker and direct .NET (bare-metal/VM) scenarios.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Docker Compose Deployment](#docker-compose-deployment)
3. [Direct .NET Deployment](#direct-net-deployment)
4. [Nginx Reverse Proxy](#nginx-reverse-proxy)
5. [Environment Variables Reference](#environment-variables-reference)
6. [First-Run Bootstrap](#first-run-bootstrap)
7. [Database Migrations (Auto-applied)](#database-migrations-auto-applied)
8. [Production Checklist](#production-checklist)

---

## Prerequisites

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| .NET Runtime | 10.0 | 10.0 (latest patch) |
| PostgreSQL | 15 | 16 |
| RAM | 2 GB | 4 GB |
| CPU | 2 cores | 4 cores |
| Disk | 20 GB | 100 GB (logs + temp DICOM) |

> **Note:** The Hub does not store permanent DICOM pixel data. Disk is primarily for structured logs and temporary routing buffers.

---

## Docker Compose Deployment

### docker-compose.yml

```yaml
version: "3.9"

services:
  postgres:
    image: postgres:16-alpine
    container_name: edgeguard-postgres
    restart: unless-stopped
    environment:
      POSTGRES_USER: edgeguard
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: edgeguard_hub
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U edgeguard -d edgeguard_hub"]
      interval: 10s
      timeout: 5s
      retries: 5

  hub:
    image: edgeguard/hub:latest
    container_name: edgeguard-hub
    restart: unless-stopped
    depends_on:
      postgres:
        condition: service_healthy
    environment:
      HUB_DB_CONNECTION_STRING: "Host=postgres;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=${POSTGRES_PASSWORD};SSL Mode=Disable"
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: "http://+:5000"
      Jwt__Secret: ${JWT_SECRET}
      Jwt__Issuer: "https://your-hub-domain.example.com"
      Jwt__Audience: "edgeguard-clients"
      Jwt__ExpiryMinutes: "60"
      CorsOrigins__0: "https://your-hub-domain.example.com"
      Diagnostics__PHIRedaction: "Strict"
      Diagnostics__Seq__Enabled: "false"
    ports:
      - "5000:5000"
      - "8001:8001"     # HL7 MLLP
    volumes:
      - hub_logs:/app/logs
      - hub_data:/app/data
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  postgres_data:
  hub_logs:
  hub_data:
```

### .env file

```env
POSTGRES_PASSWORD=change_me_strong_password
JWT_SECRET=change_me_at_least_32_chars_long_secret_key
```

### Start and verify

```bash
# Start services
docker compose up -d

# Check logs
docker compose logs -f hub

# Verify health
curl http://localhost:5000/health
```

---

## Direct .NET Deployment

### Publish the application

```bash
dotnet publish src/backend/Dicom.Edge.Hub.Api \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained false \
  --output /opt/edgeguard/hub
```

### systemd unit file

Create `/etc/systemd/system/edgeguard-hub.service`:

```ini
[Unit]
Description=EdgeGuard Platform Hub
After=network.target postgresql.service
Requires=postgresql.service

[Service]
Type=notify
User=edgeguard
Group=edgeguard
WorkingDirectory=/opt/edgeguard/hub
ExecStart=/usr/bin/dotnet /opt/edgeguard/hub/Dicom.Edge.Hub.Api.dll

# Environment
EnvironmentFile=/etc/edgeguard/hub.env
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

# Restart policy
Restart=on-failure
RestartSec=10
KillMode=mixed
KillSignal=SIGTERM
TimeoutStopSec=30

# Security hardening
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ReadWritePaths=/opt/edgeguard/hub/logs /opt/edgeguard/hub/data

[Install]
WantedBy=multi-user.target
```

### /etc/edgeguard/hub.env

```env
HUB_DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=strong_password
Jwt__Secret=change_me_at_least_32_chars_long_secret_key
Jwt__Issuer=https://your-hub-domain.example.com
Jwt__Audience=edgeguard-clients
Diagnostics__PHIRedaction=Strict
```

### Enable and start

```bash
sudo systemctl daemon-reload
sudo systemctl enable edgeguard-hub
sudo systemctl start edgeguard-hub
sudo systemctl status edgeguard-hub
```

---

## Nginx Reverse Proxy

The Hub uses SignalR for real-time updates. WebSocket support is required in the Nginx configuration.

```nginx
upstream edgeguard_hub {
    server 127.0.0.1:5000;
    keepalive 32;
}

server {
    listen 80;
    server_name your-hub-domain.example.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-hub-domain.example.com;

    ssl_certificate     /etc/ssl/certs/edgeguard.crt;
    ssl_certificate_key /etc/ssl/private/edgeguard.key;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # HSTS
    add_header Strict-Transport-Security "max-age=63072000; includeSubdomains" always;

    client_max_body_size 512M;

    location / {
        proxy_pass         http://edgeguard_hub;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_read_timeout 300s;
        proxy_send_timeout 300s;
    }

    # SignalR WebSocket support
    location /hubs/ {
        proxy_pass         http://edgeguard_hub;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade    $http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_set_header   Host       $host;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 86400s;   # Keep WS connections alive
    }
}
```

---

## Environment Variables Reference

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `HUB_DB_CONNECTION_STRING` | Yes | — | PostgreSQL connection string |
| `ASPNETCORE_ENVIRONMENT` | Yes | `Production` | ASP.NET Core environment name |
| `ASPNETCORE_URLS` | No | `http://+:5000` | Kestrel listen addresses |
| `Jwt__Secret` | Yes | — | JWT signing secret (min 32 chars) |
| `Jwt__Issuer` | Yes | — | JWT issuer claim |
| `Jwt__Audience` | Yes | — | JWT audience claim |
| `Jwt__ExpiryMinutes` | No | `60` | Token expiry in minutes |
| `CorsOrigins__0` | Yes | — | Allowed CORS origin(s) (index-based) |
| `Diagnostics__PHIRedaction` | No | `Relaxed` | `Strict` or `Relaxed` |
| `Diagnostics__Seq__Enabled` | No | `false` | Enable Seq log sink |
| `Diagnostics__Seq__Url` | No | — | Seq server URL |
| `Diagnostics__Seq__ApiKey` | No | — | Seq API key |

---

## First-Run Bootstrap

On first startup with an empty database, the Hub generates a bootstrap admin token and logs it to the console and log file:

```
[INF] ========================================================
[INF] BOOTSTRAP TOKEN (one-time use):
[INF] eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
[INF] Use this token to create the initial admin account.
[INF] ========================================================
```

**Steps to create the first admin user:**

1. Copy the bootstrap token from the log.
2. Call the bootstrap endpoint:

```bash
curl -X POST https://your-hub-domain.example.com/api/auth/bootstrap \
  -H "Authorization: Bearer <bootstrap_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "StrongPassword123!",
    "email": "admin@your-org.example.com"
  }'
```

3. The bootstrap token is invalidated after first use. Log in normally with the created credentials.

> **Warning:** The bootstrap token appears only once. If missed, restart the Hub with an empty database or use a direct DB seed script (contact support).

---

## Database Migrations (Auto-applied)

The Hub automatically applies pending EF Core migrations at startup via `app.Services.MigrateHubAsync()`. **No manual migration step is required** for standard deployments.

Migration output appears in startup logs:

```
[INF] Applying database migrations...
[INF] Migration 'InitialMigrationHub' already applied.
[INF] Applying migration 'AddNodeTelemetryRecords'...
[INF] All migrations applied successfully.
```

For manual migration control, see [database-migrations.md](./database-migrations.md).

---

## Production Checklist

Before going live, verify each item:

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] HTTPS enforced via reverse proxy (TLS 1.2+)
- [ ] `Jwt__Secret` is at least 32 characters, randomly generated, stored in secrets manager
- [ ] `CorsOrigins` restricted to your actual frontend domain(s)
- [ ] `Diagnostics__PHIRedaction=Strict`
- [ ] PostgreSQL user has minimal required privileges (no superuser)
- [ ] PostgreSQL not exposed on public network interface
- [ ] Log directory is writable but not world-readable
- [ ] Health check `/health` returns `Healthy` before directing traffic
- [ ] Backup schedule configured (see [backup-recovery.md](./backup-recovery.md))
- [ ] Rate limiting reviewed for expected load
- [ ] HL7 MLLP port (8001) accessible only from HIS/RIS network segment
- [ ] Monitoring/alerting configured (see [monitoring.md](./monitoring.md))
