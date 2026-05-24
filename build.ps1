[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("debug", "release")]
    [string]$ExportMode = "release",

    [string]$ProjectRoot = "",
    [string]$GameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [string]$DotnetPath = "",
    [string]$GodotDotnetRoot = "",
    [string]$GodotPath = "",
    [string]$BaseLibPath = "",
    [switch]$SkipBuild,
    [switch]$SkipExport,
    [switch]$SkipLink
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "== $Message ==" -ForegroundColor Cyan
}

function Resolve-DotnetPath {
    param([string]$PreferredPath)

    $candidates = [System.Collections.Generic.List[string]]::new()
    if ($PreferredPath) { $candidates.Add($PreferredPath) }
    $candidates.Add("C:\Program Files (x86)\dotnet\dotnet.exe")
    $candidates.Add("C:\Program Files\dotnet\dotnet.exe")

    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnetCommand) { $candidates.Add($dotnetCommand.Source) }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (-not $candidate -or -not (Test-Path -LiteralPath $candidate)) { continue }
        $sdks = & $candidate --list-sdks
        if ($LASTEXITCODE -eq 0 -and ($sdks | Select-String '^9\.')) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw "Unable to find a usable .NET 9 SDK. Install .NET 9 or pass -DotnetPath explicitly."
}

function Resolve-GodotPath {
    param([string]$PreferredPath, [string]$ProjectRoot)

    # 1. Explicit user override wins
    if ($PreferredPath -and (Test-Path $PreferredPath)) { return (Resolve-Path $PreferredPath).Path }

    # 2. Local embedded Godot portable in _godot/ (zero-config after git clone)
    $localGodotConsole = Join-Path $ProjectRoot "_godot\Godot_v4.5.1-stable_mono_win64_console.exe"
    if (Test-Path $localGodotConsole) { return (Resolve-Path $localGodotConsole).Path }

    # 3. Explicit override may point to non-console variant — still try it
    $candidates = [System.Collections.Generic.List[string]]::new()
    if ($PreferredPath) { $candidates.Add($PreferredPath) }
    $candidates.Add("D:\Task_Panel\0_AliceJOH\0_AOJ_Reference\workspace\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe")
    $candidates.Add("D:\Task_Panel\0_AliceJOH\0_AOJ_Reference\workspace\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe")

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $searchRoots = @($WorkspaceRoot, "D:\Task_Panel", ${env:ProgramFiles}, ${env:ProgramFiles(x86)}) |
        Where-Object { $_ -and (Test-Path -LiteralPath $_) } |
        Select-Object -Unique

    foreach ($root in $searchRoots) {
        $found = Get-ChildItem -Path $root -Recurse -Filter "Godot_v4.5.1-stable_mono_win64_console.exe" -File -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName
        if ($found) {
            return $found
        }
    }

    throw "Unable to find Godot 4.5.1 mono. Pass -GodotPath explicitly."
}

function Resolve-GodotDotnetRoot {
    param([string]$PreferredPath)

    $candidates = [System.Collections.Generic.List[string]]::new()
    if ($PreferredPath) { $candidates.Add($PreferredPath) }
    $candidates.Add("C:\Program Files\dotnet")

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (-not $candidate -or -not (Test-Path -LiteralPath $candidate)) { continue }

        $fxrVersions = Get-ChildItem -LiteralPath (Join-Path $candidate "host\fxr") -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "9.*" }
        $runtimeVersions = Get-ChildItem -LiteralPath (Join-Path $candidate "shared\Microsoft.NETCore.App") -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "9.*" }

        if ($fxrVersions -and $runtimeVersions) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    return ""
}

function Ensure-Directory {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Reset-Directory {
    param([string]$Path)

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Force -Recurse
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Copy-DirectoryContents {
    param(
        [string]$SourcePath,
        [string]$DestinationPath
    )

    if (-not (Test-Path -LiteralPath $SourcePath)) {
        return
    }

    Ensure-Directory -Path $DestinationPath
    Get-ChildItem -LiteralPath $SourcePath -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $DestinationPath $_.Name) -Force -Recurse
    }
}

