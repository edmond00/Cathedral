# Linux port — notes

Running log of the work described in `PORT-BRIEF.md`. Newest information is kept in place per
step; open questions for the developer are collected at the end.

## Environment

- Cloud session, Ubuntu (Linux 6.18 kernel), no GPU, no sound device.
- .NET SDK 8.0.131 from apt (`cloud-setup.sh`).
- **Pushing was refused** at the start of the session: the git proxy has no write credential for
  `edmond00/cathedral` (the Claude GitHub App is not installed on it / GitHub not linked). Work is
  committed locally on `linux-port` and pushed once access works; any commit not yet on
  `origin/linux-port` when the session ends is listed in the final summary.
- `cloud-setup.sh` failed at the llama.cpp step on its first run: in a cloud session the GitHub
  REST API is gated per repository, so `api.github.com/repos/ggml-org/llama.cpp/releases/...`
  returned an access message instead of the release, and `jq` choked on it. The release asset
  itself downloads fine from `github.com/.../releases/download/`. Fixed in the script (falls back
  to the conventional asset name), see step 1.

## Step 1 — Baseline

- `dotnet build -p:EnableWindowsTargeting=true` on the unmodified `develop` tree: **succeeds**,
  0 errors, 40 warnings (all nullable / unused-field warnings, the same kind Windows shows).
- `.gitattributes` added with `*.sh text eol=lf`. `git add --renormalize .` changed nothing: the
  four `.sh` files were already LF in the index.

## Step 2 — Project target

- `Cathedral.csproj`: a `CathedralWindows` property decides the target. With a runtime identifier
  it is `RID.StartsWith('win')`; without one it is the build host's OS. So a bare `dotnet build` on
  Windows is still `net8.0-windows` (output `bin/Debug/net8.0-windows/`, as before), and on Linux
  it is `net8.0`.
- Windows only: `Microsoft.Msagl.GraphViewerGdi`, the WinForms `FrameworkReference`,
  `EnableUnsafeBinaryFormatterSerialization` (it exists for GViewer), an explicit `WINDOWS` define
  (the SDK already sets it for `-windows` TFMs), and `EnableWindowsTargeting` so Linux can build
  `-r win-x64`. `Microsoft.Msagl` / `.Drawing` stay on both: they are netstandard and harmless.
- `src/debug/*Window.cs` are wrapped whole in `#if WINDOWS`. The four managers
  (`DialogueViewLauncher`, `LlmMonitorDebugManager`, `SceneDebugManager`,
  `WorldHistoryViewerManager`) keep their Windows body under `#if WINDOWS` and get an `#else` stub
  with the same public methods; off Windows, asking for a window prints once per window per process
  (`src/debug/PlatformNotice.cs`, itself `#if !WINDOWS`):
  `[debug] The dialogue viewer is not available on this platform: it is a WinForms window, Windows only. --dialogue-audit prints the text report.`
  No developer key opens these windows; they open from `--dialogue-view`, and automatically when
  viewers are on (`--view`, `--debug` without `--hidden`).
- **A RID build must name the project**: `dotnet build Cathedral.csproj -r linux-x64`. The SDK
  refuses `-r` on a solution (NETSDK1134), and the folder holds `Cathedral.sln`. A bare
  `dotnet build` still goes through the solution as before.

Results here:

| command | result |
|---|---|
| `dotnet build Cathedral.csproj -r linux-x64` | OK, 0 errors → `bin/Debug/net8.0/linux-x64/` |
| `dotnet build Cathedral.csproj -r win-x64` | OK, 0 errors → `bin/Debug/net8.0-windows/win-x64/` |
| `dotnet build` (Linux, bare) | OK → `bin/Debug/net8.0/` |
| `dotnet build -p:CathedralWindows=true` (what a bare Windows build evaluates to) | OK → `bin/Debug/net8.0-windows/` |
| `--dialogue-view` on Linux | prints the notice, exits 0 |

The Windows assembly still contains all four window types; the Linux one contains none.

