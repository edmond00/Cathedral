<#
.SYNOPSIS
    Builds a distributable Proscribed Palimpsest package: a folder with ProscribedPalimpsest.exe
    and everything it needs, and a zip of that folder ready to upload to itch.io.

.DESCRIPTION
    Two rules shape what ends up in the package.

    It is SELF-CONTAINED. The .NET runtime ships inside it, so a player needs nothing installed.
    That costs about 220 MB and removes the single most common "it won't start" support request.

    The payload is an ALLOW-LIST, not an exclude-list. Only files named in $Payload below are
    copied, and every one of them must exist or this script fails before writing a zip. The
    opposite arrangement — copy the repository, delete what looks unnecessary — silently ships
    whatever was added since anyone last read the delete list, and silently breaks the moment a
    new runtime asset is added without being un-deleted.

.PARAMETER OutputDir
    Where to build. Default "dist". Wiped at the start of every run.

.PARAMETER NoModel
    Skip the 2 GB model and the 332 MB word vectors. The result cannot run — it is for checking
    the script itself in seconds rather than minutes.

.PARAMETER NoZip
    Leave the staged folder without archiving it. Useful when you want to test the build by
    running it in place.

.PARAMETER ReadyToRun
    Precompile to native code. Roughly 30% larger, noticeably faster to start. Off by default
    because it makes the build slower and is not needed to verify a package is correct.

.EXAMPLE
    ./package.ps1
    ./package.ps1 -NoModel -NoZip      # fast check that staging works
