# =============================================================
# CONSAN® - Setup Daily Backup Task in Windows Task Scheduler
# =============================================================

$ProjectPath = "C:\Users\Orbit\RiderProjects\text_2\text_2"
$TaskName = "ConsanDb-DailyBackup"
$ScriptPath = Join-Path $ProjectPath "scripts\backup-db.ps1"
$LogFile = Join-Path $ProjectPath "logs\task-scheduler.log"

# --- Ensure paths exist ---
if (-not (Test-Path $ScriptPath)) {
    Write-Host "❌ Backup script not found: $ScriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "🔧 Setting up task: $TaskName" -ForegroundColor Cyan
Write-Host "   Script: $ScriptPath"
Write-Host "   Time: 02:00 (every day)"

# --- Remove old task if exists ---
$existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existingTask) {
    Write-Host "⚠️  Removing existing task..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}

# --- Create action ---
$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File `"$ScriptPath`"" `
    -WorkingDirectory $ProjectPath

# --- Create trigger: daily at 02:00 ---
$trigger = New-ScheduledTaskTrigger -Daily -At 2:00AM

# --- Create settings ---
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -RunOnlyIfNetworkAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Hours 1)

# --- Create principal (run as current user, whether logged in or not) ---
$principal = New-ScheduledTaskPrincipal `
    -UserId "$env:USERDOMAIN\$env:USERNAME" `
    -LogonType Interactive `
    -RunLevel Limited

# --- Register the task ---
Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal `
    -Description "Daily backup of ConsanDb database from Docker container to host." | Out-Null

Write-Host "✅ Task registered successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Task details:" -ForegroundColor Cyan
Get-ScheduledTask -TaskName $TaskName | Format-List TaskName, State, Description
Write-Host ""
Write-Host "🧪 To run it now (for testing):" -ForegroundColor Yellow
Write-Host "   Start-ScheduledTask -TaskName '$TaskName'"
Write-Host ""
Write-Host "🗑️  To remove it later:" -ForegroundColor Yellow
Write-Host "   Unregister-ScheduledTask -TaskName '$TaskName' -Confirm:\$false"
