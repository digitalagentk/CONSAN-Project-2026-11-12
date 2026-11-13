# =============================================================
# CONSAN® - Database Backup Script
# =============================================================

param(
    [string]$ProjectPath = "C:\Users\Orbit\RiderProjects\text_2\text_2",
    [string]$ContainerName = "consan_sqlserver",
    [string]$DatabaseName = "ConsanDb",
    [int]$RetentionDays = 7
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyy-MM-dd_HHmm"
$backupFileName = "${DatabaseName}_${timestamp}.bak"
$containerBackupPath = "/var/opt/mssql/backups/${backupFileName}"
$hostBackupDir = Join-Path $ProjectPath "backups"
$hostBackupPath = Join-Path $hostBackupDir $backupFileName
$logFile = Join-Path $ProjectPath "logs\backup.log"

if (-not (Test-Path $hostBackupDir)) { New-Item -ItemType Directory -Path $hostBackupDir -Force | Out-Null }
if (-not (Test-Path (Split-Path $logFile))) { New-Item -ItemType Directory -Path (Split-Path $logFile) -Force | Out-Null }

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Level] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

Write-Log "========== Backup started =========="

try {
    $containerStatus = docker inspect --format='{{.State.Status}}' $ContainerName 2>$null
    if ($containerStatus -ne "running") { throw "Container '$ContainerName' is not running (status: $containerStatus)" }
    Write-Log "Container '$ContainerName' is running."
    # --- Ensure backup directory exists inside container ---
    docker exec $ContainerName mkdir -p /var/opt/mssql/backups | Out-Null
    Write-Log "Backup directory ensured inside container."

    $saPassword = docker exec $ContainerName printenv MSSQL_SA_PASSWORD
    if ([string]::IsNullOrWhiteSpace($saPassword)) { throw "Could not read MSSQL_SA_PASSWORD from container." }
    Write-Log "SA password retrieved."

    Write-Log "Running BACKUP DATABASE $DatabaseName ..."
    $backupQuery = "BACKUP DATABASE [$DatabaseName] TO DISK = N'$containerBackupPath' WITH INIT, NAME = N'$DatabaseName-Full Backup';"

    $sqlOutput = docker exec $ContainerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $saPassword -C -Q $backupQuery 2>&1
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE. Output: $sqlOutput" }
    Write-Log "BACKUP DATABASE completed inside container."

    $fileCheck = docker exec $ContainerName test -f $containerBackupPath
    if ($LASTEXITCODE -ne 0) { throw "Backup file not found inside container at $containerBackupPath" }
    Write-Log "Backup file verified inside container."

    Write-Log "Copying backup to host: $hostBackupPath"
    docker cp "${ContainerName}:${containerBackupPath}" $hostBackupPath
    if ($LASTEXITCODE -ne 0) { throw "docker cp failed with exit code $LASTEXITCODE" }

    if (-not (Test-Path $hostBackupPath)) { throw "Backup file not found on host after copy." }
    $hostFileSize = (Get-Item $hostBackupPath).Length
    $hostFileSizeMB = [math]::Round($hostFileSize / 1MB, 2)
    Write-Log "Backup saved on host. Size: $hostFileSizeMB MB"

    docker exec $ContainerName rm -f $containerBackupPath | Out-Null
    Write-Log "Removed .bak from container (host copy retained)."

    $cutoff = (Get-Date).AddDays(-$RetentionDays)
    $oldBackups = Get-ChildItem -Path $hostBackupDir -Filter "*.bak" | Where-Object { $_.LastWriteTime -lt $cutoff }
    if ($oldBackups.Count -gt 0) {
        foreach ($file in $oldBackups) { Remove-Item $file.FullName -Force; Write-Log "Deleted old backup: $($file.Name)" "CLEANUP" }
    } else { Write-Log "No old backups to delete." "CLEANUP" }

    $remainingCount = (Get-ChildItem -Path $hostBackupDir -Filter "*.bak").Count
    Write-Log "Backup SUCCESS. Total backups on disk: $remainingCount"
    Write-Log "========== Backup finished =========="
    exit 0
}
catch {
    Write-Log "Backup FAILED: $_" "ERROR"
    Write-Log "========== Backup aborted =========="
    exit 1
}
