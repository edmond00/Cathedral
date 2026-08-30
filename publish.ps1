<#
.SYNOPSIS
    Uploads a packaged Proscribed Palimpsest build to itch.io with butler.

.DESCRIPTION
    Run package.ps1 first; this uploads what it staged.

    It pushes the STAGED FOLDER, not the zip, and that choice matters more than it looks.
    butler diffs a build against the previous one file by file and uploads only what changed.
    Nearly all of this game is model.gguf, which does not change between releases — pushed as a
    folder it is uploaded once and skipped forever after, so a code-only update sends a few
    hundred MB instead of 2.2 GB. A zip is one opaque blob: change one byte of code and the whole
    archive is new. Players still get a downloadable archive either way; itch builds one from the
    pushed files.

    Credentials are never handled here. butler reads its own stored login, or BUTLER_API_KEY from
    the environment. Nothing is passed on the command line, where it would land in shell history
    and in the process list.

.PARAMETER Target
    itch.io target as user/game:channel. Defaults to $DefaultTarget below, which is set to this
    project's own page (edmond00.itch.io/proscribed). Override only to push somewhere else.

    The channel name is not cosmetic: itch reads the platform from it, so it must contain
    "windows" for players to be offered this build on Windows.

.PARAMETER Status
    Report the channel's current builds and exit. Uploads nothing. Good first run.

.PARAMETER UserVersion
    Version label shown on itch. Defaults to <date>-<git short sha>. Purely informational — itch
    numbers builds itself.

.PARAMETER Yes
    Skip the confirmation prompt. For unattended use.

.PARAMETER SkipSmokeTest
    Do not launch the staged build before uploading. The smoke test costs about a minute and is
    the only check that the artifact actually runs, so skip it only when re-pushing something you
    have already started once.

.EXAMPLE
    ./publish.ps1 -Status
    ./publish.ps1
    ./publish.ps1 -UserVersion 0.1.0
#>
param(
    [string]$Target,
    [string]$UserVersion,
    [string]$StageDir = "dist/ProscribedPalimpsest",
    [switch]$Status,
    [switch]$Yes,
    [switch]$SkipSmokeTest
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

# Must match $ShipName in package.ps1 and the AssemblyName in the csproj.
$ShipExe   = "ProscribedPalimpsest.exe"
$ManualPdf = "ProscribedPalimpsest-Manual.pdf"

# Named here only so the closing reminder can point at the staged copy. The manual is NOT
# uploaded by this script: butler can only replace builds in channels it owns, so it cannot
# update the PDF on the page, and pushing it as a channel would arrive as an archive instead of
# a one-click download. It stays a hand-upload; see the reminder at the end.

# ── The project this publishes to ────────────────────────────────────────────
# From the page URL edmond00.itch.io/proscribed: user "edmond00", game slug "proscribed".
# The channel is "windows" because itch reads the platform from the channel name.
$DefaultTarget = "edmond00/proscribed:windows"

function Step($text) { Write-Host "`n== $text" -ForegroundColor Cyan }
function Note($text) { Write-Host "   $text" -ForegroundColor DarkGray }
function Fail($text) { Write-Host "`nFAILED: $text" -ForegroundColor Red; exit 1 }

# ── 1. Find butler ───────────────────────────────────────────────────────────
#
# On PATH for a standalone install; under the itch app's "broth" folder when the app manages it.
# Both are normal, so look in both rather than making the user set a path.

function Find-Butler {
    $onPath = Get-Command butler -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    $brothRoots = @("$env:APPDATA\itch\broth\butler", "$env:LOCALAPPDATA\itch\broth\butler")
    foreach ($broth in $brothRoots) {
        if (-not (Test-Path $broth)) { continue }
        # Several versions can be installed side by side; take the newest.
        $exe = Get-ChildItem $broth -Recurse -Filter "butler.exe" -ErrorAction SilentlyContinue |
               Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($exe) { return $exe.FullName }
    }
    return $null
}

Step "Locating butler"
$butler = Find-Butler
if (-not $butler) {
    Fail @"
butler not found.

Install it from https://itch.io/docs/butler/installing.html, or install the itch app (which
manages a copy), then re-run. If it is installed somewhere unusual, put it on PATH.
"@
}
Note $butler

