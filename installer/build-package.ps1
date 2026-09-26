# TivuStream Privacy Intelligence Engine (PIE)
# Builds the installation packages.
#
# Installation Specification, Package: one package per platform, with the
# program, the compiled interface, the installation scripts and the guide.
# The .NET runtime is included, so nothing else needs to be installed.
#
# Run from any folder:
#
#   powershell -ExecutionPolicy Bypass -File installer\build-package.ps1
#
# The packages are written to dist\ at the root of the repository.

[CmdletBinding()]
param(
    [string[]] $Platforms = @('win-x64', 'linux-x64')
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$frontend = Join-Path $root 'frontend'
$api = Join-Path $root 'backend\src\TivuStream.Pie.Api\TivuStream.Pie.Api.csproj'
$dist = Join-Path $root 'dist'

[xml] $props = Get-Content (Join-Path $root 'backend\Directory.Build.props')
$version = ($props.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version

if (-not $version) {
    throw 'The product version was not found in backend\Directory.Build.props.'
}

Write-Host "TivuStream PIE $version"

# The interface is compiled into the engine's wwwroot, where the published
# program serves it from.
Write-Host 'Compiling the interface...'
Push-Location $frontend
try {
    # Installed only when absent: reinstalling would pull the modules from
    # under a development server that may be running.
    if (-not (Test-Path 'node_modules')) {
        npm ci --no-audit --no-fund | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' }
    }

    npm run build | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'The interface did not compile.' }
}
finally {
    Pop-Location
}

if (Test-Path $dist) {
    Remove-Item $dist -Recurse -Force
}

foreach ($platform in $Platforms) {
    $name = "tivustream-pie-$version-$platform"
    $stage = Join-Path $dist $name
    $app = Join-Path $stage 'app'

    Write-Host "Publishing for $platform..."

    dotnet publish $api `
        --configuration Release `
        --runtime $platform `
        --self-contained true `
        --output $app `
        -p:PublishSingleFile=false `
        -p:DebugType=none | Out-Host

    if ($LASTEXITCODE -ne 0) { throw "Publishing for $platform failed." }

    # The documentation files serve the build, which needs them for its
    # analysers, and nobody who installs.
    Get-ChildItem $app -Filter '*.xml' | Remove-Item

    # Nothing of the developer's machine may travel in a package: not the
    # local settings with the token, not a database, not a certificate.
    $forbidden = Get-ChildItem $app -Recurse -File |
        Where-Object { $_.Name -eq 'appsettings.Local.json' -or $_.Extension -in '.db', '.pfx', '.bak' }

    if ($forbidden) {
        throw "The package would contain files of this machine: $($forbidden.FullName -join ', ')"
    }

    if (-not (Test-Path (Join-Path $app 'wwwroot\index.html'))) {
        throw 'The package would contain no interface.'
    }

    $scripts = if ($platform -like 'win-*') { 'windows' } else { 'linux' }

    Copy-Item (Join-Path $PSScriptRoot "$scripts\*") $stage -Recurse
    Copy-Item (Join-Path $PSScriptRoot 'INSTALL.md') $stage
    Copy-Item (Join-Path $root 'LICENSE.md') $stage

    if ($platform -like 'win-*') {
        Compress-Archive -Path $stage -DestinationPath "$stage.zip"
    }
    else {
        # tar is part of Windows 10 and later. Permissions do not survive it
        # from Windows: the Linux script makes the program executable.
        tar -czf "$stage.tar.gz" -C $dist $name
        if ($LASTEXITCODE -ne 0) { throw 'The Linux archive could not be written.' }
    }

    Write-Host "Written: $stage"
}
