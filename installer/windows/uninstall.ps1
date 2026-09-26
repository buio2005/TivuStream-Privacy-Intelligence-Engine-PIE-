# TivuStream Privacy Intelligence Engine (PIE)
# Removes PIE from Windows.
#
# Installation Specification, Uninstallation Script. The data folder stays,
# unless -RemoveData is given and confirmed. Run as administrator:
#
#   powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
#   powershell -ExecutionPolicy Bypass -File .\uninstall.ps1 -RemoveData

param(
    [switch] $RemoveData
)

$ErrorActionPreference = 'Stop'

$ServiceName = 'TivuStreamPIE'
$ProgramDir = Join-Path $env:ProgramFiles 'TivuStream PIE'
$DataDir = Join-Path $env:ProgramData 'TivuStream PIE'
$FirewallRule = 'TivuStream PIE (HTTPS)'

$principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host 'This script must be run as administrator.' -ForegroundColor Red
    exit 1
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($null -ne $service) {
    if ($service.Status -ne 'Stopped') {
        Write-Host 'Stopping the service...'
        Stop-Service -Name $ServiceName
        $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(60))
    }

    sc.exe delete $ServiceName | Out-Null
    Write-Host 'Service removed.'
}

Get-NetFirewallRule -DisplayName $FirewallRule -ErrorAction SilentlyContinue | Remove-NetFirewallRule
Write-Host 'Firewall rule removed.'

if ([System.Diagnostics.EventLog]::SourceExists($ServiceName)) {
    Remove-EventLog -Source $ServiceName
}

if (Test-Path $ProgramDir) {
    Remove-Item $ProgramDir -Recurse -Force
    Write-Host "Program removed from $ProgramDir."
}

if (-not (Test-Path $DataDir)) {
    exit 0
}

if (-not $RemoveData) {
    Write-Host ''
    Write-Host "Your data is still in ${DataDir}: the history of your network, your settings and the token of your DNS server."
    Write-Host 'Installing PIE again will find it. To delete it too, run this script with -RemoveData.'
    exit 0
}

Write-Host ''
Write-Host "$DataDir holds:" -ForegroundColor Yellow
Write-Host '  - the history of what your network contacted'
Write-Host '  - your accounts and settings, with the token of your DNS server'
Write-Host '  - the certificate of PIE and any backup copies of the database'
Write-Host ''

$answer = Read-Host 'Delete all of it? This cannot be undone. Type YES to confirm'

if ($answer -cne 'YES') {
    Write-Host 'Nothing was deleted.'
    exit 0
}

Remove-Item $DataDir -Recurse -Force
Write-Host 'Data deleted.'
