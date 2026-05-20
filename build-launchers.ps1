<#
.SYNOPSIS
    ReCnC Launcher Build Script - Builds game launcher executables for Windows

.DESCRIPTION
    Builds RedAlert.exe, TiberianDawn.exe, Dune2000.exe, TiberianSun.exe launchers, OpenRA.Utility.exe,
    and OpenRA.Server.exe. Creates portable ZIP archive and optionally NSIS installer.
    
    This is a Windows-native replacement for packaging/windows/buildpackage.sh.
    
    Features:
    - Generates ICO files from PNG artwork
    - Patches PE headers (LARGEADDRESSAWARE, GUI subsystem)
    - Sets EXE metadata (version, icon, company info) via rcedit
    - Downloads GeoIP database for server hosting
    - Sets version tags in mod.yaml files
    - Creates portable ZIP archive
    - Optionally builds NSIS installer
    
    Logs output in CMTrace format.

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Release)

.PARAMETER Platform
    Target platform: x64 or x86 (default: x64)

.PARAMETER OutputDir
    Output directory for built files (default: .\build)

.PARAMETER BuildInstaller
    If specified, compiles the NSIS installer after building (requires NSIS installed)

.PARAMETER PortableOnly
    If specified, skips NSIS installer build even if -BuildInstaller is set.
    Useful for creating only the portable ZIP distribution.

.PARAMETER Tag
    Version tag for the build. If not specified, auto-generates "release-YYYYMMDD" from
    today's date so the internal version always matches the actual build date.

.EXAMPLE
    .\build-launchers.ps1
    Basic build - creates portable ZIP only (auto-tags release-YYYYMMDD)

.EXAMPLE
    .\build-launchers.ps1 -BuildInstaller
    Full build with NSIS installer

.EXAMPLE
    .\build-launchers.ps1 -BuildInstaller -PortableOnly
    Build everything but skip installer (portable ZIP only)

.EXAMPLE
    .\build-launchers.ps1 -Platform x86 -Tag "release-20260405"
    32-bit build with custom version tag
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [ValidateSet("x64", "x86")]
    [string]$Platform = "x64",
    
    [string]$OutputDir = ".\build",
    
    [switch]$BuildInstaller,
    
    [switch]$PortableOnly,
    
    # BUG-007: Default tag auto-generates today's date so internal build version matches
    # the actual build date. Override with -Tag if a specific build label is needed.
    [string]$Tag = "release-$(Get-Date -Format 'yyyyMMdd')"
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
$OpenRASource = Join-Path $ScriptRoot "OpenRA-source"
$LogFile = Join-Path $ScriptRoot "build-launchers.log"
$Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

# FAQ URL for launcher crash dialogs
$FaqUrl = "https://wiki.openra.net/FAQ"

# Launcher definitions: Name, DisplayName, ModID
$Launchers = @(
    @{ Name = "RedAlert"; DisplayName = "Red Alert"; ModID = "ra" },
    @{ Name = "TiberianDawn"; DisplayName = "Tiberian Dawn"; ModID = "cnc" },
    @{ Name = "Dune2000"; DisplayName = "Dune 2000"; ModID = "d2k" },
    # BEGIN ReCnC BUG-011 — TS mod entry point (parity with other mods).
    @{ Name = "TiberianSun"; DisplayName = "Tiberian Sun"; ModID = "ts" }
    # END ReCnC BUG-011
)