# ── 2. Check we are authenticated ────────────────────────────────────────────
#
# Two ways in, and the script needs neither to hold a secret:
#
#   butler login          one-off, interactive, opens a browser and stores a token under
#                         ~/.config/itch/butler_creds. This is what a person should use.
#   BUTLER_API_KEY        an env var holding a key from itch.io/user/settings/api-keys.
#                         For CI, where nobody can click a browser.
#
# Presence of the credentials file is not proof the token is still valid — only a real request
# tells you that, which is what -Status is for.

$credsPaths = @(
    "$env:USERPROFILE\.config\itch\butler_creds",
    "$env:APPDATA\itch\butler_creds"
)
$haveCreds = $credsPaths | Where-Object { Test-Path $_ }
$haveApiKey = -not [string]::IsNullOrWhiteSpace($env:BUTLER_API_KEY)

if (-not $haveCreds -and -not $haveApiKey) {
    Fail @"
butler is not authenticated.

Run this once, interactively:

    butler login

It opens a browser, you approve, and it stores a token. Nothing needs to be given to this script.

For an unattended machine instead, create a key at https://itch.io/user/settings/api-keys and put
it in the environment as BUTLER_API_KEY. Do not pass it as an argument to anything.
"@
}
Note $(if ($haveApiKey) { "using BUTLER_API_KEY from the environment" } else { "using stored login ($($haveCreds | Select-Object -First 1))" })

# ── 3. Resolve the target ────────────────────────────────────────────────────

