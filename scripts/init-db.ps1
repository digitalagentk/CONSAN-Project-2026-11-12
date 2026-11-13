# ============================================================
# init-db.ps1 — ينشئ مستخدم consan_app في قاعدة البيانات
# ============================================================
$ErrorActionPreference = "Stop"
Write-Host "Initializing database users..." -ForegroundColor Cyan

# انتظر SQL Server
Write-Host "Waiting for SQL Server..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0

do {
    $attempt++
    docker exec consan_sqlserver /opt/mssql-tools18/bin/sqlcmd `
        -S localhost -U sa -P $env:MSSQL_SA_PASSWORD -C `
        -Q "SELECT 1" 2>$null | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "SQL Server is ready" -ForegroundColor Green
        break
    }
    Write-Host "   Attempt $attempt/$maxAttempts..."
    Start-Sleep -Seconds 3
} while ($attempt -lt $maxAttempts)

if ($attempt -ge $maxAttempts) { throw "SQL Server failed to start" }

# قراءة سكربت SQL
$sqlScript = Get-Content "db-init\01-init-app-user.sql" -Raw
$sqlScript = $sqlScript.Replace('$(APP_PASSWORD)', $env:MSSQL_APP_PASSWORD)

# نسخ السكربت للحاوية
$sqlScript | Out-File -FilePath "db-init\_temp_init.sql" -Encoding UTF8
docker cp "db-init\_temp_init.sql" "consan_sqlserver:/tmp/init.sql"
Remove-Item "db-init\_temp_init.sql" -Force

# تنفيذ
Write-Host "Running init script..." -ForegroundColor Yellow
docker exec consan_sqlserver /opt/mssql-tools18/bin/sqlcmd `
    -S localhost -U sa -P $env:MSSQL_SA_PASSWORD -C `
    -i /tmp/init.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "Database users initialized successfully" -ForegroundColor Green
} else {
    throw "Failed to init database"
}