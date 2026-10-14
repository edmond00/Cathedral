# Linux port — brief for a cloud session

You are working in a Linux sandbox on a game that has, until now, only ever been built and run on
Windows. The developer works on Windows and **cannot see this session's machine**: everything you
find out has to reach them through commits and the notes file described at the end.

Read `CLAUDE.md` first. Its six prohibitions hold here as everywhere — in particular **never
enable trimming** (a self-contained Linux publish will tempt you to) and **shader sources stay
ASCII**.

## Goal

A self-contained `linux-x64` build of the game that starts, renders, plays its audio and runs its
LLM, **without changing anything a Windows player experiences**, plus a Docker image that runs the
headless test suite against it.

## Ground rules

- Work on the branch `linux-port` (already created from `develop`; check it out if the session
  did not start on it). Commit in small, single-purpose commits and push after each one, so
  progress survives the session. Never push to `main` or `develop`, never
  run the `release` skill, never touch itch.io or butler.
- **The Windows build must keep compiling and behaving the same.** You cannot run it here; you can
  compile it (`dotnet build -r win-x64` after the target change, or
  `dotnet build -p:EnableWindowsTargeting=true` before it). Do so after every change that touches
  shared code, and say in the notes what the developer has to check by hand on Windows.
- Gate platform code on `OperatingSystem.IsWindows()` / `IsLinux()` at runtime, or on a
  `WINDOWS` compile constant where the code cannot even compile elsewhere (WinForms). Do not fork
  files when a guard will do.
- No rule a player is subject to changes, so the manual is not touched.
- Big binaries never go in git: follow the pattern in `models/README.md` and `.gitignore`
  (provenance + SHA-256 committed, the file itself fetched).

## Environment

`tools/linux/cloud-setup.sh` should already have run. If `dotnet --list-sdks` shows no 8.x, run it
yourself: `bash tools/linux/cloud-setup.sh`. It installs the SDK, Xvfb + Mesa (software OpenGL),
OpenAL, and llama.cpp's Ubuntu CPU build into `models/llama-linux/`.

The model and the GloVe vectors are not in the clone. `--playground` runs need neither the model
nor the server; fetch them only when a step actually needs them:
`bash tools/linux/fetch-assets.sh --glove` / `--model`.

The game opens a window, so every run goes through Xvfb:
`xvfb-run -a -s "-screen 0 1280x1024x24" <command>`. Set `ALSOFT_DRIVERS=null` when there is no
sound device, so OpenAL opens a null device instead of failing.

**Invoke the `verifying` skill before writing or running any `.cli` script**, and the `audits`
skill before running audits. Copy known-good `.cli` scripts from `cli/` rather than composing new
ones from the docs.

## The work, in order

Each step ends in a commit. Steps 1–3 are the core; do them before anything else.

### 1. Baseline

Before changing anything: build as-is with `dotnet build -p:EnableWindowsTargeting=true` to prove
the toolchain works, and record the result. Then add a `.gitattributes` with `*.sh text eol=lf`
(the repo relies on `core.autocrlf` today, which a Linux clone does not have).

### 2. Project target

`Cathedral.csproj` targets `net8.0-windows` and references WinForms and
`Microsoft.Msagl.GraphViewerGdi`. Only the developer debug windows use them:
`src/debug/{DialogueViewLauncher,DialogueViewWindow,LlmMonitorDebugManager,LlmMonitorWindow,
SceneDebugManager,SceneDebugWindow,WorldHistoryViewerManager,WorldHistoryWindow}.cs`.

Make the TFM depend on the runtime: `net8.0-windows` + WinForms + GraphViewerGdi + a `WINDOWS`
define for Windows, plain `net8.0` for Linux. Exclude or `#if WINDOWS` the debug windows, and make
their entry points (flags such as `--dialogue-view`, any developer key that opens one) print "not
available on this platform" instead of failing. `dotnet build` with no arguments on Windows must
still produce exactly what it does today.

Then: `dotnet build -r linux-x64` succeeds, and `dotnet build -r win-x64` still succeeds.

### 3. First headless run

Get `--help`, then the audits (`./run_tests.sh audits`), then one `--playground --cli` script
running under Xvfb. Expect, and fix as found:

- **File name case.** Linux paths are case-sensitive; Windows hid every mismatch between a path in
  code/data and the file on disk. Fix the reference to match the file, never rename assets.
- **Backslashes** in paths built by string concatenation or stored in data files.
- `run_tests.sh` guards and cleans up through `powershell.exe Get-Process`. Make it portable
  (`pgrep -x Cathedral` / `pkill` on Linux) while keeping the Windows path identical, and keep the
  guard's semantics described in its comments.
- `src/CrashReport.cs` `DetectWine()` P/Invokes `kernel32`; guard it with `IsWindows()`.

Then the full CLI suite: `xvfb-run -a ./run_tests.sh cli`. Record pass/fail counts in the notes.
Mesa's llvmpipe is slower than a GPU; raise `CLI_TIMEOUT` through an environment variable if needed
rather than editing the constant for everyone.

