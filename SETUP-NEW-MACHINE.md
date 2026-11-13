# CONSAN - Setup on a New Machine

Step-by-step guide to deploy this project on a fresh Windows machine.

---

## Prerequisites

### Required

- Docker Desktop (latest) - https://www.docker.com/products/docker-desktop/
- Git (latest) - https://git-scm.com/downloads

### Optional

- .NET 10 SDK - for local development
- VS Code - for editing
- SQL Server Management Studio - for DB management
- PowerShell 7+ - for better scripting

---

## Step-by-Step Setup

### Step 1: Install Docker Desktop

1. Download from https://www.docker.com/products/docker-desktop/
2. Install with default settings
3. Restart your computer
4. Open Docker Desktop - wait for "Engine Running"
5. Verify:

    docker --version
    docker ps

### Step 2: Clone the Project

    cd C:\
    git clone <repository-url> text_2
    cd text_2

### Step 3: Create .env

    Copy-Item .env.example .env

Open .env and set real values:

    MSSQL_SA_PASSWORD=YourStrong!Passw0rd
    MSSQL_APP_PASSWORD=App!Passw0rd2026
    ADMIN_PASSWORD=AdminPass2026!X

    CONNECTION_STRING=Server=consan_sqlserver,1433;Database=ConsanDb;User Id=consan_app;Password=App!Passw0rd2026;TrustServerCertificate=True;MultipleActiveResultSets=true
    MIGRATION_CONNECTION_STRING=Server=consan_sqlserver,1433;Database=ConsanDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true

    ASPNETCORE_URLS=http://0.0.0.0:8080
    ASPNETCORE_ENVIRONMENT=Production

Password rules:
- Minimum 8 characters
- Must contain: uppercase, lowercase, digit, symbol
- Avoid: semicolons (;), braces ({}) - they break connection strings

### Step 4: Start Containers

    docker compose up -d --build

Wait ~2 minutes.

### Step 5: Verify

    docker ps
    docker inspect --format='{{.State.Health.Status}}' consan_sqlserver
    Invoke-WebRequest http://localhost:8080 -UseBasicParsing | Select-Object StatusCode

### Step 6: Access

- Public site: http://localhost:8080
- Admin: http://localhost:8080/admin

---

## Optional: Setup Automatic Backups

Register the scheduled task:

    powershell -ExecutionPolicy Bypass -File .\setup-backup-task.ps1

Verify:

    Get-ScheduledTask -TaskName "ConsanDb-DailyBackup"
    Get-ScheduledTaskInfo -TaskName "ConsanDb-DailyBackup"

Test the backup:

    Start-ScheduledTask -TaskName "ConsanDb-DailyBackup"
    Start-Sleep -Seconds 15
    Get-ChildItem .\backups\

---

## Optional: Connect via SSMS

1. Open SSMS
2. Server name: localhost,1433
3. Authentication: SQL Server Authentication
4. Login: sa
5. Password: value from .env (MSSQL_SA_PASSWORD)
6. Options - Connection Properties:
   - Encrypt connection: No
   - Trust server certificate: Yes
7. Connect

---

## Common Issues

### Docker not running

Error: Cannot connect to the Docker daemon

Fix: Open Docker Desktop, wait for Engine Running, retry.

### Port 8080 in use

Edit docker-compose.yml:

    ports:
      - "8090:8080"

Then: docker compose up -d

### Port 1433 in use

Another SQL Server is running. Change to:

    ports:
      - "127.0.0.1:1434:1433"

### Login failed for user 'sa'

Password mismatch. Reset by:

    docker compose down -v
    docker compose up -d --build

Warning: this deletes all data.

### App crashes on startup

Check logs:

    docker compose logs --tail=100 consan_app

---

## Backup & Restore

### Automatic

Daily at 2:00 AM to ./backups/

### Manual

    powershell -ExecutionPolicy Bypass -File .\scripts\backup-db.ps1

### Restore

    $saPass = docker exec consan_sqlserver printenv MSSQL_SA_PASSWORD
    $backupFile = "ConsanDb_2026-10-09_2213.bak"

    docker cp ".\backups\$backupFile" consan_sqlserver:/var/opt/mssql/backups/

    docker exec consan_sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $saPass -C -Q "RESTORE DATABASE [ConsanDb_Restored] FROM DISK = N'/var/opt/mssql/backups/$backupFile' WITH MOVE 'ConsanDb' TO '/var/opt/mssql/data/ConsanDb_Restored.mdf', MOVE 'ConsanDb_log' TO '/var/opt/mssql/data/ConsanDb_Restored_log.ldf', RECOVERY;"

---

## Health Check

    powershell -ExecutionPolicy Bypass -File .\check-health.ps1

---

## Checklist

- [ ] Docker Desktop installed and running
- [ ] Project cloned
- [ ] .env created with real values
- [ ] docker compose up -d --build succeeded
- [ ] http://localhost:8080 loads
- [ ] /admin login works
- [ ] SSMS connects (optional)
- [ ] Backup task registered (optional)
- [ ] Health check passes

---

Copyright 2026 CONSAN. All rights reserved.
