<#
.SYNOPSIS
    Checks models/ against what the repository says it should contain.

.DESCRIPTION
    The binaries under models/ are deliberately not in git (see models/README.md), so nothing
    propagates them between machines and nothing notices when one falls behind. This is what
    notices.

    The split it checks is the whole point: BUILD.txt is TRACKED and states which llama.cpp build
    a toolchain should be, while the binaries beside it are UNTRACKED and are whatever that
    machine happens to have. A git pull moves the declaration and not the reality, so a stale
    machine reads "b8851" out of a committed file while its own llama-server reports 8746 — and
    will happily build a release that way. Comparing the two is a second's work and is the only
    thing standing between that and a shipped package nobody can explain.

    Exit code is the number of problems, so it can gate a release or a CI step.

.PARAMETER Fix
    Print the exact commands that would repair each problem, rather than only naming it. Nothing
    is downloaded or modified either way — a 2 GB fetch is not something a verifier should decide
    to do on your behalf.

.EXAMPLE
    ./tools/verify_models.ps1
    ./tools/verify_models.ps1 -Fix
#>
param(
    [switch]$Fix
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$models = Join-Path $root "models"

$problems = 0
$fixes = @()

$warnings = 0

function Ok($t)    { Write-Host "  OK    $t" -ForegroundColor DarkGreen }
function Bad($t)   { Write-Host "  FAIL  $t" -ForegroundColor Red;    $script:problems++ }
function Warn($t)  { Write-Host "  WARN  $t" -ForegroundColor Yellow; $script:warnings++ }
function Head($t)  { Write-Host "`n== $t" -ForegroundColor Cyan }

# ── What the repository declares ─────────────────────────────────────────────
#
# Kept here rather than parsed out of models/README.md: a verifier that reads its expectations
# from the same prose it is checking can only ever agree with itself. These are duplicated on
# purpose, and the duplication is the check.
$Expected = @(
    @{ Path = "model.gguf"
       Bytes = 2104932768
       Sha   = "626b4a6678b86442240e33df819e00132d3ba7dddfe1cdc4fbb18e0a9615c62d"
       Url   = "https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf" }

    @{ Path = "embeddings/glove.6B.100d.txt"
       Bytes = 347116733
       Sha   = "95dde4dfd627ab26608d33e76d1195ec059734bd29089ea52cadb08d07c64544"
       Url   = "https://nlp.stanford.edu/data/glove.6B.zip"
       # The 400,001-line copy carrying an appended <unk> vector is also in circulation and is
       # functionally identical — nothing looks <unk> up. Reported, never failed.
       AltBytes = 347117594
       AltNote  = "the 400,001-line variant with an appended <unk> vector; inert, nothing reads it" }
)

# Toolchains: folder, the architecture its llama-server.exe must be, and whether it needs the
# fourteen x86 microarchitecture variants. ARM64 has no such fan-out and ships one ggml-cpu.dll.
$Toolchains = @(
    @{ Folder = "llama";       Machine = 0x8664; Arch = "x64";   IsaVariants = $true  }
    @{ Folder = "llama-arm64"; Machine = 0xAA64; Arch = "arm64"; IsaVariants = $false }
)

Write-Host "Verifying $models" -ForegroundColor White

# ── 1. The payload files ─────────────────────────────────────────────────────

Head "Payload files"

foreach ($e in $Expected) {
    $path = Join-Path $models $e.Path
    if (-not (Test-Path $path)) {
        Bad "$($e.Path) is missing"
        $fixes += "# fetch $($e.Path):`n#   $($e.Url)"
        continue
    }

    $len = (Get-Item $path).Length

    # Size first: it is instant, and hashing 2 GB to discover a truncated download is a waste of
    # thirty seconds when the length already said so.
    if ($len -ne $e.Bytes) {
        if ($e.AltBytes -and $len -eq $e.AltBytes) {
            Warn "$($e.Path) is $len bytes — $($e.AltNote)"
            continue
        }
        Bad "$($e.Path) is $len bytes, expected $($e.Bytes)"
        $fixes += "# re-fetch $($e.Path):`n#   $($e.Url)"
        continue
    }

    $sha = (Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($sha -ne $e.Sha) {
        Bad "$($e.Path) hash mismatch`n          got      $sha`n          expected $($e.Sha)"
        $fixes += "# re-fetch $($e.Path):`n#   $($e.Url)"
    } else {
        Ok "$($e.Path)  ($len bytes, sha256 verified)"
    }
}

# ── 2. The llama.cpp toolchains ──────────────────────────────────────────────
#
# The check that actually catches machine drift: BUILD.txt is committed, the binaries are not.

Head "llama.cpp toolchains"

foreach ($t in $Toolchains) {
    $dir = Join-Path $models $t.Folder
    if (-not (Test-Path $dir)) {
        Bad "$($t.Folder)/ is missing — see $($t.Folder)/BUILD.txt in git for which build to fetch"
        continue
    }

    $server = Join-Path $dir "llama-server.exe"
    if (-not (Test-Path $server)) { Bad "$($t.Folder)/llama-server.exe is missing"; continue }

    # Architecture, from the PE header. A folder named -arm64 holding x64 binaries would satisfy
    # every other check here and be wrong on exactly the machines it exists for.
    $fs = [IO.File]::OpenRead($server)
    try {
        $br = New-Object IO.BinaryReader($fs)
        $fs.Position = 0x3C
        $fs.Position = $br.ReadInt32() + 4
        $machine = $br.ReadUInt16()
    } finally { $fs.Close() }

    if ($machine -ne $t.Machine) {
        Bad ("$($t.Folder)/llama-server.exe is machine 0x{0:X}, expected 0x{1:X} ({2})" -f $machine, $t.Machine, $t.Arch)
        continue
    }

    # Declared build (committed) versus actual build (not committed). The drift check.
    $buildFile = Join-Path $dir "BUILD.txt"
    $declared = $null
    if (Test-Path $buildFile) {
        $m = Select-String -Path $buildFile -Pattern '\bb(\d{3,6})\b' | Select-Object -First 1
        if ($m) { $declared = $m.Matches[0].Groups[1].Value }
    } else {
        Bad "$($t.Folder)/BUILD.txt is missing — it is tracked by git, so this folder is not what the repo expects"
        continue
    }

    # llama-server prints "version: 8746 (0893f50f2)" and exits — on STDERR, which is why this
    # goes through ProcessStartInfo rather than `& $server --version 2>&1`. In Windows PowerShell
    # 5.1 that redirection wraps every stderr line in a NativeCommandError, which under
    # ErrorActionPreference=Stop throws even though the exe exited 0. Reading the streams directly
    # is what LlamaProbe.RunCaptured does, for the same reason.
    $actual = $null
    try {
        $psi = New-Object Diagnostics.ProcessStartInfo
        $psi.FileName               = $server
        $psi.Arguments              = "--version"
        $psi.UseShellExecute        = $false
        $psi.CreateNoWindow         = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError  = $true
        $proc = [Diagnostics.Process]::Start($psi)
        $out  = $proc.StandardOutput.ReadToEnd() + $proc.StandardError.ReadToEnd()
        $proc.WaitForExit()
        if ($out -match 'version:\s*(\d+)') { $actual = $Matches[1] }
    } catch { }

    if (-not $actual) {
        Warn "$($t.Folder): could not read the binary's version (it may not run on this host — $($t.Arch))"
    } elseif ($declared -ne $actual) {
        Bad "$($t.Folder): BUILD.txt declares b$declared but the binary is b$actual — this folder is stale relative to the repo"
        $fixes += "# $($t.Folder) is b$actual, repo expects b$declared. Re-fetch the upstream zip named in`n#   models/$($t.Folder)/BUILD.txt, or update BUILD.txt if b$actual is intended."
    } else {
        Ok "$($t.Folder)  (b$actual $($t.Arch), matches BUILD.txt)"
    }

    if ($t.IsaVariants) {
        $n = (Get-ChildItem $dir -Filter "ggml-cpu-*.dll" -ErrorAction SilentlyContinue).Count
        if ($n -lt 10) { Bad "$($t.Folder): only $n ggml-cpu-*.dll; expected the full ISA set" }
        else { Ok "$($t.Folder): $n CPU ISA variants" }
    }

    # GPU backends are informational — none is a valid, supported configuration.
    $backends = Get-ChildItem (Join-Path $dir "backends") -Directory -ErrorAction SilentlyContinue |
                Where-Object { (Get-ChildItem $_.FullName -Filter "ggml-*.dll" -ErrorAction SilentlyContinue).Count -gt 0 }
    if ($backends) {
        Write-Host "        GPU backends: $(($backends | ForEach-Object { $_.Name }) -join ', ')" -ForegroundColor DarkGray
        foreach ($b in $backends) {
            # A backend from a different revision than the ggml-base.dll beside it crashes inside
            # the backend with no usable diagnostic, and the DLLs carry no version resource — so
            # the folder's own BUILD.txt is the only thing that can be checked.
            Write-Host "          $($b.Name): must be b$declared (nothing can verify this by reading the DLL)" -ForegroundColor DarkGray
        }
    } else {
        Write-Host "        GPU backends: none (CPU only — supported)" -ForegroundColor DarkGray
    }
}

# ── 3. What git thinks ───────────────────────────────────────────────────────

Head "git"

Push-Location $root
try {
    # The documentation files are the only things under models/ that git carries. If one is
    # modified, this machine changed a declaration and may not have changed the binaries with it.
    $dirty = git status --porcelain -- models/ 2>$null
    if ($dirty) {
        Warn "uncommitted changes under models/:"
        $dirty -split "`n" | Where-Object { $_ } | ForEach-Object { Write-Host "          $_" -ForegroundColor Yellow }
    } else {
        Ok "models/ documentation matches HEAD"
    }
} finally { Pop-Location }

# ── Result ───────────────────────────────────────────────────────────────────

Write-Host ""
if ($problems -eq 0) {
    $suffix = if ($warnings) { " ($warnings warning(s) above — none of them block a release)" } else { "" }
    Write-Host "models/ is consistent with the repository.$suffix" -ForegroundColor Green
} else {
    Write-Host "$problems problem(s) found." -ForegroundColor Red
    if ($Fix -and $fixes) {
        Write-Host "`nSuggested repairs (nothing was downloaded or modified):" -ForegroundColor Cyan
        $fixes | ForEach-Object { Write-Host $_ -ForegroundColor DarkGray }
    } elseif ($fixes) {
        Write-Host "Re-run with -Fix to print the repair commands." -ForegroundColor DarkGray
    }
}

exit $problems
