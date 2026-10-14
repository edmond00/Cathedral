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
| **`CLI_TIMEOUT=400 CLI_EXTRA_FLAGS=--no-embeddings xvfb-run -a ./run_tests.sh cli`** | **193/193 pass**, 0 failed, ~53 min on 4 vCPU (~15 s per script). Includes the two scripts that `wait embeddings` |

No file-name case mismatches or backslash paths turned up anywhere in the suite. The default 200 s
`CLI_TIMEOUT` would also have done: no script came near it.

## Step 4 — llama.cpp on Linux

- `LlamaRuntime`: `ServerExecutableName` / `BenchExecutableName` are `static readonly` now (were
  `const`; nothing used them in a constant context), `llama-server.exe` / `llama-bench.exe` on
  Windows and `llama-server` / `llama-bench` elsewhere. Backend discovery looks for `ggml-*.dll` on
  Windows and `libggml-*.so` elsewhere, with the non-backend prefixes (`cpu`, `base`, `rpc`)
  prefixed `lib` to match. Linux resolves `models/llama-linux` — and, unlike ARM64, never falls back
  to `models/llama`, since a Windows executable cannot run there. `ApplyBackend` also prepends the
  backend folder to `LD_LIBRARY_PATH` off Windows (the Linux twin of the `PATH` prepend).
  `LlamaRuntime.ToolchainLabel` (`x64` / `arm64 native` / `linux x64`) feeds the startup line and the
  probe report. `LlamaInstallCheck` hashes whatever `DiscoverBackends` returns, so it needed only
  its message to name the real folder instead of `models/llama`.
- `llama-server` carries `RUNPATH=$ORIGIN` and finds its `.so` files from its own folder with no
  `LD_LIBRARY_PATH`. A backend pack in `backends/vulkan/` resolves `libggml-base.so.0` from the
  server, which has already loaded it.
- **Pruning** is done by `cloud-setup.sh` itself, so every machine gets the same folder: the `ldd`
  closure of `llama-server` + `llama-bench` that lives in the folder (with symlink targets), plus
  the 14 `libggml-cpu-*.so`, `LICENSE` and `BUILD.txt` — 30 entries, 41 MB. `BUILD.txt` is written
  deterministically (kept list + SHA-256 of every file) and is tracked; re-running setup rewrites it
  byte-for-byte. System libraries left to the OS: libstdc++6, libgomp1, libssl3.
- `.gitignore` mirrors the ARM64 block for `models/llama-linux/` (BUILD.txt and
  `backends/*/BUILD.txt` tracked, the rest ignored). `models/README.md` table has a `llama-linux/`
  row.
- **Vulkan**: `cloud-setup.sh --vulkan` fetches `libggml-vulkan.so` from upstream's Ubuntu Vulkan
  build into `models/llama-linux/backends/vulkan/` with a tracked `BUILD.txt` (build + hash), and
  installs `libvulkan1` + `mesa-vulkan-drivers`. There is no GPU here; Mesa's **lavapipe** (software
  Vulkan) gives one device, which llama.cpp hides as CPU-type unless `GGML_VK_VISIBLE_DEVICES=0`.
  With that set, the probe's two-candidate path runs. **Real Vulkan on a real GPU is untested.**

### The LLM run — with a stand-in model

The real model **cannot be fetched here**: `huggingface.co` is denied by the network policy, like
the GloVe hosts. To exercise the Linux plumbing anyway, I generated a **1-layer random-weight llama
GGUF** (21 MB, 40k-token vocabulary taken from `models/en_scowl_40.txt`, ChatML template) with the
`gguf` Python package and put it at `models/model.gguf` (gitignored; not committed). It produces
gibberish, so this proves the process and HTTP plumbing, **not narration quality**:

