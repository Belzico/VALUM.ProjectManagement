# VALUM Log Viewer
# Simple PowerShell script to view VALUM API logs
# Usage: .\scripts\watch-logs.ps1

$logDir = Join-Path $PSScriptRoot "..\src\VALUM.Api\logs"

if (-not (Test-Path $logDir)) {
    Write-Host "Log directory not found: $logDir" -ForegroundColor Yellow
    Write-Host "Logs will be created in this directory when the API starts." -ForegroundColor Yellow
    exit 0
}

# Find the latest log file
$logFiles = Get-ChildItem -Path $logDir -Filter "valum-*.log" | Sort-Object LastWriteTimeUtc -Descending

if ($logFiles.Count -eq 0) {
    Write-Host "No log files found in: $logDir" -ForegroundColor Yellow
    exit 0
}

$latestLog = $logFiles[0].FullName

Write-Host "Watching log file: $latestLog" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

# Show existing content first
Get-Content -Path $latestLog -Tail 50

# Watch for new content
Get-Content -Path $latestLog -Wait -Tail 10