function Write-ModManifest {
    param(
        [string]$ManifestPath,
        [string]$Id,
        [string]$Name,
        [string]$Author = "OpenAI",
        [string]$Description,
        [string[]]$Dependencies = @(),
        [bool]$HasDll = $true,
        [bool]$HasPck = $false
    )

    $manifest = [ordered]@{
        id = $Id
        name = $Name
        author = $Author
        version = "0.1.0"
        description = $Description
        has_dll = $HasDll
        dependencies = $Dependencies
        has_pck = $HasPck
        affects_gameplay = $true
    }

    $json = $manifest | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText($ManifestPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}

function Sync-File {
    param(
        [string]$SourcePath,
        [string]$DestinationPath
    )

    $sourceItem = Get-Item -LiteralPath $SourcePath
    $shouldCopy = $true

    if (Test-Path -LiteralPath $DestinationPath) {
        $destinationItem = Get-Item -LiteralPath $DestinationPath
        if (
            $destinationItem.Length -eq $sourceItem.Length -and
            $destinationItem.LastWriteTimeUtc -eq $sourceItem.LastWriteTimeUtc
        ) {
            $shouldCopy = $false
        }
    }

    if (-not $shouldCopy) {
        return
    }

    try {
        Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force
        (Get-Item -LiteralPath $DestinationPath).LastWriteTimeUtc = $sourceItem.LastWriteTimeUtc
    }
    catch [System.IO.IOException] {
        if (-not (Test-Path -LiteralPath $DestinationPath)) {
            throw
        }

        Write-Warning "Skipped updating locked mirror file: $DestinationPath"
    }
}

function Ensure-Junction {
    param(
        [string]$LinkPath,
        [string]$TargetPath
    )

    Ensure-Directory -Path $TargetPath

    if (Test-Path -LiteralPath $LinkPath) {
        $item = Get-Item -LiteralPath $LinkPath -Force
        $isReparsePoint = ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0

        if (-not $isReparsePoint) {
            throw "Link path already exists and is not a junction: $LinkPath"
        }

        $currentTarget = @($item.Target)[0]
        if ($currentTarget -eq $TargetPath) {
            return
        }

        Remove-Item -LiteralPath $LinkPath -Force -Recurse
    }

    New-Item -ItemType Junction -Path $LinkPath -Target $TargetPath | Out-Null
}

# --- Load local user settings if available ---
if (-not $ProjectRoot) {
    # Must resolve project root before loading user_settings.json (which lives inside it)
    $ProjectRoot = $PSScriptRoot
}

$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$WorkspaceRoot = Split-Path -Parent $ProjectRoot

# Auto-load user_settings.json (if present — not tracked in git)
$UserSettingsPath = Join-Path $ProjectRoot "user_settings.json"
if (Test-Path $UserSettingsPath) {
    Write-Host "[+] Loaded local settings from: $UserSettingsPath"
    $settings = Get-Content $UserSettingsPath -Raw | ConvertFrom-Json
    if ($settings.GameRoot)   { $GameRoot   = $settings.GameRoot }
    if ($settings.BaseLibPath) { $BaseLibPath = $settings.BaseLibPath }
}
$ProjectName = Split-Path -Leaf $ProjectRoot
$ProjectFile = Join-Path $ProjectRoot "$ProjectName.csproj"
$DeployDir = Join-Path $ProjectRoot "_deploy"
$ExportDir = Join-Path $ProjectRoot "_export"
$ExportPckPath = Join-Path $ExportDir "$ProjectName.pck"
$FakeGameRoot = Join-Path $WorkspaceRoot "Steam\steamapps\common\Slay the Spire 2"
$FakeDataDir = Join-Path $FakeGameRoot "data_sts2_windows_x86_64"
$FakeBaseLibDir = Join-Path $FakeGameRoot "Mods\BaseLib"
$RealDataDir = Join-Path $GameRoot "data_sts2_windows_x86_64"

if (-not $BaseLibPath) {
    $BaseLibPath = Join-Path $WorkspaceRoot "0_UserInput\BaseLib.dll"
}

if (-not (Test-Path -LiteralPath $ProjectFile)) {
    throw "Project file not found: $ProjectFile"
}

if (-not (Test-Path -LiteralPath $GameRoot)) {
    throw "Game root not found: $GameRoot"
}

if (-not (Test-Path -LiteralPath $BaseLibPath)) {
    throw "BaseLib.dll not found: $BaseLibPath"
}

$DotnetPath = Resolve-DotnetPath -PreferredPath $DotnetPath
$GodotPath = Resolve-GodotPath -PreferredPath $GodotPath -ProjectRoot $ProjectRoot
$BuildDotnetRoot = Split-Path -Parent $DotnetPath
$GodotDotnetRoot = Resolve-GodotDotnetRoot -PreferredPath $GodotDotnetRoot

Write-Step "Prepare local dependency mirror"
Ensure-Directory -Path $FakeDataDir
Ensure-Directory -Path $FakeBaseLibDir
Sync-File -SourcePath (Join-Path $RealDataDir "sts2.dll") -DestinationPath (Join-Path $FakeDataDir "sts2.dll")
Sync-File -SourcePath (Join-Path $RealDataDir "GodotSharp.dll") -DestinationPath (Join-Path $FakeDataDir "GodotSharp.dll")
Sync-File -SourcePath (Join-Path $RealDataDir "0Harmony.dll") -DestinationPath (Join-Path $FakeDataDir "0Harmony.dll")
Sync-File -SourcePath $BaseLibPath -DestinationPath (Join-Path $FakeBaseLibDir "BaseLib.dll")
Write-ModManifest -ManifestPath (Join-Path $FakeBaseLibDir "BaseLib.json") -Id "BaseLib" -Name "BaseLib" -Author "Alchyr" -Description "Base library dependency for Slay the Spire 2 mods." -HasDll $true -HasPck $false

# Build-time properties for MSBuild (resolves $(GameRoot) / $(BaseLibPath) in .csproj)
$buildProps = @("-p:GameRoot=`"$GameRoot`"", "-p:BaseLibPath=`"$BaseLibPath`"")

if (-not $SkipBuild) {
    Write-Step "dotnet build"
    & $DotnetPath build $ProjectFile -c $Configuration -v minimal $buildProps
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed."
    }
}

Write-Step "Prepare deploy directory"
Reset-Directory -Path $DeployDir

Write-Step "dotnet publish"
& $DotnetPath publish $ProjectFile -c $Configuration -o $DeployDir -v minimal $buildProps
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

Get-ChildItem -LiteralPath $DeployDir -File | Where-Object {
    $_.Name -ne "$ProjectName.dll" -and
    $_.Name -ne "$ProjectName.pdb"
} | Remove-Item -Force

$DeployAssetDir = Join-Path $DeployDir "AliceMagatroid"
Copy-DirectoryContents -SourcePath (Join-Path $ProjectRoot "AliceMagatroid\Images") -DestinationPath (Join-Path $DeployAssetDir "Images")
Copy-DirectoryContents -SourcePath (Join-Path $ProjectRoot "AliceMagatroid\Scenes") -DestinationPath (Join-Path $DeployAssetDir "Scenes")
if (Test-Path -LiteralPath (Join-Path $ProjectRoot "AliceMagatroid\mod_image.png")) {
    Ensure-Directory -Path $DeployAssetDir
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "AliceMagatroid\mod_image.png") -Destination (Join-Path $DeployAssetDir "mod_image.png") -Force
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "AliceMagatroid\mod_image.png") -Destination (Join-Path $DeployDir "mod_image.png") -Force
}

