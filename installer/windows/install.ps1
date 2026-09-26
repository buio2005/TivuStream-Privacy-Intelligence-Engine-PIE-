# TivuStream Privacy Intelligence Engine (PIE)
# Installs PIE on Windows as a service, or updates an installation.
#
# Installation Specification, Installation Script. Run from the folder of the
# extracted package, in a PowerShell opened as administrator:
#
#   powershell -ExecutionPolicy Bypass -File .\install.ps1

$ErrorActionPreference = 'Stop'

$ServiceName = 'TivuStreamPIE'
$DisplayName = 'TivuStream PIE'
$ProgramDir = Join-Path $env:ProgramFiles 'TivuStream PIE'
$DataDir = Join-Path $env:ProgramData 'TivuStream PIE'
$Program = Join-Path $ProgramDir 'tivustream-pie.exe'
$Completed = Join-Path $DataDir 'installation-completed'
$HttpPort = 5000
$HttpsPort = 5443
$FirewallRule = 'TivuStream PIE (HTTPS)'
$Package = Join-Path $PSScriptRoot 'app'

$done = New-Object System.Collections.Generic.List[string]

function Step([string] $text) {
    Write-Host ''
    Write-Host "== $text" -ForegroundColor Cyan
}

function Stop-Because([string] $text) {
    Write-Host ''
    Write-Host $text -ForegroundColor Red
    if ($done.Count -gt 0) {
        Write-Host 'Already done:'
        $done | ForEach-Object { Write-Host "  - $_" }
    }
    exit 1
}

# Runs the program in this window, so that what it asks is seen and what the
# person types reaches it, and returns how it ended.
function Invoke-Pie([string[]] $arguments) {
    $quoted = @($arguments | ForEach-Object { "`"$_`"" }) + "`"--DataDirectory=$DataDir`""
    $process = Start-Process -FilePath $Program -ArgumentList $quoted -NoNewWindow -Wait -PassThru
    return $process.ExitCode
}

function Copy-Program {
    robocopy $Package $ProgramDir /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    return $LASTEXITCODE -lt 8
}

function Wait-Answer {
    # 401 is the answer of a PIE that is up and asks who is calling.
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            Invoke-WebRequest "http://localhost:$HttpPort/api/v1/health" -UseBasicParsing -TimeoutSec 2 | Out-Null
            return $true
        }
        catch {
            $response = $_.Exception.Response
            if ($null -ne $response -and [int]$response.StatusCode -eq 401) {
                return $true
            }
        }
        Start-Sleep -Seconds 1
    }
    return $false
}

$EventViewerHint = 'Look at the Event Viewer, under Windows Logs, Application, source TivuStreamPIE.'

# ------------------------------------------------------------------
Step 'Checking this computer'

$principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Stop-Because 'This script must be run as administrator. Open PowerShell with "Run as administrator" and start it again.'
}

if (-not [Environment]::Is64BitOperatingSystem) {
    Stop-Because 'PIE needs a 64-bit Windows.'
}

if (-not (Test-Path (Join-Path $Package 'tivustream-pie.exe'))) {
    Stop-Because "The program was not found in $Package. Run the script from the folder of the extracted package."
}

$drive = Get-PSDrive -Name ($env:SystemDrive.TrimEnd(':'))
if ($drive.Free -lt 1GB) {
    Stop-Because "There is less than 1 GB free on $($env:SystemDrive). PIE needs room for the program and its history."
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

# An installation is updated only when it was completed: an interrupted one
# is taken up again from the start, and every step below can be repeated.
$installed = $null -ne $service -and (Test-Path $Completed)

if ($null -ne $service -and $service.Status -ne 'Stopped') {
    Stop-Service -Name $ServiceName
    $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(60))
    $done.Add('service stopped')
}

foreach ($port in @($HttpPort, $HttpsPort)) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        Stop-Because "Port $port is already in use by another program. Stop it, then start this script again."
    }
}

Write-Host 'Everything needed is in place.'

# ------------------------------------------------------------------
if ($installed) {
    Step 'Updating PIE'

    Write-Host 'Replacing the program. Your data and settings are not touched.'
    if (-not (Copy-Program)) {
        Stop-Because 'The program could not be replaced. The service is stopped: run this script again.'
    }
    $done.Add('program replaced')

    Start-Service -Name $ServiceName

    if (-not (Wait-Answer)) {
        Stop-Because "The service did not answer after the update. $EventViewerHint"
    }

    Step 'Updated'
    Invoke-Pie @('access') | Out-Null
    exit 0
}