if ([string]::IsNullOrWhiteSpace($Target)) { $Target = $DefaultTarget }
if ([string]::IsNullOrWhiteSpace($Target)) {
    Fail @"
No target. Pass -Target user/game:channel, or set `$DefaultTarget at the top of this script.

    user     your itch.io account name (as it appears in your page URL)
    game     the project slug
    channel  must contain "windows" - itch reads the platform from the channel name

e.g.  ./publish.ps1 -Target edmond00/proscribed:windows
"@
}
if ($Target -notmatch '^[^/]+/[^:]+:.+$') {
    Fail "Target '$Target' is not in user/game:channel form."
}
if ($Target -notmatch 'win') {
    Write-Host "   WARNING: channel name has no 'windows' in it. itch infers the platform from" -ForegroundColor Yellow
    Write-Host "            the channel, so players may not be offered this build." -ForegroundColor Yellow
}
Note "target: $Target"

# ── 4. Status mode ───────────────────────────────────────────────────────────

if ($Status) {
    Step "Channel status"
    & $butler status $Target
    if ($LASTEXITCODE -ne 0) { Fail "butler status returned $LASTEXITCODE (bad target, or the login has expired - try: butler login)" }
    exit 0
}

# ── 5. Pre-flight: never upload a broken build ───────────────────────────────
#
# A bad upload is worse than a failed script: it is live, players may fetch it, and fixing it
# means another multi-hundred-MB push. These are the same paths package.ps1 verifies.

Step "Checking the staged build"
$stage = Join-Path $root $StageDir
if (-not (Test-Path $stage)) { Fail "no staged build at '$stage'. Run ./package.ps1 first." }

$required = @(
    $ShipExe,
    "assets/fonts/FreeMono.ttf",
    "models/model.gguf",
    "models/llama/llama-server.exe",
    "models/embeddings/glove.6B.100d.txt"
)
$missing = $required | Where-Object { -not (Test-Path (Join-Path $stage $_)) }
if ($missing) {
    Fail ("the staged build is incomplete - re-run ./package.ps1:`n     " + ($missing -join "`n     "))
}

# A console-subsystem exe means package.ps1 was bypassed or -p:Ship=true was lost, and players
# would get a black diagnostic window behind the game. Cheap to check, embarrassing to ship.
$bytes = [System.IO.File]::ReadAllBytes((Join-Path $stage $ShipExe))
$peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
$subsystem = [BitConverter]::ToUInt16($bytes, $peOffset + 92)
if ($subsystem -ne 2) {
    Fail "$ShipExe is a console build (subsystem $subsystem, expected 2/GUI). Build it with ./package.ps1, which passes -p:Ship=true."
}
Note "GUI subsystem, all required files present"

$files = Get-ChildItem $stage -Recurse -File
Note ("{0:N0} files, {1:N2} GB" -f $files.Count, (($files | Measure-Object Length -Sum).Sum / 1GB))

# ── 5b. Smoke test: does the thing we are about to upload actually start? ────
#
# This is the one question development testing cannot answer. The CLI suite runs against the
# development build, which is the right place for it — gameplay is identical IL, and the shipped
# build differs only by a subsystem byte, two compile constants and a name. But none of that
# exercises the self-contained runtime, or whether the staged folder really contains everything
# the exe reaches for at startup.
#
# It runs with NO ARGUMENTS, which is what makes it survive the shipped build's locked-down option
# set: the game announces its own progress on stdout, and ConsoleAttach makes that readable because
# we launch it from here. Nothing about this needs a development flag, which is exactly why not
# having one costs nothing.
#
# What a pass proves: the runtime resolves, assets/ and models/ are found from the executable's own
# directory, the GGUF loads, a backend is chosen, llama-server answers, and an OpenGL window opens
# with the fonts loaded. That is every packaging failure mode there is.

if (-not $SkipSmokeTest) {
    Step "Smoke-testing the staged build"
    Note "launching with --cpu (packaging check; the GPU path is exercised by playing)"

    $outFile = Join-Path $env:TEMP "pp_smoke_out.txt"
    $errFile = Join-Path $env:TEMP "pp_smoke_err.txt"
    Remove-Item $outFile, $errFile -ErrorAction SilentlyContinue

    # --cpu, deliberately. The smoke test's job is to prove the PACKAGE works — that the runtime
    # resolves, assets/ and models/ are found, the GGUF loads, a window opens — and none of that
    # needs the GPU. Running inference on this machine's card has twice coincided with a hard
    # power-off, so the check that runs on every publish is the one that should not provoke it.
    #
    # The cost: the Vulkan path is not exercised here. It is exercised every time the game is
    # actually played, which is the right place for it.
    $proc = Start-Process -FilePath (Join-Path $stage $ShipExe) -ArgumentList "--cpu" -WorkingDirectory $stage `
                          -PassThru -RedirectStandardOutput $outFile -RedirectStandardError $errFile

    $sawWindow = $false
    $sawServer = $false
    $crashed = $false

    # Generous: a cold read of a 2 GB model from a slow disk, plus first-run device detection.
    foreach ($i in 1..90) {
        Start-Sleep -Seconds 2

        if (-not $sawWindow) {
            $proc.Refresh()
            if (-not $proc.HasExited -and $proc.MainWindowTitle) { $sawWindow = $true }
        }
        if (-not $sawServer -and (Test-Path $outFile)) {
            if (Select-String -Path $outFile -Pattern "LLM Server is ready" -Quiet -ErrorAction SilentlyContinue) {
                $sawServer = $true
            }
        }
        if ($sawWindow -and $sawServer) { break }

        if ($proc.HasExited) { $crashed = $true; break }
    }

    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
    # llama-server is a child process and does not go away with a killed parent.
    Get-Process -Name "llama-server" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    if (-not ($sawWindow -and $sawServer)) {
        Write-Host "`n   --- last 25 lines of output ---" -ForegroundColor DarkGray
        Get-Content $outFile -Tail 25 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "   $_" -ForegroundColor DarkGray }
        Get-Content $errFile -Tail 10 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "   $_" -ForegroundColor DarkGray }
        $why = if ($crashed) { "it exited on its own (code $($proc.ExitCode))" }
               elseif (-not $sawWindow) { "no window ever opened" }
               else { "the language model server never became ready" }
        Fail "the staged build did not start: $why. Nothing has been uploaded."
    }

    $title = "unknown"
    try { $title = (Get-Content $outFile -ErrorAction SilentlyContinue | Select-String "Using model" | Select-Object -First 1).ToString().Trim() } catch { }
    Note "window opened, llama-server ready"
    if ($title -ne "unknown") { Note $title }
}

# Directories the game creates for itself when it runs — including during the smoke test above,
# which is why this comes after it.
#
#   logs/            LLM session transcripts. Useless to a player and containing the full text of
#                    everything the model was asked and answered.
#   catalyst-models/ Catalyst's on-disk model store, from AppDomain.BaseDirectory. Always empty,
#                    because the English models are embedded in the NuGet package and nothing is
#                    ever downloaded into it. The game recreates it on first run either way.
#
# Both reappear every time anyone runs the staged build, so this cleanup is not one-time tidying:
# without it, whether they ship depends on whether the last person happened to launch the game.
#   log.txt          the per-run log GameLog writes beside the executable. Created by the smoke
#                    test above, and the reason a published build once shipped with 131 KB of
#                    someone else's session in it.
foreach ($generated in @("logs", "catalyst-models", "log.txt")) {
    $path = Join-Path $stage $generated
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
        Note "removed $generated (created by running the game)"
    }
}

