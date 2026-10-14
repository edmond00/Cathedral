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