function Write-CMTraceLog
{
    param(
        [string]$Message,
        [ValidateSet("Info", "Warning", "Error")]
        [string]$Type = "Info",
        [string]$Component = "ReCnC-Launchers"
    )
    
    $TypeCode = switch ($Type)
    {
        "Info"    { 1 }
        "Warning" { 2 }
        "Error"   { 3 }
    }
    
    $Time = Get-Date -Format "HH:mm:ss.fff"
    $Date = Get-Date -Format "MM-dd-yyyy"
    
    $LogEntry = "<![LOG[$Message]LOG]!><time=`"$Time`" date=`"$Date`" component=`"$Component`" context=`"`" type=`"$TypeCode`" thread=`"$PID`" file=`"build-launchers.ps1`">"
    
    Add-Content -Path $LogFile -Value $LogEntry -Encoding UTF8
    
    $Color = switch ($Type)
    {
        "Info"    { "White" }
        "Warning" { "Yellow" }
        "Error"   { "Red" }
    }
    Write-Host "[$Type] $Message" -ForegroundColor $Color
}

function Convert-PngToIco
{
    param(
        [string[]]$PngPaths,
        [string]$OutputPath
    )
    
    try
    {
        Add-Type -AssemblyName System.Drawing
        
        Write-CMTraceLog "Creating ICO file: $OutputPath"
        
        $images = @()
        foreach ($pngPath in $PngPaths)
        {
            if (Test-Path $pngPath)
            {
                $img = [System.Drawing.Image]::FromFile($pngPath)
                $images += $img
                Write-CMTraceLog "  Added: $([System.IO.Path]::GetFileName($pngPath)) ($($img.Width)x$($img.Height))"
            }
            else
            {
                Write-CMTraceLog "PNG not found: $pngPath" -Type Warning
            }
        }
        
        if ($images.Count -eq 0)
        {
            Write-CMTraceLog "No valid PNG images found for ICO creation" -Type Error
            return $false
        }
        
        $ms = New-Object System.IO.MemoryStream
        $bw = New-Object System.IO.BinaryWriter($ms)
        
        # ICO header: reserved (2), type=1 for ICO (2), image count (2)
        $bw.Write([uint16]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]$images.Count)
        
        # Calculate offset for first image data (after header + all directory entries)
        # Header = 6 bytes, each ICONDIRENTRY = 16 bytes
        $dataOffset = 6 + ($images.Count * 16)
        
        # Store PNG data for each image
        $pngDataList = @()
        foreach ($img in $images)
        {
            $pngMs = New-Object System.IO.MemoryStream
            $img.Save($pngMs, [System.Drawing.Imaging.ImageFormat]::Png)
            $pngDataList += ,$pngMs.ToArray()
            $pngMs.Dispose()
        }
        
        # Write ICONDIRENTRY for each image
        for ($i = 0; $i -lt $images.Count; $i++)
        {
            $img = $images[$i]
            $pngData = $pngDataList[$i]
            
            # Width (0 means 256)
            $width = if ($img.Width -ge 256) { 0 } else { [byte]$img.Width }
            # Height (0 means 256)
            $height = if ($img.Height -ge 256) { 0 } else { [byte]$img.Height }
            
            $bw.Write([byte]$width)
            $bw.Write([byte]$height)
            $bw.Write([byte]0)           # Color palette (0 for PNG)
            $bw.Write([byte]0)           # Reserved
            $bw.Write([uint16]1)         # Color planes
            $bw.Write([uint16]32)        # Bits per pixel
            $bw.Write([uint32]$pngData.Length)  # Size of image data
            $bw.Write([uint32]$dataOffset)       # Offset to image data
            
            $dataOffset += $pngData.Length
        }
        
        # Write actual PNG data for each image
        foreach ($pngData in $pngDataList)
        {
            $bw.Write($pngData)
        }
        
        # Save to file
        $bw.Flush()
        [System.IO.File]::WriteAllBytes($OutputPath, $ms.ToArray())
        
        # Cleanup
        $bw.Dispose()
        $ms.Dispose()
        foreach ($img in $images) { $img.Dispose() }
        
        Write-CMTraceLog "Successfully created: $OutputPath"
        return $true
    }
    catch
    {
        Write-CMTraceLog "Failed to create ICO: $($_.Exception.Message)" -Type Error
        return $false
    }
}

function Set-PESubsystem
{
    param(
        [string]$ExePath
    )
    
    try
    {
        Write-CMTraceLog "Patching PE headers: $ExePath"
        
        $bytes = [System.IO.File]::ReadAllBytes($ExePath)
        
        # Read PE offset from 0x3C (2 bytes, little-endian)
        $peOffset = [BitConverter]::ToUInt16($bytes, 0x3C)
        
        # Validate PE signature (should be 0x4550 = "PE\0\0")
        $peSignature = [BitConverter]::ToUInt32($bytes, $peOffset)
        if ($peSignature -ne 0x4550)
        {
            Write-CMTraceLog "Invalid PE signature at offset $peOffset" -Type Error
            return $false
        }
        
        # Set LARGEADDRESSAWARE flag at PE+4+18 (PE header + COFF header offset + characteristics)
        # Characteristics is at offset PE+4+18 = PE+22
        $charOffset = $peOffset + 4 + 18
        $currentFlags = $bytes[$charOffset]
        $bytes[$charOffset] = $currentFlags -bor 0x20
        Write-CMTraceLog "  Set LARGEADDRESSAWARE flag"
        
        # Set subsystem to GUI (2) at PE+0x5C (optional header subsystem field)
        $subsystemOffset = $peOffset + 0x5C
        $bytes[$subsystemOffset] = 0x02
        $bytes[$subsystemOffset + 1] = 0x00
        Write-CMTraceLog "  Set subsystem to Windows GUI"
        
        # Write back
        [System.IO.File]::WriteAllBytes($ExePath, $bytes)
        
        Write-CMTraceLog "Successfully patched: $ExePath"
        return $true
    }
    catch
    {
        Write-CMTraceLog "Failed to patch PE headers: $($_.Exception.Message)" -Type Error
        return $false
    }
}

function Get-RCEdit
{
    $rceditPath = Join-Path $ScriptRoot "rcedit-x64.exe"
    $rceditUrl = "https://github.com/electron/rcedit/releases/download/v1.1.1/rcedit-x64.exe"
    
    if (Test-Path $rceditPath)
    {
        Write-CMTraceLog "Using cached rcedit: $rceditPath"
        return $rceditPath
    }
    
    try
    {
        Write-CMTraceLog "Downloading rcedit from: $rceditUrl"
        
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($rceditUrl, $rceditPath)
        $webClient.Dispose()
        
        if (Test-Path $rceditPath)
        {
            $size = (Get-Item $rceditPath).Length / 1KB
            Write-CMTraceLog "Downloaded rcedit: $($size.ToString('F1')) KB"
            return $rceditPath
        }
        else
        {
            Write-CMTraceLog "Failed to download rcedit" -Type Error
            return $null
        }
    }
    catch
    {
        Write-CMTraceLog "Failed to download rcedit: $($_.Exception.Message)" -Type Error
        return $null
    }
}

function Set-ExeMetadata
{
    param(
        [string]$ExePath,
        [string]$ProductVersion,
        [string]$ProductName = "OpenRA",
        [string]$CompanyName = "The OpenRA team",
        [string]$FileDescription,
        [string]$LegalCopyright = "Copyright (c) The OpenRA Developers and Contributors",
        [string]$IconPath
    )
    
    $rceditPath = Get-RCEdit
    if (-not $rceditPath)
    {
        Write-CMTraceLog "Cannot set EXE metadata without rcedit" -Type Warning
        return $false
    }
    
    try
    {
        Write-CMTraceLog "Setting metadata for: $ExePath"
        
        # Convert tag format from "release-20250405" to "20250405-release" for rcedit
        # (rcedit can't handle versions starting with letters)
        $backwardsVersion = $ProductVersion
        if ($ProductVersion -match "^(\w+)-(.+)$")
        {
            $backwardsVersion = "$($Matches[2])-$($Matches[1])"
        }
        
        # Set product version
        $output = & $rceditPath $ExePath --set-product-version $backwardsVersion 2>&1
        if ($LASTEXITCODE -ne 0) { Write-CMTraceLog "  Warning: set-product-version: $output" -Type Warning }
        else { Write-CMTraceLog "  Set ProductVersion: $backwardsVersion" }
        
        # Set version strings
        $output = & $rceditPath $ExePath --set-version-string "ProductName" $ProductName 2>&1
        if ($LASTEXITCODE -eq 0) { Write-CMTraceLog "  Set ProductName: $ProductName" }
        
        $output = & $rceditPath $ExePath --set-version-string "CompanyName" $CompanyName 2>&1
        if ($LASTEXITCODE -eq 0) { Write-CMTraceLog "  Set CompanyName: $CompanyName" }
        
        if ($FileDescription)
        {
            $output = & $rceditPath $ExePath --set-version-string "FileDescription" $FileDescription 2>&1
            if ($LASTEXITCODE -eq 0) { Write-CMTraceLog "  Set FileDescription: $FileDescription" }
        }
        
        $output = & $rceditPath $ExePath --set-version-string "LegalCopyright" $LegalCopyright 2>&1
        if ($LASTEXITCODE -eq 0) { Write-CMTraceLog "  Set LegalCopyright" }
        
        # Set icon
        if ($IconPath -and (Test-Path $IconPath))
        {
            $output = & $rceditPath $ExePath --set-icon $IconPath 2>&1
            if ($LASTEXITCODE -eq 0) { Write-CMTraceLog "  Set Icon: $IconPath" }
            else { Write-CMTraceLog "  Warning: set-icon failed: $output" -Type Warning }
        }
        
        Write-CMTraceLog "Successfully updated metadata: $ExePath"
        return $true
    }
    catch
    {
        Write-CMTraceLog "Failed to set EXE metadata: $($_.Exception.Message)" -Type Error
        return $false
    }
}

function Get-GeoIPDatabase
{
    param(
        [string]$DestPath
    )
    
    $geoipFile = "IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP"
    $localPath = Join-Path $OpenRASource $geoipFile
    $destFile = Join-Path $DestPath $geoipFile
    $geoipUrl = "https://github.com/OpenRA/GeoIP-Database/releases/download/monthly/$geoipFile"
    
    # Check if we have a recent local copy (less than 30 days old)
    if (Test-Path $localPath)
    {
        $fileAge = (Get-Date) - (Get-Item $localPath).LastWriteTime
        if ($fileAge.TotalDays -lt 30)
        {
            Write-CMTraceLog "Using cached GeoIP database (age: $($fileAge.TotalDays.ToString('F1')) days)"
            Copy-Item -Path $localPath -Destination $destFile -Force
            return $true
        }
        else
        {
            Write-CMTraceLog "GeoIP database is older than 30 days, re-downloading..."
            Remove-Item $localPath -Force -ErrorAction SilentlyContinue
        }
    }
    
    try
    {
        Write-CMTraceLog "Downloading GeoIP database from: $geoipUrl"
        
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($geoipUrl, $localPath)
        $webClient.Dispose()
        
        if (Test-Path $localPath)
        {
            $size = (Get-Item $localPath).Length / 1MB
            Write-CMTraceLog "Downloaded GeoIP database: $($size.ToString('F2')) MB"
            Copy-Item -Path $localPath -Destination $destFile -Force
            return $true
        }
        else
        {
            Write-CMTraceLog "Failed to download GeoIP database" -Type Warning
            return $false
        }
    }
    catch
    {
        Write-CMTraceLog "Failed to download GeoIP database: $($_.Exception.Message)" -Type Warning
        return $false
    }
}

function Set-ModVersion
{
    param(
        [string]$VersionTag,
        [string[]]$ModYamlPaths
    )
    
    Write-CMTraceLog "Setting mod versions to: $VersionTag"
    
    foreach ($yamlPath in $ModYamlPaths)
    {
        if (-not (Test-Path $yamlPath))
        {
            Write-CMTraceLog "Mod YAML not found: $yamlPath" -Type Warning
            continue
        }
        
        try
        {
            $content = Get-Content -Path $yamlPath -Raw
            
            # Replace Version: line
            $content = $content -replace 'Version:\s*.*$', "Version: $VersionTag"
            
            # Replace user mod path references (e.g., /something: User -> /version: User)
            $content = $content -replace '/[^/]*:\s*User$', "/$VersionTag`: User"
            
            Set-Content -Path $yamlPath -Value $content -NoNewline
            Write-CMTraceLog "  Updated: $([System.IO.Path]::GetFileName($yamlPath))"
        }
        catch
        {
            Write-CMTraceLog "Failed to update $yamlPath`: $($_.Exception.Message)" -Type Warning
        }
    }
    
    Write-CMTraceLog "Mod version update complete"
}