# log-crash-<stamp>.txt — a copy of log.txt taken when a phase failed, so a tester's evidence
# survives the next launch. Named per occurrence rather than fixed, so it needs a wildcard; shipping
# one would be shipping the whole session it copied, which is the exact fault log.txt is deleted for.
Get-ChildItem -Path $stage -Filter "log-crash-*.txt" -File -ErrorAction SilentlyContinue | ForEach-Object {
    Remove-Item $_.FullName -Force
    Note "removed $($_.Name) (a crash report from running the game)"
}

# ── 6. Version label ─────────────────────────────────────────────────────────

if ([string]::IsNullOrWhiteSpace($UserVersion)) {
    $sha = (& git -C $root rev-parse --short HEAD)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sha)) { $sha = "nogit" }
    $UserVersion = "{0}-{1}" -f (Get-Date -Format "yyyyMMdd"), $sha.Trim()
}
Note "version: $UserVersion"

# ── 7. Confirm ───────────────────────────────────────────────────────────────
#
# Publishing is public and cannot be quietly undone: itch keeps the build, and players on the app
# start downloading it. Worth one keystroke.

if (-not $Yes) {
    Write-Host "`n   About to publish to $Target as $UserVersion." -ForegroundColor Yellow

    # Read-Host throws outright in a non-interactive session rather than returning nothing, so
    # without this a scripted run ends in a stack trace instead of a decision. Cancelling is the
    # right default when nobody can answer: -Yes is how an unattended run says it meant it.
    $answer = $null
    try { $answer = Read-Host "   Type 'yes' to continue" }
    catch {
        Write-Host "   Cancelled: nothing can answer the prompt in a non-interactive session." -ForegroundColor Yellow
        Write-Host "   Re-run with -Yes to publish without confirmation." -ForegroundColor Yellow
        exit 0
    }

    if ($answer -ne "yes") { Write-Host "   Cancelled."; exit 0 }
}

# ── 8. Push ──────────────────────────────────────────────────────────────────

Step "Uploading"
Note "butler resumes interrupted uploads and only sends changed files; re-running after a"
Note "network drop is safe and cheap."

# Not redirected with 2>&1: in Windows PowerShell 5.1 that turns a native command's progress
# output into ErrorRecords and, under ErrorActionPreference=Stop, aborts a perfectly good upload.
& $butler push $stage $Target --userversion $UserVersion
if ($LASTEXITCODE -ne 0) { Fail "butler push returned $LASTEXITCODE" }

Step "Published"
& $butler status $Target

Write-Host "`n   itch processes the build for a minute or two before it is downloadable." -ForegroundColor DarkGray
Write-Host "   Set the build live on your project page if the channel is not already published." -ForegroundColor DarkGray

# ── The manual is the one thing this script cannot do for you ────────────────
#
# butler only ever replaces builds in channels it owns, so it cannot update a file uploaded
# through the itch web form — and a channel push arrives as an archive rather than a one-click
# PDF, which is a worse page for a document. So the manual stays a manual step, and this is the
# reminder that it does. package.ps1 has already rebuilt the PDF from docs/manual/*.md and staged
# the copy named below, so the file to upload is sitting there, current, and identical to the one
# inside the game folder.

$manual = Join-Path $stage $ManualPdf
if (Test-Path $manual) {
    $age = (Get-Item $manual).LastWriteTime
    Write-Host "`n   ── Still to do by hand ──────────────────────────────────────────" -ForegroundColor Yellow
    Write-Host "   Upload the manual on the project page, replacing the previous $ManualPdf." -ForegroundColor Yellow
    Write-Host "   The current one was rebuilt at $($age.ToString('HH:mm')) and is here:" -ForegroundColor Yellow
    Write-Host "     $manual" -ForegroundColor Yellow
    # The dashboard, not a constructed edit URL: itch keys project edit pages by numeric id, not
    # by slug, so a link built from the target would 404.
    Write-Host "   Uploads are edited from https://itch.io/dashboard" -ForegroundColor DarkGray
}