Copy-DirectoryContents -SourcePath (Join-Path $ProjectRoot "localization") -DestinationPath (Join-Path $DeployDir "localization")

$DeployPckPath = Join-Path $DeployDir "$ProjectName.pck"
if (Test-Path -LiteralPath $DeployPckPath) {
    Remove-Item -LiteralPath $DeployPckPath -Force
}

Write-ModManifest -ManifestPath (Join-Path $DeployDir "$ProjectName.json") -Id $ProjectName -Name "Alice Magatroid" -Author "OpenAI" -Description "Playable Alice Magatroid character mod for Slay the Spire 2." -Dependencies @("BaseLib") -HasDll $true -HasPck (-not $SkipExport)

if (-not $SkipLink) {
    Write-Step "Create game Mods links"
    $GameModsDir = Join-Path $GameRoot "Mods"
    Ensure-Directory -Path $GameModsDir

    $ModLinkPath = Join-Path $GameModsDir $ProjectName
    Ensure-Junction -LinkPath $ModLinkPath -TargetPath $DeployDir

    $BaseLibLinkPath = Join-Path $GameModsDir "BaseLib"
    if (Test-Path -LiteralPath $BaseLibLinkPath) {
        $existingBaseLib = Join-Path $BaseLibLinkPath "BaseLib.dll"
        if (-not (Test-Path -LiteralPath $existingBaseLib)) {
            Ensure-Junction -LinkPath $BaseLibLinkPath -TargetPath $FakeBaseLibDir
        }
    }
    else {
        Ensure-Junction -LinkPath $BaseLibLinkPath -TargetPath $FakeBaseLibDir
    }
}

if (-not $SkipExport) {
    Write-Step "Godot export"
    Ensure-Directory -Path $ExportDir
    if (-not $GodotDotnetRoot) {
        throw "Godot export requires a 64-bit .NET 9 runtime. Install the x64 .NET 9 runtime/SDK under C:\Program Files\dotnet, or pass -GodotDotnetRoot explicitly."
    }

    $existingGodotIds = @(Get-Process | Where-Object { $_.Path -eq $GodotPath } | Select-Object -ExpandProperty Id)
    & $GodotPath --verbose --headless --path $ProjectRoot --export-pack "Windows Desktop" $DeployPckPath
    $godotExitCode = $LASTEXITCODE
    Start-Sleep -Seconds 1

    $newGodotProcesses = Get-Process | Where-Object {
        $_.Path -eq $GodotPath -and $_.Id -notin $existingGodotIds
    }

    if ($newGodotProcesses) {
        $newGodotProcesses | Wait-Process -Timeout 120 -ErrorAction SilentlyContinue
    }

    if ($godotExitCode -ne 0) {
        throw "Godot export failed."
    }

    if (-not (Test-Path -LiteralPath $DeployPckPath)) {
        throw "Godot export completed without producing $DeployPckPath"
    }
}