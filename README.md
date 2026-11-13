# CONSAN - Middle Euphrates Workshop

Full-stack ASP.NET Core 10 web app + admin dashboard, packaged in Docker with SQL Server 2022.

---

## Quick Start (5 minutes)

### 1. Prerequisites

- Docker Desktop - https://www.docker.com/products/docker-desktop/
- PowerShell 7+ (optional) - https://aka.ms/powershell
- SQL Server Management Studio (optional) - https://aka.ms/ssms

### 2. Clone the Repository

    git clone <repository-url>
    cd text_2

### 3. Create .env File

    Copy-Item .env.example .env
    notepad .env

Fill in real values.

### 4. Start Everything

    docker compose up -d --build

Wait ~2 minutes.

### 5. Access the App

| URL | Purpose |
|---|---|
| http://localhost:8080/ | Public website |
| http://localhost:8080/admin | Admin dashboard |
| http://localhost:8080/industrial-services | Industrial services |
| http://localhost:8080/explorer | Explorer |
| http://localhost:8080/guide | Comprehensive guide |

---

## Admin Dashboard

- URL: http://localhost:8080/admin
- Password: value of ADMIN_PASSWORD in .env

---

## Database Access (SSMS)

| Setting | Value |
|---|---|
| Server name | localhost,1433 |
| Authentication | SQL Server Authentication |
| Login | sa |
| Password | value of MSSQL_SA_PASSWORD in .env |
| Database | ConsanDb |
| Encrypt connection | Optional |
| Trust server certificate | Yes |

---

## Backup

Automatic: Daily at 2:00 AM. Location: ./backups/. Retention: 7 days.

Manual:

    powershell -ExecutionPolicy Bypass -File .\scripts\backup-db.ps1

---

## Common Commands

    docker compose up -d
    docker compose down
    docker compose up -d --build consan_app
    docker compose logs -f consan_app
    docker compose logs -f consan_sqlserver
    docker ps

---

## Troubleshooting

App won't start:

    docker compose logs --tail=100 consan_app

SQL Server not healthy:

    docker compose logs --tail=100 consan_sqlserver
    docker inspect --format='{{.State.Health.Status}}' consan_sqlserver

Port 8080 in use: edit docker-compose.yml and change 8080:8080 to 8090:8080.

Reset everything (destroys data!):

    docker compose down -v
    docker compose up -d --build

---

## Health Check

    powershell -ExecutionPolicy Bypass -File .\check-health.ps1

---

Copyright 2026 CONSAN. All rights reserved.