#>
param(
    [string]$OutputDir     = "dist",
    [string]$Configuration = "Release",
    [string]$Runtime       = "win-x64",
    [switch]$NoModel,
    [switch]$NoZip,
    [switch]$ReadyToRun
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

# The player-facing name. "Cathedral" stays the development name — the repository, the namespaces
# and the Debug binary all keep it — but nothing a player downloads should say it. The csproj
# renames the executable under the same Ship flag; these two must agree.
$ShipName  = "ProscribedPalimpsest"
$ShipExe   = "$ShipName.exe"

$stage = Join-Path $root (Join-Path $OutputDir $ShipName)

function Step($text) { Write-Host "`n== $text" -ForegroundColor Cyan }
function Note($text) { Write-Host "   $text" -ForegroundColor DarkGray }
function Fail($text) { Write-Host "`nFAILED: $text" -ForegroundColor Red; exit 1 }

# ── What ships beside the executable ─────────────────────────────────────────
#
# Required = the game cannot run without it, so a missing one fails the build.
# Optional = a feature degrades but the game still starts, so a missing one only warns.
#            Only the sanitizer word lists are optional; CommonWordLexicon disables its
#            layer-2 guard and says so rather than throwing.
$Payload = @(
    # Fonts and terminal art. Resolved RELATIVE TO THE WORKING DIRECTORY (Config.FontPath is
    # "assets/fonts/FreeMono.ttf"), which is the exe's folder for a double-click or a normal
    # shortcut — but not if someone sets "Start in" to somewhere else.
    @{ Path = "assets/art";                       Required = $true  }
    @{ Path = "assets/fonts";                     Required = $true  }

    # The language model. Always this name — see models/README.md.
    @{ Path = "models/model.gguf";                Required = $true;  Big = $true }

    # The llama.cpp runtime, including any GPU backend under backends/.
    @{ Path = "models/llama";                     Required = $true  }

    # GloVe vectors. WordEmbedding treats these as mandatory and errors out without them.
    @{ Path = "models/embeddings";                Required = $true;  Big = $true }

    # The sanitizer's common-word list (CommonWordLexicon). Optional: without it the layer-2
    # guard disables itself and says so, rather than throwing.
    @{ Path = "models/en_scowl_40.txt";           Required = $false }

    # Player-facing documentation for the one thing a player may want to change.
    @{ Path = "models/README.md";                 Required = $false }
)

# Deliberately NOT shipped, recorded here so the reasoning survives:
#   data/        design source (skills.csv, items, body_parts.md) — nothing reads it at runtime
#   assets/old/  superseded art, unreferenced. Excluded for free: the payload above names
#                assets/art and assets/fonts individually rather than copying assets/ whole.
#   src/         shaders under src/terminal/Shaders are read from disk IF PRESENT and otherwise
#                fall back to strings embedded in the renderers, so a shipped build uses the
#                embedded copies. Keep the two in sync when editing a shader.
#   cli/ docs/ tools/ logs/ cache/ models_old/ DEPRECATED/   development only
#   *.pdb        debug symbols; suppressed at publish rather than deleted afterwards

# ── 1. Publish ───────────────────────────────────────────────────────────────

Step "Publishing ($Configuration, $Runtime, self-contained)"

if (Test-Path (Join-Path $root $OutputDir)) {
    Note "clearing $OutputDir"
    Remove-Item (Join-Path $root $OutputDir) -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $stage | Out-Null

$publishArgs = @(
    "publish", (Join-Path $root "Cathedral.csproj"),
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-o", $stage,
    # GUI subsystem: no console window behind the game for a player who double-clicks it.
    # A developer's `dotnet build` / `dotnet run` is unaffected and keeps its console.
    # ConsoleAttach still joins a terminal when the exe is launched from one, so `--cli` works
    # against this package — which is how the build below is verified.
    "-p:Ship=true",
    # No symbols in a player build: ~1 MB of nothing they can use.
    "-p:DebugType=none",
    "-p:DebugSymbols=false",
    # Trimming is NOT enabled and must not be. The audits reflect over Outcome and Verb
    # subclasses to build their catalogues, and Catalyst/MSAGL load types by name; a trimmer
    # removes exactly those and the failure appears at runtime, not at build.
    "-p:PublishTrimmed=false",

    # One executable instead of ~290 loose assemblies. Without this the folder a player opens
    # holds 301 files with the game buried in the middle of them, alphabetically between
    # PresentationFramework.dll and System.Private.CoreLib.dll.
    #
    # Bundling, NOT trimming: every assembly is still present, just inside the exe, so the
    # reflection the audits and Catalyst depend on is untouched. Managed code runs from the
    # bundle without being extracted anywhere, so there is no first-run unpacking cost. The
    # handful of unmanaged libraries that cannot be bundled stay beside it.
    "-p:PublishSingleFile=true",

    # The unmanaged libraries too, so the folder holds the game, assets/ and models/ and nothing
    # else. Without this they sit beside the executable, because the OS loader looks for a native
    # DLL next to the process — they cannot simply be moved to a subdirectory.
    #
    # The trade: these are unpacked to %TEMP%\.net\ the first time the game runs, which costs a
    # moment on that one launch and needs a writable TEMP. If an antivirus ever objects to the
    # self-extraction, dropping this one line puts the DLLs back beside the executable and changes
    # nothing else.
    "-p:IncludeNativeLibrariesForSelfExtract=true",

    # The game is English-only, and satellite resources are 13 folders and 221 files of
    # localised framework strings that nothing will ever read.
    "-p:SatelliteResourceLanguages=en"
)
if ($ReadyToRun) { $publishArgs += "-p:PublishReadyToRun=true" }

# Deliberately not redirected with 2>&1. In Windows PowerShell 5.1 that wraps every stderr line
# from a native command in an ErrorRecord and clears $?, which under ErrorActionPreference=Stop
# turns MSBuild's ordinary warning chatter into a failed build. $LASTEXITCODE is the truth.
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { Fail "dotnet publish returned $LASTEXITCODE" }

$exe = Join-Path $stage $ShipExe
if (-not (Test-Path $exe)) { Fail "no $ShipExe in the publish output" }

# Two native libraries the SDK copies that this build can never load: a macOS dylib, and the
# 32-bit MIDI native in a process that is x64 by <PlatformTarget>. Small, but the point of the
# single-file bundle is that what remains beside the executable is the short list a player can
# look at and see nothing out of place.
foreach ($junk in @("*.dylib", "*Native32.dll")) {
    Get-ChildItem $stage -File -Filter $junk -ErrorAction SilentlyContinue | ForEach-Object {
        Note ("removed {0} (cannot load on win-x64)" -f $_.Name)
        Remove-Item $_.FullName -Force
    }
}
Note ("runtime: {0:N0} files, {1:N1} MB" -f `
    (Get-ChildItem $stage -Recurse -File).Count,
    ((Get-ChildItem $stage -Recurse -File | Measure-Object Length -Sum).Sum / 1MB))

# ── 2. Stage the payload ─────────────────────────────────────────────────────

Step "Copying runtime files"

foreach ($item in $Payload) {
    $src = Join-Path $root $item.Path

    if ($NoModel -and $item.Big) {
        Note ("skipped (-NoModel): {0}" -f $item.Path)
        continue
    }

    if (-not (Test-Path $src)) {
        if ($item.Required) { Fail ("required payload missing: {0}" -f $item.Path) }
        Write-Host ("   WARNING: optional payload missing: {0}" -f $item.Path) -ForegroundColor Yellow
        continue
    }

    $dest = Join-Path $stage $item.Path
    $destParent = Split-Path $dest -Parent
    if (-not (Test-Path $destParent)) { New-Item -ItemType Directory -Force -Path $destParent | Out-Null }

    if (Test-Path $src -PathType Container) {
        Copy-Item $src $dest -Recurse -Force
    } else {
        Copy-Item $src $dest -Force
    }

    $bytes = if (Test-Path $dest -PathType Container) {
        (Get-ChildItem $dest -Recurse -File | Measure-Object Length -Sum).Sum
    } else {
        (Get-Item $dest).Length
    }
    Note ("{0,-28} {1,8:N1} MB" -f $item.Path, ($bytes / 1MB))
}

# ── 3. Verify before archiving ───────────────────────────────────────────────
#
# A package that is missing one file is worse than a build that failed, because it fails on the
# player's machine instead of here. These are the paths the game resolves at startup.

Step "Verifying"

$mustExist = @($ShipExe, "assets/fonts/FreeMono.ttf", "models/llama/llama-server.exe")
if (-not $NoModel) { $mustExist += @("models/model.gguf", "models/embeddings/glove.6B.100d.txt") }

$missing = $mustExist | Where-Object { -not (Test-Path (Join-Path $stage $_)) }
if ($missing) { Fail ("staged package is missing:`n     " + ($missing -join "`n     ")) }
Note ("checked {0} required paths, all present" -f $mustExist.Count)

# The CPU backends are not interchangeable — one per microarchitecture, chosen at runtime by
# host ISA. Shipping a subset silently gives some players a slower path or none at all.
$cpuBackends = (Get-ChildItem (Join-Path $stage "models/llama") -Filter "ggml-cpu-*.dll").Count
if ($cpuBackends -lt 10) { Fail "only $cpuBackends ggml-cpu-*.dll present; expected the full set" }
Note "$cpuBackends CPU backends"

$gpuBackends = Get-ChildItem (Join-Path $stage "models/llama/backends") -Directory -ErrorAction SilentlyContinue |
               Where-Object { (Get-ChildItem $_.FullName -Filter "ggml-*.dll").Count -gt 0 }
if ($gpuBackends) {
    Note ("GPU backends: " + (($gpuBackends | ForEach-Object { $_.Name }) -join ", "))
} else {
    Write-Host "   NOTE: no GPU backend bundled — every player runs on CPU." -ForegroundColor Yellow
    Write-Host "         See models/llama/backends/README.md." -ForegroundColor Yellow
}

$staged = (Get-ChildItem $stage -Recurse -File | Measure-Object Length -Sum).Sum
Note ("package: {0:N1} GB across {1:N0} files" -f ($staged / 1GB), (Get-ChildItem $stage -Recurse -File).Count)

# ── 4. Archive ───────────────────────────────────────────────────────────────

if ($NoZip) {
    Step "Done (no zip requested)"
    Write-Host "   $stage"
    exit 0
}

Step "Archiving"

$stamp = Get-Date -Format "yyyyMMdd"
$zip = Join-Path $root (Join-Path $OutputDir "$ShipName-win64-$stamp.zip")

# ZipFile, not Compress-Archive: the cmdlet in Windows PowerShell 5.1 fails on inputs above 2 GB,
# and this package is larger than that before the model is even counted.
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $stage, $zip, [System.IO.Compression.CompressionLevel]::Optimal, $true)

$zipBytes = (Get-Item $zip).Length
Note ("{0}  ({1:N2} GB)" -f (Split-Path $zip -Leaf), ($zipBytes / 1GB))

Step "Done"
Write-Host "   $zip"
if ($zipBytes -gt 1GB) {
    Write-Host "`n   Over 1 GB: itch.io's browser uploader will refuse this." -ForegroundColor Yellow
    Write-Host "   Use butler:  butler push `"$zip`" user/cathedral:windows" -ForegroundColor Yellow
}