| check | result |
|---|---|
| `--llm-probe-audit`, CPU only | "CPU (no GPU backend installed)", no bench needed — correct |
| `--llm-probe-audit` with the Vulkan pack + `GGML_VK_VISIBLE_DEVICES=0` | `llama-bench` ran for CPU and for `vulkan — llvmpipe`, table printed, CPU chosen by the usual rule |
| real (non-playground) run, `--cpu`, to `LocationInteraction` | server started from `models/llama-linux`, loaded, `Using model: tiny-random-test (llama.cpp b11515 (linux x64); backends installed: vulkan)`; first observation request streamed a full reply (`done=y eof=y`, 802 chars); the next failed with HTTP 400 *context exceeded*, because the random model's gibberish is fed back into the next prompt — a property of the stand-in, not of the port |
| install check | only "model.gguf is tiny-random-test; this game expects qwen2.5-3b-instruct" — the server build check and the Vulkan pack's hash both pass |
| game quits normally | "Stopping llama server...", no `llama-server` left |
| `SIGTERM` to the game | server gone (`ProcessExit` hook runs) |
| `SIGKILL` to the game | server **orphaned** (reparented to init). Same as a hard kill on Windows, where there is no job object either; not changed. A Linux fix would be `prctl(PR_SET_PDEATHSIG)` in the child, which `ProcessStartInfo` cannot express — would need a tiny wrapper |

**What the developer should still do with the real model** (or I can, given network access to
huggingface.co): `bash tools/linux/fetch-assets.sh --glove --model`, then one `--cpu` run to a
location, and read the narration.

## Step 5 — Audio

- **`src/audio/AudioOutput.cs`**: `AudioBackend { WinMM, OpenAL }`, three small interfaces —
  `IUiSfx` (hover/click), `INoiseStream` (the brown-noise wash), `IMidiOut` (where the music's MIDI
  goes) — and `AudioOutput`, which picks the backend once per process and builds them.
  `AmbianceEngine` now asks `AudioOutput` for its three outputs and otherwise does not know which
  path it is on. **Windows defaults to WinMM**: `UiSfxPlayer` and `BrownNoiseStreamer` are the same
  classes as before (they now implement the interfaces), and `DeviceMidiOut` is the old
  `OpenBestDevice` lifted out of the engine verbatim (GS Wavetable preferred, else the first device).
- **`src/audio/OpenAlAudio.cs`**: one shared OpenAL Soft device/context opened on first need (via
  `OpenTK.Audio.OpenAL`, already a dependency); `OpenAlUiSfx` plays the very same tick samples from
  buffers through four rotating sources; `OpenAlNoiseStream` loops WinMM's two noise blocks back to
  back; `SoundFontMidiOut` renders the engine's MIDI with **MeltySynth 2.4.1** (MIT, pure C#,
  netstandard2.1, new `PackageReference`) and streams it into one OpenAL source — 512-frame blocks,
  4 queued, ~50 ms latency, one lock around the synth because the engine sends from several threads.
  Every failure (no `libopenal.so.1` / `openal32.dll`, no device, no SoundFont) is a log line and
  silence, the same state as a machine with no MIDI device.
- The PCM generators were refactored so both paths share the samples (`UiSfxPlayer.HoverSamples` /
  `ClickSamples`, `BrownNoiseStreamer.BrownNoiseSamples`). **`--export-sfx` output is byte-identical
  before and after** (click `cb36a1e6…`, hover `7f7c2305…`), so Windows plays the same ticks. The
  noise refactor is the same arithmetic moved into a function.
- **Flags**: `--audio winmm|openal` (Windows: default winmm; elsewhere openal is the only path and
  `winmm` says so and falls back) and `--soundfont <file>` (for comparing fonts). Both inert by
  default, both in `--help`. `--silent` still opens nothing on either path.
- The SoundFont is loaded from **`assets/audio/soundfont.sf2`** (cwd first, then up from the
  executable), gitignored; provenance and the candidates are in `assets/audio/README.md`.
- One behaviour difference on Windows, failure case only: `PrepareForEventsSending` used to run
  outside `OpenBestDevice`'s try/catch, so a device that opened but could not prepare would have
  thrown out of `AmbianceEngine.Start`; now it is inside, and means "no music". Not a path any
  machine is known to take.

Verified here (no sound card; "audible" was not checkable, "not silent" was):

| check | result |
|---|---|
| playground script, audio on, OpenAL Soft's `wave` driver writing the mix to a file | OpenAL opened, `music through SoundFont soundfont.sf2 (MeltySynth, OpenAL)`, script passes, 16 s capture with ticks peaking at 0.3 FS |
| `--music` (engine alone) through the same capture | music present, RMS 0.003–0.007 (~−45 dBFS). The engine plays very softly by design — `--export-music` shows note velocities 5–48, mean 23 — so how loud this should sit against the GS synth is for the A/B |
| `--soundfont /nonexistent.sf2` | "no such file", "no music output", script passes |
| `dotnet build -r win-x64` | 0 errors; `MeltySynth.dll` and `OpenTK.Audio.OpenAL.dll` in the output |

