<#
.SYNOPSIS
    ReCnC Build Script - Builds OpenRA with our modifications

.DESCRIPTION
    Wrapper script to build the modified OpenRA engine and verify our changes compile.
    Logs output in CMTrace format for easy review.

.PARAMETER Command
    Build command: all, clean, check, test (default: all)

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug for better error messages)

.EXAMPLE
    .\make.ps1
    .\make.ps1 -Command all
    .\make.ps1 -Command check -Configuration Release
#>

param(
    [ValidateSet("all", "clean", "check", "test")]
    [string]$Command = "all",
    
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
$OpenRASource = Join-Path $ScriptRoot "OpenRA-source"
$LogFile = Join-Path $ScriptRoot "build.log"
$BuildLogsDir = Join-Path $ScriptRoot "build-logs"
$Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$DateStamp = Get-Date -Format "yyyy-MM-dd"

# Ensure build-logs directory exists
if (-not (Test-Path $BuildLogsDir)) { New-Item -ItemType Directory -Path $BuildLogsDir | Out-Null }

# Calculate next build ID
$existingLogs = Get-ChildItem -Path $BuildLogsDir -Filter "${DateStamp}_*.log" -ErrorAction SilentlyContinue
$nextId = 1
if ($existingLogs) {
    $maxId = $existingLogs | ForEach-Object { 
        if ($_.Name -match "${DateStamp}_(\d+)") { [int]$Matches[1] } else { 0 }
    } | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum
    $nextId = $maxId + 1
}
$BuildId = $nextId.ToString("000")
$BuildLogFile = Join-Path $BuildLogsDir "${DateStamp}_${BuildId}_${Command}.log"

function Write-CMTraceLog
{
    param(
        [string]$Message,
        [ValidateSet("Info", "Warning", "Error")]
        [string]$Type = "Info",
        [string]$Component = "ReCnC-Build"
    )
    
    $TypeCode = switch ($Type) {
        "Info"    { 1 }
        "Warning" { 2 }
        "Error"   { 3 }
    }
    
    $Time = Get-Date -Format "HH:mm:ss.fff"
    $Date = Get-Date -Format "MM-dd-yyyy"
    
    $LogEntry = "<![LOG[$Message]LOG]!><time=`"$Time`" date=`"$Date`" component=`"$Component`" context=`"`" type=`"$TypeCode`" thread=`"$PID`" file=`"make.ps1`">"
    
    Add-Content -Path $LogFile -Value $LogEntry -Encoding UTF8
    
    $Color = switch ($Type) {
        "Info"    { "White" }
        "Warning" { "Yellow" }
        "Error"   { "Red" }
    }
    Write-Host "[$Type] $Message" -ForegroundColor $Color
}

function Test-DotNetSDK
{
    try
    {
        $dotnetVersion = & dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0)
        {
            Write-CMTraceLog "Found .NET SDK version: $dotnetVersion"
            return $true
        }
    }
    catch { }
    
    Write-CMTraceLog ".NET SDK not found. Please install from https://dotnet.microsoft.com/download" -Type Error
    return $false
}

function Get-NewFiles
{
    $newFiles = @(
        "OpenRA.Mods.Common\Pathfinder\IPathfindingStrategy.cs",
        "OpenRA.Mods.Common\Pathfinder\OpenRAPathfinder.cs",
        "OpenRA.Mods.Common\Pathfinder\ClassicPathfinder.cs",
        "OpenRA.Mods.Common\Pathfinder\ImprovedPathfinder.cs",
        "OpenRA.Mods.Common\Pathfinder\MoveType.cs",
        "OpenRA.Mods.Common\Pathfinder\PathfindingStrategyManager.cs",
        "OpenRA.Mods.Common\Traits\Air\IAircraftLanding.cs",
        "OpenRA.Mods.Common\Traits\Air\OpenRAAircraftLanding.cs",
        "OpenRA.Mods.Common\Traits\Air\ClassicAircraftLanding.cs",
        "OpenRA.Mods.Common\Traits\Air\ImprovedAircraftLanding.cs",
        "OpenRA.Mods.Common\Traits\Air\AircraftLandingManager.cs",
        "OpenRA.Mods.Common\Traits\World\AircraftLandingService.cs",
        "OpenRA.Mods.Common\Traits\IDockingStrategy.cs",
        "OpenRA.Mods.Common\Traits\OpenRADockingStrategy.cs",
        "OpenRA.Mods.Common\Traits\ClassicDockingStrategy.cs",
        "OpenRA.Mods.Common\Traits\ImprovedDockingStrategy.cs",
        "OpenRA.Mods.Common\Traits\SmartDockingService.cs",
        "OpenRA.Mods.Common\Traits\DockingStrategyManager.cs",
        "OpenRA.Mods.Common\Activities\ImprovedMoveToDock.cs"
    )
    return $newFiles
}

function Test-NewFilesExist
{
    Write-CMTraceLog "Checking for ReCnC custom files..."
    $newFiles = Get-NewFiles
    $missing = @()
    $found = 0
    
    foreach ($file in $newFiles)
    {
        $fullPath = Join-Path $OpenRASource $file
        if (Test-Path $fullPath)
        {
            $found++
        }
        else
        {
            $missing += $file
        }
    }
    
    Write-CMTraceLog "Found $found of $($newFiles.Count) custom files"
    
    if ($missing.Count -gt 0)
    {
        Write-CMTraceLog "Missing files:" -Type Warning
        foreach ($file in $missing)
        {
            Write-CMTraceLog "  - $file" -Type Warning
        }
        return $false
    }
    
    return $true
}

function Invoke-Build
{
    param([string]$BuildCommand, [string]$BuildConfig)
    
    Write-CMTraceLog "Starting build: Command=$BuildCommand, Configuration=$BuildConfig"
    
    Push-Location $OpenRASource
    try
    {
        $buildArgs = @()
        
        switch ($BuildCommand)
        {
            "all" {
                Write-CMTraceLog "Building all projects..."
                $buildArgs = @("build", "-c", $BuildConfig, "--nologo", "-p:TargetPlatform=win-x64")
            }
            "clean" {
                Write-CMTraceLog "Cleaning build output..."
                $buildArgs = @("clean", "--nologo")
            }
            "check" {
                Write-CMTraceLog "Building with warnings as errors..."
                $buildArgs = @("build", "-c", $BuildConfig, "--nologo", "-warnaserror", "-p:TargetPlatform=win-x64")
            }
            "test" {
                Write-CMTraceLog "Building test project..."
                $buildArgs = @("build", "OpenRA.Test\OpenRA.Test.csproj", "-c", $BuildConfig, "--nologo", "-p:TargetPlatform=win-x64")
            }
        }
        
        Write-CMTraceLog "Executing: dotnet $($buildArgs -join ' ')"
        
        $output = & dotnet @buildArgs 2>&1
        $exitCode = $LASTEXITCODE
        
        foreach ($line in $output)
        {
            $lineStr = $line.ToString()
            if ($lineStr -match "error\s*(CS|MSB)\d+")
            {
                Write-CMTraceLog $lineStr -Type Error
            }
            elseif ($lineStr -match "warning\s*(CS|MSB)\d+")
            {
                Write-CMTraceLog $lineStr -Type Warning
            }
            elseif ($lineStr -match "^\s*\d+ Error" -or $lineStr -match "Build FAILED")
            {
                Write-CMTraceLog $lineStr -Type Error
            }
            elseif ($lineStr -match "^\s*\d+ Warning")
            {
                Write-CMTraceLog $lineStr -Type Warning
            }
            elseif ($lineStr.Trim().Length -gt 0)
            {
                Write-CMTraceLog $lineStr
            }
        }
        
        return $exitCode
    }
    finally
    {
        Pop-Location
    }
}

# Main execution
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ReCnC Build Script" -ForegroundColor Cyan
Write-Host "  $Timestamp" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Initialize log file
if (Test-Path $LogFile) { Remove-Item $LogFile -Force }
Write-CMTraceLog "=== ReCnC Build Started ==="
Write-CMTraceLog "Command: $Command"
Write-CMTraceLog "Configuration: $Configuration"
Write-CMTraceLog "OpenRA Source: $OpenRASource"

# Pre-flight checks
if (-not (Test-Path $OpenRASource))
{
    Write-CMTraceLog "OpenRA source directory not found: $OpenRASource" -Type Error
    exit 1
}

if (-not (Test-DotNetSDK))
{
    exit 1
}

if (-not (Test-NewFilesExist))
{
    Write-CMTraceLog "Some ReCnC files are missing. Build may fail." -Type Warning
}

# Run build
$startTime = Get-Date
$exitCode = Invoke-Build -BuildCommand $Command -BuildConfig $Configuration
$duration = (Get-Date) - $startTime

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

$result = if ($exitCode -eq 0) { "SUCCESS" } else { "FAILED" }

if ($exitCode -eq 0)
{
    Write-CMTraceLog "=== BUILD SUCCEEDED ===" 
    Write-Host "  BUILD SUCCEEDED" -ForegroundColor Green
    Write-Host "  Duration: $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Green
}
else
{
    Write-CMTraceLog "=== BUILD FAILED (Exit Code: $exitCode) ===" -Type Error
    Write-Host "  BUILD FAILED" -ForegroundColor Red
    Write-Host "  Exit Code: $exitCode" -ForegroundColor Red
    Write-Host "  Duration: $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Red
}

# Save build log to build-logs folder
$buildLogContent = @"
================================================================================
ReCnC Build Log
================================================================================
Build ID:      $BuildId
Date:          $Timestamp
Command:       .\make.ps1 -Command $Command -Configuration $Configuration
Result:        $result

Duration:      $($duration.TotalSeconds.ToString('F1')) seconds
Exit Code:     $exitCode

CMTrace Log:   $LogFile
================================================================================
"@

$buildLogContent | Out-File -FilePath $BuildLogFile -Encoding UTF8
Write-Host "  Build Log: $BuildLogFile" -ForegroundColor Cyan
Write-Host "  CMTrace Log: $LogFile" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

exit $exitCode
