# CONSAN - Health Check Script

$ErrorActionPreference = "SilentlyContinue"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  CONSAN - Health Check" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$allOk = $true

# 1. Containers
Write-Host "Containers:" -ForegroundColor Yellow
$containers = docker ps --format "{{.Names}}|{{.Status}}"

if ($containers -match "consan_app") {
    $status = ($containers | Select-String "consan_app").ToString().Split("|")[1]
    Write-Host "   OK   consan_app        -> $status" -ForegroundColor Green
} else {
    Write-Host "   ERR  consan_app        -> NOT RUNNING" -ForegroundColor Red
    $allOk = $false
}

if ($containers -match "consan_sqlserver") {
    $status = ($containers | Select-String "consan_sqlserver").ToString().Split("|")[1]
    Write-Host "   OK   consan_sqlserver  -> $status" -ForegroundColor Green
} else {
    Write-Host "   ERR  consan_sqlserver  -> NOT RUNNING" -ForegroundColor Red
    $allOk = $false
}
Write-Host ""

# 2. SQL Server Health
Write-Host "Database:" -ForegroundColor Yellow
$health = docker inspect --format='{{.State.Health.Status}}' consan_sqlserver 2>$null
if ($health -eq "healthy") {
    Write-Host "   OK   SQL Server healthy" -ForegroundColor Green
} else {
    Write-Host "   ERR  SQL Server health: $health" -ForegroundColor Red
    $allOk = $false
}
Write-Host ""

# 3. Public APIs
Write-Host "Public APIs:" -ForegroundColor Yellow
$apis = @("/", "/api/content", "/api/map-pins", "/api/industrial-content", "/admin")

foreach ($path in $apis) {
    try {
        $r = Invoke-WebRequest "http://localhost:8080$path" -UseBasicParsing -TimeoutSec 5
        Write-Host "   OK   GET $path -> $($r.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "   ERR  GET $path -> FAILED" -ForegroundColor Red
        $allOk = $false
    }
}
Write-Host ""

# 4. Backups
Write-Host "Backups:" -ForegroundColor Yellow
$backups = Get-ChildItem ".\backups\*.bak" -ErrorAction SilentlyContinue
if ($backups) {
    $latest = $backups | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $sizeMB = [math]::Round($latest.Length / 1MB, 2)
    Write-Host "   OK   $($backups.Count) backup(s) found" -ForegroundColor Green
    Write-Host "        Latest: $($latest.Name) ($sizeMB MB)" -ForegroundColor Cyan
} else {
    Write-Host "   WARN No backups found" -ForegroundColor Yellow
}
Write-Host ""

# 5. Scheduled Task
Write-Host "Scheduled Backup Task:" -ForegroundColor Yellow
$task = Get-ScheduledTask -TaskName "ConsanDb-DailyBackup" -ErrorAction SilentlyContinue
if ($task) {
    $info = Get-ScheduledTaskInfo -TaskName "ConsanDb-DailyBackup"
    Write-Host "   OK   Task exists - State: $($task.State)" -ForegroundColor Green
    Write-Host "        Next run: $($info.NextRunTime)" -ForegroundColor Cyan
} else {
    Write-Host "   WARN Task not registered" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "===============================================" -ForegroundColor Cyan
if ($allOk) {
    Write-Host "  ALL CHECKS PASSED" -ForegroundColor Green
} else {
    Write-Host "  SOME CHECKS FAILED" -ForegroundColor Red
}
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