## Step 3 — First headless run

What a Linux run hit, and the fix for each:

- **`run_tests.sh` and `tests/save_reload.sh` were not executable** in git (mode 100644: a Windows
  checkout never records the bit). Set to 100755 with `git update-index --chmod=+x`; Windows ignores
  the bit, so nothing changes there.
- **`run_tests.sh` guard and cleanup** went through `powershell.exe Get-Process`. Now two functions,
  `game_count` / `kill_games`, branch on `uname -s`: `MINGW*|MSYS*|CYGWIN*` (Git Bash on Windows)
  runs the **identical** PowerShell commands as before; anything else uses `pgrep -x Cathedral` /
  `pkill -KILL -x Cathedral`. The guard's semantics and comments are kept: refuse to start if a
  `Cathedral` process exists, so any alive at exit is the suite's own. A WSL shell takes the Linux
  path, which is right: the suite it runs builds a Linux binary.
- **`CLI_TIMEOUT`** is overridable from the environment (`CLI_TIMEOUT=400 ./run_tests.sh cli`);
  the default stays 200.
- **`CLI_EXTRA_FLAGS`** (new, empty by default): extra flags appended to every CLI script's command
  line. Exists for `--no-embeddings`, below.
- **`UiSfxPlayer` / `BrownNoiseStreamer`** P/Invoke `winmm.dll`, and `AmbianceEngine`'s constructor
  builds a `UiSfxPlayer` even under `--silent` — so every launch died with `DllNotFoundException:
  winmm.dll`. Both are now inert off Windows (`OperatingSystem.IsWindows()` guards around the WinMM
  calls). This is the stopgap until step 5; on Windows the calls are exactly as before.
- **`CrashReport.DetectWine()`** returns null off Windows before touching `kernel32`, and the
  report's Wine line says `no (a native build; the OS line above is the host)` there instead of
  "not detected (running on Windows)". Windows output unchanged.
- **`RecordMode.FindFfmpeg`** looked for `ffmpeg.exe` on PATH; off Windows it now looks for
  `ffmpeg` (outside the brief's list, but `--record` could not find ffmpeg on Linux at all).
- **The GloVe vectors are required even under `--playground`.** The brief assumed not: in fact
  `WordEmbedding.InitializeAsync` exits the process when `models/embeddings/glove.6B.100d.txt` is
  missing, whatever the mode. In this sandbox the file cannot be fetched at all — the network
  policy denies `nlp.stanford.edu`, `downloads.cs.stanford.edu` and `huggingface.co` (CONNECT 403,
  "organization policy"). Added **`--no-embeddings`**, a debug flag inert at its default: with it a
  missing file is a log line instead of an exit, keyword ranking takes the word-length fallback a
  playground run already takes before the vectors finish loading, and the CLI's `wait embeddings`
  returns at once (saying so). Documented in `--help` and on `Config.Debug.NoEmbeddings`.
  The same flag is what the stretch CI job (no model download) will need.
  Note `fetch-assets.sh` reported success here although curl had failed: run through `| tail` the
  pipeline's status was tail's. Run it bare, or check for the file afterwards.
- **No case-sensitivity or backslash problems found so far.** A search of C# string literals turned
  up no backslash paths (only the `%APPDATA%\Cathedral` help text); the backslashes in `assets/art/`
  are ASCII art. The suite run is the real test of case mismatches.
- `error: XDG_RUNTIME_DIR is invalid or not set in the environment.` is printed by GLFW/Mesa under
  Xvfb as root and is harmless.

Results:

| run | result |
|---|---|
| `dotnet run -- --help` | OK, exit 0 |
| `./run_tests.sh audits` | **12/12 pass** (crime, mm, verb, building, dialogue, npc, item, outcome, save, world-variant, history, demo). 20 min 29 s on 4 vCPU — verb and building take most of it |
| `cli/verb/examine/success.cli` under Xvfb, `--no-embeddings` | pass, 14 s |