function Build-PortableZip
{
    param(
        [string]$SourceDir,
        [string]$OutputPath
    )
    
    try
    {
        Write-CMTraceLog "Creating portable ZIP: $OutputPath"
        
        if (Test-Path $OutputPath)
        {
            Remove-Item $OutputPath -Force
        }
        
        Compress-Archive -Path "$SourceDir\*" -DestinationPath $OutputPath -CompressionLevel Optimal
        
        if (Test-Path $OutputPath)
        {
            $size = (Get-Item $OutputPath).Length / 1MB
            Write-CMTraceLog "Successfully created portable ZIP: $OutputPath ($($size.ToString('F2')) MB)"
            return $true
        }
        else
        {
            Write-CMTraceLog "Failed to create portable ZIP" -Type Error
            return $false
        }
    }
    catch
    {
        Write-CMTraceLog "Failed to create portable ZIP: $($_.Exception.Message)" -Type Error
        return $false
    }
}

function Build-Icons
{
    param(
        [string]$DestPath
    )
    
    Write-CMTraceLog "=== Generating Game Icons ==="
    
    $artworkDir = Join-Path $OpenRASource "packaging\artwork"
    
    if (-not (Test-Path $artworkDir))
    {
        Write-CMTraceLog "Artwork directory not found: $artworkDir" -Type Error
        return $false
    }
    
    $allSuccess = $true
    
    foreach ($mod in @("ra", "cnc", "d2k", "ts"))
    {
        $sizes = @("16x16", "24x24", "32x32", "48x48", "256x256")
        $pngPaths = @()
        
        foreach ($size in $sizes)
        {
            $pngPath = Join-Path $artworkDir "${mod}_${size}.png"
            if (Test-Path $pngPath)
            {
                $pngPaths += $pngPath
            }
        }
        
        if ($pngPaths.Count -gt 0)
        {
            $icoPath = Join-Path $DestPath "$mod.ico"
            if (-not (Convert-PngToIco -PngPaths $pngPaths -OutputPath $icoPath))
            {
                $allSuccess = $false
            }
        }
        elseif ($mod -eq "ts")
        {
            # ReCnC BUG-011: No dedicated TS ICO sources in packaging/artwork yet — reuse CNC icon for launcher metadata.
            $fallbackIco = Join-Path $DestPath "cnc.ico"
            $tsIco = Join-Path $DestPath "ts.ico"
            if ((Test-Path $fallbackIco) -and -not (Test-Path $tsIco))
            {
                Copy-Item -Path $fallbackIco -Destination $tsIco -Force
                Write-CMTraceLog "TS: no PNG artwork; using cnc.ico as placeholder for ts.ico"
            }
            elseif (-not (Test-Path $tsIco))
            {
                Write-CMTraceLog "No PNG files found for mod: ts (and cnc.ico missing for placeholder)" -Type Warning
                $allSuccess = $false
            }
        }
        else
        {
            Write-CMTraceLog "No PNG files found for mod: $mod" -Type Warning
            $allSuccess = $false
        }
    }
    
    return $allSuccess
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

function Stop-DotNetProcesses
{
    Write-CMTraceLog "Checking for lingering build processes..."
    
    # Only target specific OpenRA-related executables, NOT general dotnet or IDE processes
    $targetNames = @("RedAlert", "TiberianDawn", "Dune2000", "TiberianSun", "OpenRA.Utility", "OpenRA.Server")
    
    foreach ($name in $targetNames)
    {
        $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
        foreach ($proc in $procs)
        {
            try
            {
                Write-CMTraceLog "Stopping OpenRA process: $($proc.ProcessName) ($($proc.Id))"
                $proc | Stop-Process -Force -ErrorAction SilentlyContinue
            }
            catch { }
        }
    }
    
    Start-Sleep -Seconds 1
    Write-CMTraceLog "Process check complete"
}

function Clean-BuildDirectories
{
    param([string]$DestPath)
    
    Write-CMTraceLog "Cleaning build directories..."
    
    # First stop any processes that might be holding locks
    Stop-DotNetProcesses
    
    # Clean output directory
    if (Test-Path $DestPath)
    {
        Write-CMTraceLog "Removing: $DestPath"
        Remove-Item -Path $DestPath -Recurse -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
    }
    
    # Clean OpenRA bin directories
    $binDir = Join-Path $OpenRASource "bin"
    if (Test-Path $binDir)
    {
        Write-CMTraceLog "Removing: $binDir"
        Remove-Item -Path $binDir -Recurse -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
    }
    
    # Note: Don't manually remove obj directories - let dotnet clean handle it
    # Removing obj dirs can cause race conditions and build failures
    
    Start-Sleep -Seconds 2
    Write-CMTraceLog "Clean complete"
}

function Build-Launcher
{
    param(
        [string]$LauncherName,
        [string]$DisplayName,
        [string]$ModID,
        [string]$TargetPlatform,
        [string]$DestPath
    )
    
    Write-CMTraceLog "Building launcher: $LauncherName ($ModID) for $TargetPlatform"
    
    $LauncherProject = Join-Path $OpenRASource "OpenRA.WindowsLauncher\OpenRA.WindowsLauncher.csproj"
    
    if (-not (Test-Path $LauncherProject))
    {
        Write-CMTraceLog "Launcher project not found: $LauncherProject" -Type Error
        return $false
    }
    
    # Publish to a temp directory first to avoid overwriting other launchers
    $TempPublishDir = Join-Path $ScriptRoot "build-temp-$LauncherName"
    if (Test-Path $TempPublishDir) { Remove-Item $TempPublishDir -Recurse -Force }
    New-Item -ItemType Directory -Path $TempPublishDir -Force | Out-Null
    
    # Build the launcher with specific parameters
    $publishArgs = @(
        "publish"
        $LauncherProject
        "-c", $Configuration
        "-r", $TargetPlatform
        "-p:LauncherName=$LauncherName"
        "-p:TargetPlatform=$TargetPlatform"
        "-p:ModID=$ModID"
        "-p:DisplayName=$DisplayName"
        "-p:PublishDir=$TempPublishDir"
        "-p:FaqUrl=$FaqUrl"
        "-p:InformationalVersion=$Tag"
        "--self-contained", "true"
        "--nologo"
        "/m:1"
        "/p:BuildInParallel=false"
    )
    
    Write-CMTraceLog "Executing: dotnet $($publishArgs -join ' ')"
    
    $output = & dotnet @publishArgs 2>&1
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
        elseif ($lineStr.Trim().Length -gt 0)
        {
            Write-CMTraceLog $lineStr
        }
    }
    
    if ($exitCode -eq 0)
    {
        $tempExePath = Join-Path $TempPublishDir "$LauncherName.exe"
        $finalExePath = Join-Path $DestPath "$LauncherName.exe"
        
        if (Test-Path $tempExePath)
        {
            # Copy the exe with retry logic for network share resilience
            $maxRetries = 5
            $retryDelay = 3
            $copySucceeded = $false
            $copyError = $null
            
            for ($attempt = 1; $attempt -le $maxRetries; $attempt++)
            {
                try
                {
                    Copy-Item -Path $tempExePath -Destination $finalExePath -Force -ErrorAction Stop
                    $copySucceeded = $true
                    break
                }
                catch
                {
                    $copyError = $_.Exception.Message
                    if ($attempt -lt $maxRetries)
                    {
                        Write-CMTraceLog "Copy failed (attempt $attempt/$maxRetries): $copyError - Retrying in $retryDelay seconds..." -Type Warning
                        Start-Sleep -Seconds $retryDelay
                    }
                }
            }
            
            if (-not $copySucceeded) { throw $copyError }
            
            # Also copy the dll, pdb, and config files
            $dllPath = Join-Path $TempPublishDir "$LauncherName.dll"
            $pdbPath = Join-Path $TempPublishDir "$LauncherName.pdb"
            $configPath = Join-Path $TempPublishDir "$LauncherName.dll.config"
            $depsPath = Join-Path $TempPublishDir "$LauncherName.deps.json"
            $runtimePath = Join-Path $TempPublishDir "$LauncherName.runtimeconfig.json"
            
            if (Test-Path $dllPath) { Copy-Item -Path $dllPath -Destination $DestPath -Force }
            if (Test-Path $pdbPath) { Copy-Item -Path $pdbPath -Destination $DestPath -Force }
            if (Test-Path $configPath) { Copy-Item -Path $configPath -Destination $DestPath -Force }
            if (Test-Path $depsPath) { Copy-Item -Path $depsPath -Destination $DestPath -Force }
            if (Test-Path $runtimePath) { Copy-Item -Path $runtimePath -Destination $DestPath -Force }
            
            # Clean up temp directory
            Remove-Item -Path $TempPublishDir -Recurse -Force -ErrorAction SilentlyContinue
            
            # Patch PE headers (LARGEADDRESSAWARE + GUI subsystem)
            Set-PESubsystem -ExePath $finalExePath | Out-Null
            
            # Set EXE metadata and icon using rcedit
            $icoPath = Join-Path $DestPath "$ModID.ico"
            Set-ExeMetadata -ExePath $finalExePath `
                -ProductVersion $Tag `
                -FileDescription "$LauncherName mod for OpenRA" `
                -IconPath $icoPath | Out-Null
            
            Write-CMTraceLog "Successfully built: $finalExePath" 
            return $true
        }
        else
        {
            Write-CMTraceLog "Build succeeded but executable not found: $tempExePath" -Type Error
            Remove-Item -Path $TempPublishDir -Recurse -Force -ErrorAction SilentlyContinue
            return $false
        }
    }
    else
    {
        Write-CMTraceLog "Build failed for $LauncherName with exit code: $exitCode" -Type Error
        Remove-Item -Path $TempPublishDir -Recurse -Force -ErrorAction SilentlyContinue
        return $false
    }
}

function Install-GameData
{
    param([string]$DestPath)
    
    Write-CMTraceLog "Installing game data to $DestPath"
    
    # Copy mods
    $modsSource = Join-Path $OpenRASource "mods"
    $modsDest = Join-Path $DestPath "mods"
    
    if (-not (Test-Path $modsDest)) { New-Item -ItemType Directory -Path $modsDest -Force | Out-Null }
    
    # ReCnC BUG-011: include TS mod trees for TiberianSun.exe portable/installer builds.
    $modsToCopy = @("common", "common-content", "cnc", "cnc-content", "ra", "ra-content", "d2k", "d2k-content", "ts", "ts-content")
    foreach ($mod in $modsToCopy)
    {
        $src = Join-Path $modsSource $mod
        if (Test-Path $src)
        {
            Write-CMTraceLog "Copying mod: $mod"
            Copy-Item -Path $src -Destination $modsDest -Recurse -Force
        }
        else
        {
            Write-CMTraceLog "Mod not found: $mod" -Type Warning
        }
    }
    
    # Copy glsl shaders
    $glslSource = Join-Path $OpenRASource "glsl"
    if (Test-Path $glslSource)
    {
        Write-CMTraceLog "Copying GLSL shaders"
        Copy-Item -Path $glslSource -Destination $DestPath -Recurse -Force
    }
    
    # Copy other required files
    $filesToCopy = @("VERSION", "AUTHORS", "COPYING", "global mix database.dat")
    foreach ($file in $filesToCopy)
    {
        $src = Join-Path $OpenRASource $file
        if (Test-Path $src)
        {
            Copy-Item -Path $src -Destination $DestPath -Force
        }
    }
    
    # Create VERSION file with tag
    $Tag | Out-File -FilePath (Join-Path $DestPath "VERSION") -Encoding UTF8 -NoNewline
    
    Write-CMTraceLog "Game data installation complete"
}

function Build-NsisInstaller
{
    param(
        [string]$SrcDir,
        [string]$OutputFile,
        [string]$VersionTag,
        [string]$TargetPlatform
    )
    
    Write-CMTraceLog "=== Building NSIS Installer ==="
    
    # Check if NSIS is installed
    $makensisPath = $null
    $possiblePaths = @(
        "C:\Program Files (x86)\NSIS\makensis.exe",
        "C:\Program Files\NSIS\makensis.exe",
        (Get-Command makensis -ErrorAction SilentlyContinue).Source
    )
    
    foreach ($path in $possiblePaths)
    {
        if ($path -and (Test-Path $path))
        {
            $makensisPath = $path
            break
        }
    }
    
    if (-not $makensisPath)
    {
        Write-CMTraceLog "NSIS not found. Please install NSIS from https://nsis.sourceforge.io/" -Type Error
        Write-CMTraceLog "Skipping installer build." -Type Warning
        return $false
    }
    
    Write-CMTraceLog "Found NSIS: $makensisPath"
    
    $nsiScript = Join-Path $OpenRASource "packaging\windows\OpenRA.nsi"
    if (-not (Test-Path $nsiScript))
    {
        Write-CMTraceLog "NSI script not found: $nsiScript" -Type Error
        return $false
    }
    
    # Determine suffix based on tag
    $Suffix = ""
    if ($VersionTag -match "^playtest") { $Suffix = " (playtest)" }
    elseif ($VersionTag -notmatch "^release") { $Suffix = " (dev)" }
    
    # Build makensis arguments
    $nsisArgs = @(
        "-V2"
        "-DSRCDIR=$SrcDir"
        "-DTAG=$VersionTag"
        "-DSUFFIX=$Suffix"
        "-DOUTFILE=$OutputFile"
    )
    
    # Add 32-bit flag if needed
    if ($TargetPlatform -eq "win-x86")
    {
        $nsisArgs += "-DUSE_PROGRAMFILES32=true"
    }
    
    $nsisArgs += $nsiScript
    
    Write-CMTraceLog "Executing: $makensisPath $($nsisArgs -join ' ')"
    
    $output = & $makensisPath @nsisArgs 2>&1
    $exitCode = $LASTEXITCODE
    
    foreach ($line in $output)
    {
        $lineStr = $line.ToString()
        if ($lineStr -match "error" -and $lineStr -notmatch "^Output")
        {
            Write-CMTraceLog $lineStr -Type Error
        }
        elseif ($lineStr.Trim().Length -gt 0)
        {
            Write-CMTraceLog $lineStr
        }
    }
    
    if ($exitCode -eq 0 -and (Test-Path $OutputFile))
    {
        $size = (Get-Item $OutputFile).Length / 1MB
        Write-CMTraceLog "Successfully built installer: $OutputFile ($($size.ToString('F2')) MB)"
        return $true
    }
    else
    {
        Write-CMTraceLog "Failed to build installer (exit code: $exitCode)" -Type Error
        return $false
    }
}

function Build-CoreAssemblies
{
    param(
        [string]$TargetPlatform,
        [string]$DestPath
    )
    
    Write-CMTraceLog "Building core assemblies for $TargetPlatform"
    
    # First run dotnet clean to release file locks from previous builds
    Write-CMTraceLog "Running dotnet clean to release file locks..."
    Push-Location $OpenRASource
    cmd /c "dotnet clean --nologo >nul 2>nul"
    $cleanExitCode = $LASTEXITCODE
    Pop-Location
    if ($cleanExitCode -ne 0)
    {
        Write-CMTraceLog "dotnet clean exited with code $cleanExitCode; continuing with build" -Type Warning
    }
    Start-Sleep -Seconds 3
    
    # Build projects one at a time to avoid file locking issues
    # Order matters - dependencies first
    $projects = @(
        "OpenRA.Game\OpenRA.Game.csproj",
        "OpenRA.Platforms.Default\OpenRA.Platforms.Default.csproj",
        "OpenRA.Mods.Common\OpenRA.Mods.Common.csproj",
        "OpenRA.Mods.Cnc\OpenRA.Mods.Cnc.csproj",
        "OpenRA.Mods.D2k\OpenRA.Mods.D2k.csproj",
        "OpenRA.Utility\OpenRA.Utility.csproj",
        "OpenRA.Server\OpenRA.Server.csproj"
    )
    
    foreach ($project in $projects)
    {
        $projectPath = Join-Path $OpenRASource $project
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
        Write-CMTraceLog "Building: $projectName"
        
        # Wait a moment to ensure previous build's file handles are released
        Start-Sleep -Seconds 2
        
        $publishArgs = @(
            "publish"
            $projectPath
            "-c", $Configuration
            "-r", $TargetPlatform
            "-p:TargetPlatform=$TargetPlatform"
            "-p:PublishDir=$DestPath"
            "--self-contained", "true"
            "--nologo"
            "/m:1"
            "/p:BuildInParallel=false"
        )
        
        Write-CMTraceLog "Executing: dotnet $($publishArgs -join ' ')"
        
        $output = & dotnet @publishArgs 2>&1
        $exitCode = $LASTEXITCODE
        
        foreach ($line in $output)
        {
            $lineStr = $line.ToString()
            if ($lineStr -match "error\s*(CS|MSB)\d+")
            {
                Write-CMTraceLog $lineStr -Type Error
            }
            elseif ($lineStr -match "warning\s*(CS|MSB)\d+" -and $lineStr -notmatch "MSB3026")
            {
                # Skip MSB3026 (retry warnings) - they're noisy but usually succeed
                Write-CMTraceLog $lineStr -Type Warning
            }
            elseif ($lineStr -match "^\s*\w+\s*->" -and $lineStr.Trim().Length -gt 0)
            {
                Write-CMTraceLog $lineStr
            }
        }
        
        if ($exitCode -ne 0)
        {
            Write-CMTraceLog "Failed to build: $projectName" -Type Error
            return $false
        }
        
        Write-CMTraceLog "Successfully built: $projectName"
    }
    
    return $true
}

# Main execution
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ReCnC Launcher Build Script" -ForegroundColor Cyan
Write-Host "  $Timestamp" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Initialize log file
if (Test-Path $LogFile) { Remove-Item $LogFile -Force }
Write-CMTraceLog "=== ReCnC Launcher Build Started ==="
Write-CMTraceLog "Configuration: $Configuration"
Write-CMTraceLog "Platform: $Platform"
Write-CMTraceLog "Output Directory: $OutputDir"
Write-CMTraceLog "OpenRA Source: $OpenRASource"

# Resolve output directory to absolute path
$OutputDir = [System.IO.Path]::GetFullPath((Join-Path $ScriptRoot $OutputDir))
Write-CMTraceLog "Resolved Output Directory: $OutputDir"

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

# Clean and create output directory
$TargetPlatform = "win-$Platform"
$startTime = Get-Date
$success = $true

Write-CMTraceLog "=== Cleaning Build Directories ==="
Clean-BuildDirectories -DestPath $OutputDir

# Create output directory
if (-not (Test-Path $OutputDir))
{
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-CMTraceLog "Created output directory: $OutputDir"
}

# Pre-download rcedit for later use
Write-CMTraceLog "=== Preparing Build Tools ==="
$rceditPath = Get-RCEdit
if (-not $rceditPath)
{
    Write-CMTraceLog "Warning: rcedit not available, EXE metadata will not be set" -Type Warning
}

# Generate game icons (needed before launcher builds for embedding)
Write-CMTraceLog "=== Generating Game Icons ==="
if (-not (Build-Icons -DestPath $OutputDir))
{
    Write-CMTraceLog "Warning: Some icons failed to generate" -Type Warning
}

# Build core assemblies first
Write-CMTraceLog "=== Building Core Assemblies ==="
if (-not (Build-CoreAssemblies -TargetPlatform $TargetPlatform -DestPath $OutputDir))
{
    Write-CMTraceLog "Core assembly build failed" -Type Error
    $success = $false
}

# Build each launcher
if ($success)
{
    Write-CMTraceLog "=== Building Game Launchers ==="
    foreach ($launcher in $Launchers)
    {
        # Delay between launcher builds to avoid file lock conflicts on network shares
        Start-Sleep -Seconds 5
        
        if (-not (Build-Launcher -LauncherName $launcher.Name -DisplayName $launcher.DisplayName -ModID $launcher.ModID -TargetPlatform $TargetPlatform -DestPath $OutputDir))
        {
            Write-CMTraceLog "Failed to build launcher: $($launcher.Name)" -Type Error
            $success = $false
        }
    }
}

# Install game data
if ($success)
{
    Write-CMTraceLog "=== Installing Game Data ==="
    Install-GameData -DestPath $OutputDir
    
    # Set mod versions in YAML files
    Write-CMTraceLog "=== Setting Mod Versions ==="
    $modYamlPaths = @(
        (Join-Path $OutputDir "mods\cnc\mod.yaml"),
        (Join-Path $OutputDir "mods\ra\mod.yaml"),
        (Join-Path $OutputDir "mods\d2k\mod.yaml"),
        (Join-Path $OutputDir "mods\ts\mod.yaml"),
        (Join-Path $OutputDir "mods\cnc-content\mod.yaml"),
        (Join-Path $OutputDir "mods\ra-content\mod.yaml"),
        (Join-Path $OutputDir "mods\d2k-content\mod.yaml"),
        (Join-Path $OutputDir "mods\ts-content\mod.yaml")
    )
    Set-ModVersion -VersionTag $Tag -ModYamlPaths $modYamlPaths
    
    # Download GeoIP database
    Write-CMTraceLog "=== Downloading GeoIP Database ==="
    Get-GeoIPDatabase -DestPath $OutputDir | Out-Null
}

# Build portable ZIP
if ($success)
{
    Write-CMTraceLog "=== Building Portable ZIP ==="
    $portableZipName = "ReCnC-$Tag-$Platform-winportable.zip"
    $portableZipPath = Join-Path $ScriptRoot $portableZipName
    Build-PortableZip -SourceDir $OutputDir -OutputPath $portableZipPath | Out-Null
}

# Build NSIS installer if requested (and not portable-only)
if ($success -and $BuildInstaller -and (-not $PortableOnly))
{
    $installerName = "ReCnC-$Tag-$Platform.exe"
    $installerPath = Join-Path $ScriptRoot $installerName
    
    if (Build-NsisInstaller -SrcDir $OutputDir -OutputFile $installerPath -VersionTag $Tag -TargetPlatform $TargetPlatform)
    {
        Write-CMTraceLog "Installer created: $installerPath"
    }
    else
    {
        Write-CMTraceLog "Installer build failed" -Type Warning
    }
}

$duration = (Get-Date) - $startTime

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

if ($success)
{
    Write-CMTraceLog "=== BUILD SUCCEEDED ==="
    Write-Host "  BUILD SUCCEEDED" -ForegroundColor Green
    Write-Host "  Duration: $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Built executables:" -ForegroundColor Green
    foreach ($launcher in $Launchers)
    {
        $exePath = Join-Path $OutputDir "$($launcher.Name).exe"
        if (Test-Path $exePath)
        {
            $size = (Get-Item $exePath).Length / 1MB
            Write-Host "    - $($launcher.Name).exe ($($size.ToString('F2')) MB)" -ForegroundColor Green
        }
    }
    
    # Show portable ZIP
    $portableZipPath = Join-Path $ScriptRoot "ReCnC-$Tag-$Platform-winportable.zip"
    if (Test-Path $portableZipPath)
    {
        $size = (Get-Item $portableZipPath).Length / 1MB
        Write-Host ""
        Write-Host "  Portable ZIP:" -ForegroundColor Green
        Write-Host "    - ReCnC-$Tag-$Platform-winportable.zip ($($size.ToString('F2')) MB)" -ForegroundColor Green
    }
    
    # Show installer if built
    if ($BuildInstaller -and (-not $PortableOnly))
    {
        $installerPath = Join-Path $ScriptRoot "ReCnC-$Tag-$Platform.exe"
        if (Test-Path $installerPath)
        {
            $size = (Get-Item $installerPath).Length / 1MB
            Write-Host ""
            Write-Host "  Installer:" -ForegroundColor Green
            Write-Host "    - ReCnC-$Tag-$Platform.exe ($($size.ToString('F2')) MB)" -ForegroundColor Green
        }
    }
    
    Write-Host ""
    Write-Host "  Output: $OutputDir" -ForegroundColor Cyan
}
else
{
    Write-CMTraceLog "=== BUILD FAILED ===" -Type Error
    Write-Host "  BUILD FAILED" -ForegroundColor Red
    Write-Host "  Duration: $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Red
}

Write-Host "  Log: $LogFile" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($success) { exit 0 } else { exit 1 }
