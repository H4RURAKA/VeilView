param(
    [string]$Runtime = "win-x64",
    [switch]$FolderBundle,
    [switch]$FrameworkDependent,
    [switch]$NoAutoInstallSdk
)

$ErrorActionPreference = "Stop"
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $OutputEncoding = [System.Text.Encoding]::UTF8
} catch {}

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root "src\VeilView\VeilView.csproj"
$OutDir = if ($FolderBundle) { Join-Path $Root "dist-folder" } else { Join-Path $Root "dist" }
$LocalDotNetDir = Join-Path $Root ".dotnet"
$LocalDotNet = Join-Path $LocalDotNetDir "dotnet.exe"
$InstallScript = Join-Path $Root ".dotnet-install.ps1"
$AppName = "VeilView"

function Test-HasSdk {
    param([Parameter(Mandatory = $true)][string]$DotNetPath)

    try {
        if ([string]::IsNullOrWhiteSpace($DotNetPath)) { return $false }
        if (-not (Test-Path $DotNetPath) -and $DotNetPath -notmatch "(^|\\|/)dotnet(\.exe)?$") { return $false }

        $sdks = & $DotNetPath --list-sdks 2>$null
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace(($sdks | Out-String))) { return $false }

        foreach ($sdk in $sdks) {
            if ($sdk -match "^(\d+)\.") {
                if ([int]$Matches[1] -ge 8) { return $true }
            }
        }
        return $false
    }
    catch { return $false }
}

function Invoke-NativeVisible {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    if ([string]::IsNullOrWhiteSpace($FilePath)) { throw "Executable path is empty." }

    & $FilePath @Arguments 2>&1 | ForEach-Object { Write-Host $_ }
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "Command failed with exit code ${exitCode}: $FilePath $($Arguments -join ' ')"
    }
}

function Invoke-WebRequestCompat {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][string]$OutFile
    )

    if ($PSVersionTable.PSVersion.Major -lt 6) {
        Invoke-WebRequest -Uri $Uri -OutFile $OutFile -UseBasicParsing
    }
    else {
        Invoke-WebRequest -Uri $Uri -OutFile $OutFile
    }
}

function Get-DotNetCli {
    if ((Test-Path $LocalDotNet) -and (Test-HasSdk -DotNetPath $LocalDotNet)) {
        return $LocalDotNet
    }

    $globalDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($globalDotnet -and (Test-HasSdk -DotNetPath $globalDotnet.Source)) {
        return $globalDotnet.Source
    }

    if ($NoAutoInstallSdk) {
        throw ".NET SDK 8 or newer was not found. Install it, or run this script without -NoAutoInstallSdk."
    }

    Write-Host "[1/5] .NET SDK 8+ was not found. Installing a local SDK into: $LocalDotNetDir"
    Write-Host "      Administrator permission is not required."
    New-Item -ItemType Directory -Force -Path $LocalDotNetDir | Out-Null

    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequestCompat -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $InstallScript | Out-Null

    Invoke-NativeVisible "powershell" @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $InstallScript,
        "-Channel", "8.0",
        "-Architecture", "x64",
        "-InstallDir", $LocalDotNetDir
    )

    if (-not (Test-HasSdk -DotNetPath $LocalDotNet)) {
        throw ".NET SDK installation finished, but dotnet.exe with SDK 8+ was not found at: $LocalDotNet"
    }

    return $LocalDotNet
}

if (-not (Test-Path $Project)) { throw "Project file not found: $Project" }

$DotNet = Get-DotNetCli
if ($DotNet -is [array]) {
    $DotNet = @($DotNet) | Where-Object { $_ -is [string] -and ((Test-Path $_) -or ($_ -match "(^|\\|/)dotnet(\.exe)?$")) } | Select-Object -Last 1
}
$DotNet = [string]$DotNet
if ([string]::IsNullOrWhiteSpace($DotNet)) { throw "Could not resolve dotnet CLI path." }

$env:DOTNET_ROOT = Split-Path -Parent $DotNet
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

Write-Host "[2/5] dotnet CLI: $DotNet"
Invoke-NativeVisible $DotNet @("--info")

if (Test-Path $OutDir) { Remove-Item -Recurse -Force $OutDir }

Write-Host "[3/5] Restoring NuGet packages..."
Invoke-NativeVisible $DotNet @("restore", $Project, "-r", $Runtime)

$selfContainedValue = if ($FrameworkDependent) { "false" } else { "true" }
$singleFileValue = if ($FolderBundle) { "false" } else { "true" }
$targetDescription = if ($FolderBundle) { "portable folder bundle" } else { "single movable exe" }

Write-Host "[4/5] Publishing $targetDescription..."
$publishArgs = @(
    "publish", $Project,
    "-c", "Release",
    "-r", $Runtime,
    "--self-contained", $selfContainedValue,
    "/p:PublishSingleFile=$singleFileValue",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:IncludeAllContentForSelfExtract=true",
    "/p:EnableCompressionInSingleFile=true",
    "/p:PublishTrimmed=false",
    "/p:DebugType=None",
    "/p:DebugSymbols=false",
    "-o", $OutDir
)
Invoke-NativeVisible $DotNet $publishArgs

$Exe = Join-Path $OutDir "$AppName.exe"
if (-not (Test-Path $Exe)) { throw "Publish completed, but the exe was not found: $Exe" }

Write-Host "[5/5] Checking output..."
if (-not $FolderBundle) {
    $extra = Get-ChildItem -Path $OutDir -Force | Where-Object { $_.Name -ne "$AppName.exe" }
    if ($extra.Count -eq 0) {
        Write-Host "Single-file output OK: only $AppName.exe exists in dist." -ForegroundColor Green
    }
    else {
        Write-Host "Single-file publish succeeded, but extra files also exist:" -ForegroundColor Yellow
        $extra | ForEach-Object { Write-Host "  $($_.Name)" }
        Write-Host "Move $AppName.exe first. If it fails, use BUILD_PORTABLE_FOLDER.cmd." -ForegroundColor Yellow
    }
}
else {
    Write-Host "Folder bundle output OK. Keep all files inside dist-folder together." -ForegroundColor Green
}

Write-Host ""
Write-Host "SUCCESS: $Exe" -ForegroundColor Green
Write-Host "Run it by executing:"
Write-Host "  `"$Exe`""