# ------------------------------------------------------------------
Step 'Copying the program'

New-Item -ItemType Directory -Force $ProgramDir | Out-Null
if (-not (Copy-Program)) {
    Stop-Because "The program could not be copied to $ProgramDir."
}
$done.Add("program copied to $ProgramDir")
Write-Host "Program in $ProgramDir"

# ------------------------------------------------------------------
Step 'Registering the service'

if ($null -eq $service) {
    New-Service -Name $ServiceName `
        -DisplayName $DisplayName `
        -Description 'Reads your DNS server and reports what your network contacts.' `
        -BinaryPathName "`"$Program`" `"--DataDirectory=$DataDir`"" `
        -StartupType Automatic | Out-Null
}

# The service runs under an account of its own, with no administrator
# rights. Windows creates the account together with the service.
sc.exe config $ServiceName obj= "NT SERVICE\$ServiceName" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Stop-Because 'The account of the service could not be set.'
}

# Restarted by Windows if it ever stops unexpectedly.
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

# Where the service reports problems, under its own name.
if (-not [System.Diagnostics.EventLog]::SourceExists($ServiceName)) {
    New-EventLog -LogName Application -Source $ServiceName
}

$done.Add("service $ServiceName registered, not started")

# ------------------------------------------------------------------
Step 'Preparing the data folder'

# Readable only by the service, by the system and by administrators: it
# holds the history of the network and the token of the DNS server.
New-Item -ItemType Directory -Force $DataDir | Out-Null

$acl = New-Object System.Security.AccessControl.DirectorySecurity
$acl.SetAccessRuleProtection($true, $false)
$inherit = [System.Security.AccessControl.InheritanceFlags]'ContainerInherit, ObjectInherit'
$none = [System.Security.AccessControl.PropagationFlags]::None
$allow = [System.Security.AccessControl.AccessControlType]::Allow

$acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule('NT AUTHORITY\SYSTEM', 'FullControl', $inherit, $none, $allow)))
$acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule('BUILTIN\Administrators', 'FullControl', $inherit, $none, $allow)))
$acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule("NT SERVICE\$ServiceName", 'Modify', $inherit, $none, $allow)))

Set-Acl -Path $DataDir -AclObject $acl

$done.Add("data folder $DataDir")
Write-Host "Data in $DataDir, readable only by PIE and by administrators."

# ------------------------------------------------------------------
Step 'Connecting to your DNS server'

if ((Invoke-Pie @('configure')) -ne 0) {
    Stop-Because 'PIE is not connected to the DNS server. Run this script again when you have the address and the token.'
}
$done.Add('connected to the DNS server')

# ------------------------------------------------------------------
Step 'Creating your administrator account'

Write-Host 'Choose the name you will sign in with: 3 to 32 characters, lower case letters, digits, dot, hyphen, underscore.'

while ($true) {
    $name = Read-Host 'Name'
    if ((Invoke-Pie @('reset-password', $name)) -eq 0) { break }
    Write-Host 'Try again.'
}
$done.Add("administrator $name")

# ------------------------------------------------------------------
Step 'Opening the HTTPS port to your home network'

# A service does not bring up the firewall question: without this rule other
# devices could not open PIE, and nobody would be told why.
Get-NetFirewallRule -DisplayName $FirewallRule -ErrorAction SilentlyContinue | Remove-NetFirewallRule

New-NetFirewallRule -DisplayName $FirewallRule `
    -Direction Inbound -Protocol TCP -LocalPort $HttpsPort `
    -Program $Program -Profile Private -Action Allow | Out-Null

$done.Add("firewall rule '$FirewallRule', private networks only")
Write-Host "Port $HttpsPort opened for PIE, on private networks only."

# ------------------------------------------------------------------
Step 'Starting PIE'

# The certificate belongs to the account of the service, which creates it as
# it starts. One left by an earlier attempt, made by another account, could
# be neither read nor replaced by it.
Remove-Item (Join-Path $DataDir 'data\tls') -Recurse -Force -ErrorAction SilentlyContinue

Start-Service -Name $ServiceName

if (-not (Wait-Answer)) {
    Stop-Because "The service started but does not answer. $EventViewerHint"
}

# The certificate is created by the service as it starts.
Start-Sleep -Seconds 2

# ------------------------------------------------------------------
Step 'Ready'

# Written last: from now on, running this script again updates PIE.
Set-Content -Path $Completed -Value (Get-Date -Format 'o')

Invoke-Pie @('access') | Out-Null

Write-Host ''
Write-Host "Sign in as '$name' with the password you just chose."