### 4. llama.cpp on Linux

`src/LLM/LlamaRuntime.cs` hard-codes `llama-server.exe` / `llama-bench.exe` and discovers backends
with `ggml-*.dll`; `LlamaInstallCheck.cs` hashes backend DLLs. Make the names platform-dependent
(no extension, `.so`), and have Linux select `models/llama-linux` the way ARM64 Windows selects
`models/llama-arm64`. Mirror the existing `.gitignore` rules so `models/llama-linux/BUILD.txt` is
committed and the binaries are not. Check that `llama-server` finds its `.so` files from its own
folder (it may need `LD_LIBRARY_PATH`, set on the `ProcessStartInfo`) and that it is killed when
the game exits (`LlamaServerManager.KillServerProcess`, the `ProcessExit` hook).

Prune `models/llama-linux` to the `ldd` closure of `llama-server` and `llama-bench` plus the
`libggml-cpu-*.so` variants, as `models/llama/BUILD.txt` describes for Windows, and write the kept
list into its `BUILD.txt`.

Then fetch the model and GloVe, run the first-run probe and one real, non-playground run with
`--cpu`, and confirm narration arrives. Vulkan: fetch the Ubuntu Vulkan build into
`models/llama-linux/backends/vulkan/` the same way only if it is quick; there is no GPU here to
test it, so say so in the notes.

### 5. Audio

Two Windows-only paths:

- `src/audio/UiSfxPlayer.cs` — UI sound effects through `winmm.dll` `waveOut*`.
- `src/audio/AmbianceEngine.cs` `OpenBestDevice()` — music through DryWetMidi `OutputDevice`,
  preferring the Microsoft GS Wavetable Synth. DryWetMidi devices are Windows/macOS only, and Linux
  has no system General MIDI synth at all.

Introduce an audio output abstraction. The Linux implementation: OpenAL via OpenTK
(`OpenTK.Audio.OpenAL`, already a dependency through the OpenTK package) for PCM, and an
in-process SoundFont synthesizer — MeltySynth is the suggested candidate, pure C#, MIT — rendering
the same MIDI the engine sends today. **Windows keeps its current path by default**; add a way to
select the new path on Windows (a flag such as `--audio openal`) so the developer can A/B it there
and decide whether to unify. Do not change what Windows plays unless that flag is given.

The SoundFont is a decision for the developer: propose two or three General MIDI fonts with their
size, licence (must allow redistribution in a commercial game) and SHA-256, wire the code to load
`assets/audio/<name>.sf2` (or wherever fits), fetch the best candidate for testing, and document
provenance like `models/README.md` does. Do not commit the `.sf2` if it is larger than a few MB.

Verifiable here only as "no crash, no error". `--export-music` / `--export-sfx` may let you render
to a file and check it is not silent; use them if they fit.

### 6. Docker

`tools/linux/Dockerfile`: Ubuntu 24.04, the same packages as `cloud-setup.sh` (share them rather
than duplicate the list if practical), the .NET 8 SDK; it builds the game and its entry point runs
`xvfb-run -a ./run_tests.sh` (all, or the arguments passed). The repository is the build context
with a `.dockerignore` excluding `bin/ obj/ dist*/ models/ logs/ local/ cache/`. `models/` is
mounted at run time, never copied into the image. Add `tools/linux/README.md` with the exact
`docker build` / `docker run` commands, including the Windows form (Docker Desktop, WSL2 backend,
PowerShell path syntax for the volume mount).

Verify the image builds and the suite passes inside it, if Docker can run in this sandbox. If it
cannot, say so plainly in the notes rather than implying it was tested.

### 7. Stretch — only if 1–6 are done

- `.github/workflows/linux.yml`: on push to `develop`, build `linux-x64` and run the audits and
  the playground CLI suite under Xvfb. No model download.
- Linux in `package.ps1` (`-Runtime linux-x64`): its own payload allow-list (llama-linux instead of
  llama/llama-arm64), and an archive that **preserves the executable bit** on the game binary and
  `llama-*` — it will be run from Windows, so a zip will not do; a `.tar.gz` written with explicit
  modes (Python's `tarfile` is available on the developer's machine) will. Test it with `pwsh`
  here if it installs cleanly.
- Update the `release`, `models` and `runtime` skills, and the `models/README.md` table, wherever
  they now state something Windows-only that is no longer true.

## Notes file

Keep `tools/linux/PORT-NOTES.md` up to date as you go, committed with the work:

- what was done, step by step, with commit hashes;
- test results: build, audits, CLI suite pass/fail counts, the LLM run;
- **what the developer must check by hand on Windows** before merging (anything touching audio,
  the debug windows, `run_tests.sh`, the csproj);
- what could not be verified here and why (GPU, Vulkan, audible output, Wayland, real drivers);
- open decisions for the developer (the SoundFont, whether to unify audio on Windows).

Finish by pushing the branch and summarising the notes in your last message.