**On Windows, `--audio openal` needs OpenAL Soft**: OpenTK loads `openal32.dll` and does not ship
it. Drop OpenAL Soft's `soft_oal.dll` (from openal-soft.org's Windows binaries, LGPL-2.1), renamed
`openal32.dll`, beside the game, plus `assets/audio/soundfont.sf2`. Without it the flag gives a
clear log line and silence.

## Step 6 — Docker

- `tools/linux/Dockerfile`: Ubuntu 24.04 (`ARG BASE_IMAGE`, default `ubuntu:24.04`), then
  **`cloud-setup.sh --no-llama`**, so the package list lives in one place. `--no-llama` is new: packages
  and SDK only, since `models/` is mounted rather than baked in. Then `tini`, a restore layer, the
  source, and `dotnet build`. Entry point `tools/linux/docker-entrypoint.sh`: `xvfb-run -a
  ./run_tests.sh "$@"` (all by default), `CLI_TIMEOUT=400` unless set, and `--no-embeddings` added
  only when the mounted `models/` has no GloVe file.
- `.dockerignore` at the root: `bin/ obj/ dist*/ models/ logs/ local/ cache/`, plus `.git/`,
  `DEPRECATED/`, the video venv and `*.sf2`. Context is ~48 MB.
- `tools/linux/README.md`: the exact `docker build` / `docker run` commands for a Linux/WSL2 shell
  and for **Windows PowerShell with Docker Desktop** (`-v "${PWD}\models:/src/models"`), log
  mounting, the options, and the one-suite-per-machine caveat below.
- `tools/linux/extra-ca/` (gitignored but for `.gitkeep`): CA certificates copied into the image, for
  building behind a TLS-intercepting proxy. Empty by default, and then it changes nothing.

Found while making it work here:

- **Docker Hub rate-limited this sandbox** (`429 Too Many Requests`), hence `BASE_IMAGE`;
  `mirror.gcr.io/library/ubuntu:24.04` is the same image. The build here used it, with
  `--network host`, the proxy as build args, and the proxy CA in `extra-ca/`.
- **`xvfb-run` as PID 1 hangs** before running anything: it waits for Xvfb's SIGUSR1, which a PID 1
  does not get. `tini` is PID 1 now, and it also forwards `docker stop` to the suite's trap.
- **A suite on a Linux host kills the games in a container running alongside it.** `run_tests.sh`
  does `pkill -x Cathedral` after every script, and the host sees the container's processes. The
  first container smoke runs died with SIGKILL (exit 137) for exactly this reason while the host
  suite was running. Documented in the README; the guard itself is unchanged. It is right for one
  machine, and the conflict needs a Linux host plus a container running at the same time.

| check | result |
|---|---|
| `docker build` (mirror base, proxy args) | OK, ~3.5 min cold |
| `docker run ... cli examine`, run alone | (see the full run below) |

## Open decisions for the developer

1. **The SoundFont** (step 5). Candidates with size, licence and SHA-256 in `assets/audio/README.md`:
   GeneralUser GS 2.0.3 (32 MB, own permissive licence with a sample-provenance caveat; suggested),
   FluidR3_GM 3.1 (148 MB, MIT), TimGM6mb 1.3 (6 MB, GPL-2). Which one ships, and whether the
   GeneralUser caveat is acceptable, is yours.
2. **Whether to unify audio on Windows** (step 5): keep WinMM + GS Wavetable as the default, or move
   Windows to OpenAL + the SoundFont too. A/B with `--audio openal` (needs `openal32.dll`, see
   step 5). Unifying would also mean shipping OpenAL Soft (LGPL-2.1) in the Windows build.
3. **`--no-embeddings`** (step 3): I added it because the GloVe file cannot be fetched here and the
   game exits without it even in playground. Keep it as a CI/container convenience, or would you
   rather the suite always ran with real vectors (which means a 822 MB fetch in every CI run)?
