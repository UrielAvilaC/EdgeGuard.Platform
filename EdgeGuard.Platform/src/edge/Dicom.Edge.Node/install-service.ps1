# ─────────────────────────────────────────────────────────────
# EdgeGuard Node — Windows Service installer script
# Run as Administrator
# ─────────────────────────────────────────────────────────────
param(
    [string]$ServiceName   = "EdgeGuardNode",
    [string]$DisplayName   = "EdgeGuard DICOM Node",
    [string]$Description   = "EdgeGuard Platform - DICOM edge node service (C-STORE SCP, routing, worklist)",
    [string]$ExePath       = "$PSScriptRoot\bin\publish\win-service\Dicom.Edge.Node.exe",
    [string]$StartupType   = "Automatic"
)

$ErrorActionPreference = "Stop"

# Ensure running as admin
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator."
    exit 1
}

# Create data directory
$dataDir = "C:\ProgramData\EdgeGuard\Node"
if (-not (Test-Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
    New-Item -ItemType Directory -Path "$dataDir\logs" -Force | Out-Null
    Write-Host "Created data directory: $dataDir"
}

# Stop existing service if running
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -eq "Running") {
        Write-Host "Stopping existing service..."
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }
    Write-Host "Removing existing service..."
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
}

# Install service
Write-Host "Installing service '$ServiceName'..."
New-Service -Name $ServiceName `
    -BinaryPathName "`"$ExePath`" --environment Production" `
    -DisplayName $DisplayName `
    -Description $Description `
    -StartupType $StartupType

# Set recovery options: restart on first, second, and subsequent failures
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null

Write-Host ""
Write-Host "Service '$ServiceName' installed successfully." -ForegroundColor Green
Write-Host "Start with: Start-Service -Name $ServiceName"
