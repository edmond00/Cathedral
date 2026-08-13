# Cathedral

**"Cathedral" is the development name.** The game is published as **Proscribed
Palimpsest** (edmond00.itch.io/proscribed) and every player-facing surface says so: the window
title, the main menu, the shipped executable, the download. The repository, the namespaces, the
Debug binary and `%APPDATA%\Cathedral` keep the old name deliberately — see "Naming" under
Packaging.

A Windows desktop narrative RPG built in C# combining a 3D glyph-sphere world with local LLM-driven storytelling. The aesthetic blends roguelike exploration with Chain-of-Thought narrative AI.

## Build & Run

```bash
dotnet build
dotnet run                  # main game
dotnet run -- --help        # all options
```

## The player manual

`docs/` holds the player manual and nothing else. `design/` (formerly `docs/`) is drafts, location
design notes and development history — **mostly deprecated, and never a source for the manual**.

The manual explains the game's **systems**, in the manner of a tabletop rulebook: how a die pool is
assembled, what an organ score buys, what a wound costs. Not content, not the interface. It is
derived from the code, chapter by chapter.

**When a change alters a rule a player is subject to, invoke the `manual` skill** — it carries the
style guide, the chapter map and the procedure. Do not edit `docs/manual/` freehand. Purely internal
changes (rendering, CLI plumbing, packaging, audits, tests) touch no chapter and need nothing.

`/manual-sync` reconciles the whole manual against `git diff $(cat docs/manual/.synced)..HEAD`, and
is the right way to catch up after a run of commits rather than after each one.

The Markdown is the source; `python tools/build_manual.py` typesets it into
`docs/manual/ProscribedPalimpsest-Manual.pdf` (headless Chrome, no install beyond
`pip install pypdf reportlab` for page numbers). All design lives in that script — never put HTML
or styling in the chapters.

## Saving

**One save, no slots, no manual save.** `%APPDATA%\Cathedral\save.json`, written automatically at the
start of every world-travel phase and at no other point. **New erases it. Death erases it** — all
three causes, in `TriggerDeath`, so a force-quit at the death screen cannot outlive the character.
The main menu's **Continue** does two jobs from one button: resume the session in memory if there is
one, otherwise load from disk.

**Every failure is the same silent refusal**: a missing, corrupt, wrong-version or wrong-seed save
reads as no save, and Continue simply greys out. There is deliberately no migration and no partial
read — `SaveGame.CurrentVersion` confines a save to the build that wrote it, which is what lets
`PartyState.Rebuild` treat an unknown content id as corruption and **fail closed**. A partly-restored
character is worse than a refused load when there is only one save.

### What is stored, and the rule for adding to it

Five things: the master seed, the clock, the avatar's vertex, `Dictionary<int, LocationInstanceState>`
verbatim, and the party. Nothing else feeds a scene build — the world is a pure function of the seed,
a scene of its location id, and the time of day is drawn fresh on every arrival.

**Locations and the party are persisted by opposite means, and that is deliberate.**
`LocationInstanceState` is plain data shared *by reference*: `AttachTo` hands its dictionaries to the
scene, gameplay writes land in the saved object, and there is no save step at all. The party cannot
work that way — it is polymorphic, reference-shared and full of get-only collections — so it is
*captured* into `PartyState`, which carries the same four-rule contract in its doc comment, with rule
three inverted (**captured, not shared**).

**Adding a field to a party type means three lines**: one in `PartyState.Capture`, one in
`PartyState.Rebuild`, and one in `SaveAudit`. The audit is what makes that mandatory —
`dotnet run -- --save-audit` reflects over the party types and fails naming any public member that is
neither persisted nor recorded as derived. It exists because this failure is otherwise *silent*: the
round-trip test compares a capture against itself, so a field captured nowhere matches perfectly.

Three hazards the rebuild is written around, all of which produce quiet wrongness rather than a crash:

- **`ModiMentis` and `LearnedModiMentis` are the same `List` object.** `Rebuild` re-aliases them; two
  lists means every read path silently diverges from every write path.
- **Memory slots hold the *same instances* as `ModiMentis`.** Serialised inline in both places you get
  two objects, and consolidate/archive/reject silently no-op. `save roundtrip` asserts the topology
  separately, because a value comparison cannot see it.
- **Restore order is load-bearing**: organ scores before `InitializeMemory()`, or the modules come out
  the wrong size and every saved slot index lands elsewhere.

### The seed is per run, not per process

The world derives from the master seed, and that seed locks before the window opens — so the save's
seed is **peeked in `Program.cs` before `GameRng.Initialize`**, and Continue then rebuilds no world at
all. `--seed` still wins outright, which keeps every scripted run reproducible and means a save from a
different seed is simply unloadable.

New therefore draws a fresh seed and regenerates the world in place (`GameRng.Reseed`, which also
clears the streams; `GlyphSphereCore.RebuildForNewSeed`; `MicroworldInterface.RegenerateWorld`). The
*first* New of a process is exempt — the world it booted with is nobody's yet — so the ordinary
launch-then-play path carries none of that risk. Note **three** things come off the seed, not one:
terrain, per-vertex pathfinding noise, and per-edge travel jitter. Regenerating only the terrain lays
a new world over the old world's travel costs.

**Reloading re-rolls anything random that follows the save**, because RNG stream *positions* are not
stored. This is a known and accepted save-scum vector, not a bug. The fix, if it is ever wanted, is a
per-stream draw counter in `SaveGame` — cheap to add precisely because `GameRng` owns `_streams`
centrally.

### Testing it

**Saving is off by default under `--cli`.** Autosave fires on every entry to the world map, so
without that rule every scripted run would overwrite the player's real save, hundreds of times per
suite. A script that is actually testing saving passes `--save-path <file>`, which turns it back on
against a file of its own; `--no-save` disables it outright.

| | |
|---|---|
| `save roundtrip` | capture → serialise → rebuild → re-capture → compare, plus the reference-topology check. Fails naming the first field that did not survive. Append it to any script after doing something interesting. |
| `inspect save` / `inspect savefile` | the live run / what is on disk, for `expect-state`. **Not `expect`** — that scans the rendered terminal and never sees them. |
| `save dump` / `save read` / `save write` / `save erase` | print or force, for diagnosis |
| `pause` | opens the main menu. Escape is handled in the launcher's key hook, not `OnKeyDown`, so `key escape` never reaches it and the menu is otherwise unreachable from a script |
| `click end-run` | the death screen's END RUN button |
| `inspect menu` | which menu buttons are enabled — a disabled button differs only by colour, so this is unassertable from the screen |
| `--black-bile` | fills every humor queue with black bile, so the next journey starves. The only death a script cannot otherwise stage (old age uses `--advance-days`, wounds `--start-fight` plus `fight-end death`) |

`cli/system/save_*.cli` and `cli/system/death_*.cli` cover the lifecycle in-process.
**`tests/save_reload.sh` is the two-launch check** — save, quit, relaunch, Continue — and is
deliberately *not* wired into `run_tests.sh`, which launches the game once per script and so cannot
express a test spanning two processes. Run it by hand before a release.

## Packaging a release

```powershell
./package.ps1                      # dist/ProscribedPalimpsest/ + dist/ProscribedPalimpsest-win64-<date>.zip
./package.ps1 -NoModel -NoZip      # seconds, for checking the script itself
./package.ps1 -ReadyToRun          # ~30% larger, faster startup
```

Self-contained (`--self-contained`), so the .NET runtime ships inside and a player needs nothing
installed. About 200 MB, and it removes the commonest "it won't start" report.

**Single-file, so the folder a player opens is legible.** A plain self-contained publish is 301
files at the root and the game sits in the middle of them, alphabetically between
`PresentationFramework.dll` and `System.Private.CoreLib.dll`. `PublishSingleFile` bundles the
managed assemblies into the executable: 9 files at the root, of which one is the game and the rest
are unmanaged libraries that cannot be bundled.

This is **bundling, not trimming** — every assembly is still present, just inside the exe, so the
reflection the audits and Catalyst depend on is untouched. Managed code runs from the bundle
without being extracted, so there is no first-run unpacking cost.

Two more prunings, worth about 16 MB and 220 files: `SatelliteResourceLanguages=en` drops thirteen
folders of localised framework strings for a game with no localisation, and the staging step
deletes a macOS `.dylib` and a 32-bit MIDI native that an x64 process can never load. After
pruning the MIDI native, confirm audio still starts — the smoke test asserts a window and a ready
server but says nothing about sound, and the line to look for is
`Ambient music engine started`.

**The console is a build flag, not a code path.** `dotnet build` and `dotnet run` produce a console
`Exe` as always; `package.ps1` passes `-p:Ship=true`, which flips `OutputType` to `WinExe` so a
player double-clicking the game gets no black window filling with diagnostics. The two differ only
in a PE subsystem byte — read it with `[BitConverter]::ToUInt16($bytes, $peOffset + 92)`: 3 is
console, 2 is GUI. It is keyed on an explicit `Ship` property rather than on `Configuration`, so
building or profiling in Release still gives you a console; shipping is a deliberate act.

`WinExe` alone would make the shipped build permanently mute, including when run from a terminal on
purpose — which is how a package is verified at all. `ConsoleAttach.AttachToParentIfPresent()`, the
first line of `Program.cs`, rejoins the launching terminal when there is one and does nothing when
there is not. So `dist\ProscribedPalimpsest\ProscribedPalimpsest.exe --cli-script …` still works, piped capture and file
redirection both still work, and a double-click is still silent.

The cost to accept: a shipped build that dies before its window opens shows the player nothing.
Reproduce it by running that same exe from a terminal.

**`Ship` also strips the development command-line options.** `ShipArguments.Filter` runs as the
second line of `Program.cs` — after `ConsoleAttach`, before the seed parse — and reduces `args` to
an **allow-list**: `--cpu`, `--gpu`, `--no-llm-probe`, `--silent`, `--help`. Everything else is
dropped, along with its value, so `--seed 42` leaves no stray `42` behind. It reports how many it
ignored, which is visible when the exe was launched from a terminal.

A filter rather than 49 conditionals, for the same reason the packaging payload is an allow-list:
the option surface grows every time a feature turns out to be hard to reach, and a debug flag left
reachable does not announce itself — it just works. Filtering once, before anything reads `args`,
makes every handler unreachable by construction and means a flag added tomorrow is excluded from
shipped builds without anyone remembering to exclude it.

The five that survive all exist to get a player out of trouble rather than to change the game, and
`--help` in a shipped build lists exactly those. Printing the full development list would be worse
than printing nothing: every line is an instruction that silently does nothing, with no way for the
reader to tell which.

**`Ship` also compiles out the developer keyboard.** It defines a `SHIP` constant, which flips
`Config.Debug.DeveloperKeys` to false. That gates the render-debug keys (D, M), the post-process
tuning keys (F, G, H, J), the debug camera (C, V), the window's diagnostic dumps (D, G) — **and
camera zoom (W, S)**. Zoom is in the list although it is not a debug feature: the game sets the
camera distance itself per phase, and a player who zooms out of that framing has no control that
restores it.

What a player keeps: **arrows** rotate, **Space** re-centres on the protagonist, **Escape** opens
the pause menu and closes narration popups. Escape is never gated — without it there is no way out
of a scene.

Two things to keep in mind when touching that keyboard:

- **Gate the branch, not the chain.** In `LocationTravelModeLauncher`'s `KeyDown`, the D and G
  branches test `DeveloperKeys` as part of their own condition. Short-circuiting the whole
  `else if` chain instead would swallow every non-Escape key before it reached the final `else`,
  which is what forwards keys to fight and dialogue modes — taking the keyboard away from gameplay.
- **`--no-developer-keys` makes a development build behave like a shipped one.** Keys cannot be
  driven by `--cli` at all, so this flag plus the `Developer keys: …` line printed at startup is
  the only way to check the shipped keyboard without building and hand-testing a shipped exe.

**The payload is an allow-list.** Only paths named in `$Payload` are copied, and a missing required
one fails the build before anything is archived. The tempting alternative — copy the repo, delete
what looks unnecessary — ships whatever was added since someone last read the delete list, and
breaks silently when a new runtime asset arrives without being un-deleted.

Three things the script knows that are not obvious:

- **`dotnet publish` copies neither `assets/` nor `models/`.** Nothing in the csproj marks them as
  content, so both are staged by the script. Adding a runtime asset means adding it to `$Payload`.
- **Never enable trimming.** The audits reflect over `Outcome` and `Verb` subclasses to build their
  catalogues, and Catalyst and MSAGL load types by name. A trimmer removes precisely those, and it
  fails at runtime on a player's machine rather than at build.
- **`Compress-Archive` is not used.** The cmdlet in Windows PowerShell 5.1 fails above 2 GB, and
  this package is larger than that before the model is counted; `ZipFile.CreateFromDirectory`
  handles it.

Not shipped, and why: `data/` is design source nothing reads at runtime, `assets/old/` is
superseded art, and `src/` is unnecessary because the two shaders under `src/terminal/Shaders/` are
read from disk *only if present* and otherwise fall back to strings embedded in the renderers —
which means **a shipped build runs the embedded copies**. Keep the two in sync when editing a
shader, or the release will not look like the dev build.

The zip lands around 2.2 GB, nearly all of it `model.gguf`, which is already-compressed
quantised weights and does not shrink. That is over itch.io's browser upload limit, so releases
go through `publish.ps1`.

### Publishing

```powershell
./publish.ps1 -Status     # read-only: what is on the channel now
./publish.ps1             # publishes to edmond00/proscribed:windows
```

The target is `edmond00/proscribed:windows`, from the page at edmond00.itch.io/proscribed.

### Naming

The game is **Proscribed Palimpsest**; "Cathedral" is the development name. The split is
deliberate and is drawn at exactly one line — what a player can see:

| player-facing, renamed | development, still Cathedral |
|---|---|
| window title (`Config.Name.WindowTitle`) | repository, csproj, namespaces |
| main menu (`Config.Name.GameTitle`, stylised lowercase) | `bin/Debug/Cathedral.exe` |
| `ProscribedPalimpsest.exe`, staged folder, zip | `%APPDATA%\Cathedral\settings.json` |
| the itch page | the dev-only launchers (fight area, image-to-text, music PoC) |

**Only the shipped executable is renamed**, by an `AssemblyName` conditioned on the same `Ship`
flag. `run_tests.sh` guards the suite with `Get-Process -Name Cathedral`, and that guard is what
stops a leftover run from racing a new one; renaming the development binary would break it
*silently*, because the guard would simply stop finding anything and read as "all clear".

`%APPDATA%\Cathedral` is left alone on purpose. No player ever sees that path, and renaming it
would discard every existing install's volumes, dither and probed compute device.

The shipped name has no space in it. A player sees it in a folder listing either way, and every
CLI invocation — the whole test driver, any future CI — would otherwise need quoting.

**It pushes the staged folder, not the zip**, and that is the whole reason it is worth having a
script. butler diffs a build against the previous one file by file; pushed as a folder, the 2 GB
model is uploaded once and skipped on every later release, so a code-only update sends a few
hundred MB. A zip is one opaque blob — change one byte and the entire archive is new. Players
still get a downloadable archive; itch builds one from the pushed files. The zip `package.ps1`
makes is for manual distribution and backups, not for this.

**No credentials live in the repo or on a command line.** `butler login` is a one-off interactive
browser flow that stores a token under `~/.config/itch/butler_creds`; `BUTLER_API_KEY` in the
environment is the unattended alternative. The script only checks that one of the two exists and
lets butler do the rest.

Pre-flight refuses to upload rather than publish something broken: the same required paths
`package.ps1` verifies, plus a **PE subsystem check** that fails a console build (which would mean
`-p:Ship=true` was lost and players would get a diagnostic window behind the game), plus deleting
any `logs/` left in the staged folder — those contain the full text of everything the model was
asked and answered.

### Verifying a shipped build

**Gameplay is verified on the development build; the shipped artifact is verified by starting it.**
That division is deliberate, and it is why the locked-down option set costs nothing.

`-p:Ship=true` changes exactly three things — a PE subsystem byte, two compile constants, and the
assembly name. No line of game logic differs, so the CLI suite testing narration, verbs and
outcomes against the development build is testing the same code with better tools. There is
deliberately **no flag that re-enables the development options in a shipped build**: the scripts
are built on `--seed`, `--skip-childhood`, `--observe-only`, `--location-type` and a dozen more, so
anything that made them usable would put most of the surface back and would drift every time a
script needed something new.

What only the artifact can prove — that the self-contained runtime resolves, that `assets/` and
`models/` are found from the executable's own directory, that the GGUF loads and a backend is
chosen, that a window opens with its fonts — needs **no options at all**. `publish.ps1` launches
the staged build with an empty command line, waits for a window title and for `LLM Server is ready`
on stdout, then kills it and its `llama-server` child. `ConsoleAttach` is what makes that output
readable. A pass covers every packaging failure mode there is; skip it with `-SkipSmokeTest` only
when re-pushing something already started once.

The confirmation prompt cancels rather than throwing when nothing can answer it, so an unattended
run never publishes by accident — `-Yes` is how such a run says it meant to.

## The language model runtime

Everything the game loads that is not code lives in `models/`, and `models/README.md` is the
player-facing version of this section.

**The model has no name.** It is always `models/model.gguf`. There is no setting, no alias table
and no path in `Config` — changing models means replacing that file, and that is the entire
interface. `LlamaRuntime.ModelFileName` is the one place the string exists.

This works because a GGUF carries its own identity: architecture, tokenizer and chat template are
all read out of the file, so llama.cpp does not care what it is called. `GgufMetadata` reads
`general.name` back out of the header for the **startup log**, since otherwise a generic file name
would leave nobody able to tell what an install was running.

The Settings screen deliberately does *not* show it. Which model backs the narration is an
implementation detail of the fiction; naming a vendor's model on a player-facing screen buys
nothing, and the diagnostic need is met by the log.

There was previously a `_modelAliases` dictionary in `LlamaServerManager` naming a *different*
model from the one `Config.LLM.ModelFileName` actually loaded. Nothing read it. Do not add another.

### Backends: how GPU support is found

`models/llama/` is a **pruned** llama.cpp release — `llama-server.exe`, `llama-bench.exe`, the
shared libraries, and the fourteen `ggml-cpu-*.dll` microarchitecture variants. `models/llama/BUILD.txt`
records which upstream build it came from and what was dropped.

Backends resolve **at runtime, not at build time**. ggml scans the server's own directory on
startup and loads whichever `ggml-*.dll` initialise — that is how the CPU variants pick themselves
by host ISA, with no help from us. Running `llama-server.exe --version` prints the choice:

```
load_backend: loaded CPU backend from ...\ggml-cpu-skylakex.dll
```

A GPU backend joins the same mechanism through the `GGML_BACKEND_PATH` environment variable, set by
`LlamaRuntime.ApplyBackend`. Three things about that are easy to get wrong:

- **It names one file, not a directory.** Pointing it at a folder is answered with
  `load_backend: failed to load`. This suits the design — exactly one GPU backend should ever be
  live — but it means there is no "load everything in here".
- **The backend's own directory must go on `PATH` too.** A CUDA backend pulls in `cudart64_12.dll`
  and `cublas64_12.dll` from beside itself, and the directory of a dynamically loaded library is not
  otherwise searched. Without it the backend fails to load with nothing to say why.
- **Never mix build numbers.** A backend from a different llama.cpp revision than `ggml-base.dll`
  crashes inside the backend with no usable diagnostic. This is why every backend is verified in a
  *subprocess* before use — a crash there costs nothing.

See `models/llama/backends/README.md` for what to drop in. Vulkan is the one worth shipping: one
59 MB file covering NVIDIA, AMD and Intel, against ~420 MB of CUDA for one vendor Vulkan already
handles. With the folder empty the game runs on CPU and nothing has to be configured for that.

### Do not pass `-ngl`

llama.cpp's `--fit` defaults to **on** and sizes the offload to fit device memory with a 1 GiB
margin; `-ngl` itself defaults to `auto`. So the right thing is to pass **neither** and let it fit.

The code used to hardcode `-ngl 99`. On the shipped CPU-only build that was inert, which is why it
survived — but `99` means "all layers in VRAM" and *overrides* the fitting, so the moment a GPU
backend is installed it becomes an out-of-memory failure on any card too small for the whole model.
`BuildServerArguments` now emits `-ngl` only when a player has explicitly set a layer count, and
`-ngl 0` when the run is deliberately on the CPU.

### The first-run probe

`LlamaProbe` decides **device only** (GPU vs CPU) once, and caches the answer in `UserSettings`
against the model file's size and timestamp — so replacing `model.gguf` re-probes, and a device
chosen for a 2 GB model is not reused as evidence about a 5 GB one.

It **measures rather than identifies**. Reading a PCI id answers "is there a GPU", which is not the
question; the question is "will llama.cpp go faster on it today", and hybrid laptop graphics, stale
drivers, and integrated GPUs slower than the CPU beside them all get the first question right and
the second one wrong. So: enumerate devices with `llama-server --list-devices`, then time the
candidates with `llama-bench -o json`.

**The common case is free.** With no backend installed there is nothing to choose between, so it
settles on CPU without loading the model or benchmarking anything. Only a machine that actually has
a backend pays for the measurement, once.

It is skipped entirely under `--playground` (no server is started at all), so the test suite never
pays for it. `--no-llm-probe` skips it otherwise.

### Failure is a downgrade, not an exit

`StartWithFallbackAsync` walks a ladder — the configured GPU backend, then CPU — and persists the
downgrade, because a machine that has failed once will fail again and re-attempting costs the player
the same minute of loading on every launch. The Settings screen's re-detect button is how they ask
for another try.

Two things make that ladder actually work, and both were bugs when written:

- **`WaitForServerReadyAsync` checks whether the process is still alive.** It polls HTTP with an
  eight-minute timeout and cannot otherwise distinguish a server still loading from one that exited
  — both are a refused connection. Without the liveness check a crashed backend costs the full eight
  minutes before anything reacts, which is indistinguishable from a hang.
- **`KillServerProcess` closes the log writer as well as killing the process.** Each attempt opens
  `llama-server.log` with `append: false`, so leaving the previous writer open makes the retry fail
  on a locked file — and that failure looks like a second backend fault rather than bookkeeping.

**Logging can never take the LLM down with it.** Every log path is relative to the working
directory, so an install the player cannot write to — extracted into Program Files, on read-only
media — fails at the first `CreateDirectory`. That call used to sit unguarded inside the
server-start try block, so the exception was caught as "Error starting server" and the game lost
narration entirely, with a message pointing at the wrong thing. Every log directory now goes
through `TryCreateLogDirectory`, which returns null and reports once; a null session directory
switches the whole diagnostic tree off, because every writer downstream already tests it. The
per-request blocks additionally catch their own writes, so a disk filling up mid-session costs
logging rather than the request. There is deliberately **no fallback to the working directory** for
`llama-server.log` when the session directory is missing — that is the directory that just proved
unwritable, and retrying it there is what turned a lost log into a lost server.

To reproduce: put a *file* named `logs` beside the executable and start the game. `CreateDirectory`
cannot create a directory over it, which is the same failure as a read-only folder without needing
to change any permissions.

### Settings

`UserSettings` (was `AudioSettings`) is the single persisted store, at
`%APPDATA%\Cathedral\settings.json` — volumes, the dither toggle, and the compute settings. They
live together because the file is rewritten whole, not merged: two classes writing it would each
silently discard the other's fields.

**Dither persists the on/off state only, not which dither.** `PostProcessRenderer.Enabled` is
derived (`Mode != Off`) and restores whichever mode was last in use, so applying the saved bool at
startup turns the layer on or off without overriding a mode chosen by `--dither`. F/G/H stay live
tuning and are deliberately unsaved — persisting the mode but not the palette depth or grain size
would be worse than persisting none of the three. The flag still wins over the saved value, guarded
by `Config.PostProcess.DitherModeSetByFlag`; without it, a player who once enabled dither in the
menu would silently break `--dither off` for every later run. The Settings screen still reads the
toggle back from the *renderer* rather than from `UserSettings`, so it shows the true live state
after a flag or an F-key cycle — meaning shown and saved can legitimately differ until the toggle
itself is clicked.

The Settings screen exposes device, GPU layers, threads and re-detect. **All of them apply at the
next launch**, and the screen says so: the server loads the model once at startup and holds it, so
applying a change in place would mean tearing it down, re-reading two gigabytes and rebuilding every
cached persona slot, possibly mid-narration. Re-detect therefore measures nothing on the spot — it
discards the saved probe signature so the probe re-runs during the next launch's model load, where
it is already behind a loading screen.

`--cpu` and `--gpu` override all of it for one run through `Config.Debug.ForcedLlmDevice`. They
deliberately do *not* write to `UserSettings`: flags are parsed before `UserSettings.Load()` runs, so
a flag that wrote there would be overwritten by the file a moment later.

## Verifying a change (read this before testing anything)

The game is an OpenGL app driven by mouse clicks, so "run it and see" is not something an
automated agent can do directly. Use **`--cli`**: the game still opens its window and renders
normally, but it also accepts commands on stdin and can print any screen back as text.

Everything the driver prints is prefixed `[cli]`, so filter with `grep '^\[cli\]'` to separate it
from the game's (very chatty) diagnostic logging on the same stdout.

### The flags that make a run reproducible

| Flag | Why you want it when testing |
|---|---|
| `--cli` | Drive from stdin, observe as text. |
| `--cli-script <file>` | Run a command script at startup (implies `--cli`). |
| `--cli-timeout <secs>` | Hard limit before the run closes itself. **Always pass this** — without it a script that never reaches `quit` hangs forever. |
| `--playground` | Replaces every LLM call with instant placeholder text. No model, no server, no waiting. |
| `--seed <n>` | Fixes the master RNG so the run is repeatable — world, spawn, dice, the starting kit, and every choice `--playground` stands in for. Two runs with the same seed produce byte-identical `[cli]` output; only LLM text (which carries no seed) is exempt. |
| `--skip-childhood` | Skips the childhood + get-up phases and fills starting skills/items randomly — from the master seed, so the same seed gives the same kit. |
| `--debug` | Lets `strategy` force action outcomes. Under `--cli` it never prompts — see below. |
| `--start-at <name>` | Spawns the protagonist on the first biome or location matching `<name>` (`village`, `farm`, `field`, `cave`…). Without it you get wherever the seed puts you, which is usually plain or forest — testing anything village-specific otherwise means hunting for a lucky seed. |
| `--start-area <name>` | Opens narration in the first area of the location whose name matches `<name>` (`pigsty`, `smithy`, `hall`…). `--start-at` picks the location, this picks the room. Without it you arrive in whichever area the factory built first — a farm's courtyard — and reaching any other one costs an observation, a think and an action **per step**, with the persona choosing what is observable at each. Anything that lives in one room (a pigsty's pigs, a smithy's anvil) is otherwise a long walk away. Inert when the location has no such area, so it can be left on across a multi-location run. |
| `--observe-only <name>` | Restricts what an observation phase may look at to objects matching `<name>`. **This is what makes a specific object reachable at all.** A phase opens on ONE object the persona chose out of a dozen, so without it a script that wants to act on the pig is waiting on a coin flip it cannot influence — and re-rolling seeds until the right object comes up first is not a test. Whole words match before substrings (`pig` means the pig, not the Courtyard–Pigsty Track), and a phase where nothing matches falls back to the whole scene rather than narrating nothing. |
| `--no-encounters` | Suppresses random travel encounters. **Pass this in every script that travels.** An encounter puts the game in `EncounterPrompt`, where a script waiting for `LocationInteraction` sits until its timeout and reports a failure that has nothing to do with what it was testing. |
| `--allow-reentry` | Lets a world-map click on the vertex the avatar already stands on enter that location. **The game refuses it** — arriving somewhere already opens it (`OnProtagonistArrived` starts the interaction itself), so that click is only ever a way back into the place just walked out of, and a visit is meant to cost a journey. `travel here` is exactly that click, and the spawn vertex is the only one a script can name from a cold start, so **`run_tests.sh` passes this on every script** — you do not put it in a `# FLAGS:` header. |
| `--period <name>` | Pins the arrival time of day (`dawn`…`night`) instead of drawing one at random. Needed for anything period-gated: every building's entry door shuts at `night`, and a random draw reaches that one visit in six. |
| `--fill-party` | Fills the companion roster to its heart-derived ceiling (`max_companions`) right after the childhood phase, with NPCs generated from random archetypes — the **last** slot a beast, every slot before it a human. Recruiting even one companion in play takes a conversation and a check (or an appease *and* a tame), which is a long approach for anything that just needs a populated party. Note the ceiling is the *heart* score, and a protagonist accepted straight out of creation has a heart of 1 — so a plain script gets one beast and nothing else. For humans too, raise the heart first (`click cell 94 24` on the creation screen bumps it, four presses reaching 5). |
| `--start-fight <creature>` | Drops straight into a fight on reaching the world map (`wolf`, `bear`, `bandit`, `brigand`). **The only way a script can reach fight mode at all** — the real routes in are a random travel encounter (which every script disables with `--no-encounters`, precisely because it fires unpredictably) and provoking a location NPC through a conversation and a check. |
| `--grant-mm <id[,id…]>[:lvl]` | Grants the named modi mentis at `lvl` (default 1) after character creation. Fighting skills are gated behind their modi mentis, so this is what makes a given skill reachable — and since a **buff's cost falls as its level rises**, it is also how you exercise both ends of that curve. Unknown ids are reported on stderr. |
| `--spawn-beast <name>` | Puts a beast (`wolf`, `boar`, `bear`, `black bear`, `stray dog`, `fox`, `cat`) in the **opening area** of every scene, at every period, flagged an enemy by the same first-contact pass that flags a rolled one. Everything a script does *to* a beast — appease, tame, track — starts with one being where the script opens, and no factory guarantees that: a wolf is rolled 10–20% of the time, a boar 25–40%, and whichever is rolled then roams between areas with the hour. Follows `--start-area`, since it spawns into whichever area narration opens in. |
| `--goal-only <verb-id>` | Forces the playground's goal choice onto one verb. `--playground` replaces the persona's "what do you want to do?" with a **uniform draw over every goal the observed object offers**, so a script meaning to `appease` a beast — and then `tame` it — is otherwise picking out of a dozen. The CLI's `goal` command sets the same switch, which is what a script that needs two different goals in one run should use. Inert when no goal in a phase matches, which then draws as usual. |
| `--advance-days <n>` | Pushes the world clock forward `n` days on first arrival at the world map. The clock only moves on travel arrival and work stints while a wound needs 100–1000 days to close, so without this nothing about healing is observable. Fires once, before anything has happened — for healing *a wound taken in play*, use the `clock` command instead. |
| `--location-type test` | Builds the **test location** (`TestSceneFactory`) — a kitchen sink holding one of everything, under names that never change. This is what the whole CLI suite runs in; see "The test location" below. It is attached to no biome, so this flag is the only way in. |
| `--silent` | Opens no audio device: no music, no sound effects, no loading wash. `run_tests.sh` passes it on every script — a full run launches the game a hundred-odd times, and without it that is an hour of overlapping music from windows nobody is watching. Implemented by not opening the MIDI device, which is a state the engine already handles (a machine with no MIDI runs the same path). |
| `--location-type <name>` | Forces which scene factory builds every location (`village`, `forest`, `cave`…), ignoring the biome underfoot. **Prefer this to `--start-at` in a test.** `--start-at` searches the *generated world* and shrugs when it finds nothing — at seed 42 there is no forest in reach, so every forest test silently ran in a plain. This builds one regardless, so a test does not depend on world generation at all. |
| `--location-id <n>` | Builds every scene as location id `n`. A scene is a pure function of its id (`CreateSeededRandom(locationId)`), so the id decides the layout, the room names, the objects and the people — a village rolls a Chain or a Hub with entirely different rooms. Together with `--location-type` it determines the whole scene, which is what makes `--verb-probe`'s reported rooms actually exist in the run. |
| `--npc-static` | Pins every NPC to the area they spend most of the day in, instead of following their schedule. Where somebody stands at a given hour is drawn from the location seed across six periods, so an NPC verb's test cannot otherwise name the room — and a test that names the wrong one finds it empty. `--verb-probe` sets the same flag, so its rooms and the run's agree by construction. |
| `--auto-dialogue` | Settles every conversation as an immediate success instead of holding one. A dozen verbs only *open* a dialogue and the tree decides what follows, so without this a **verb** test has to walk somebody else's tree to reach its own assertion — and breaks whenever that tree is re-authored. `DialogueAutoResolve` performs exactly the writes a won conversation makes, so nothing reading the world afterwards can tell. The trees themselves are covered by `cli/_systems/dialogue_*.cli`. |
| `--grant-item <id[,id…]>` | Puts named items in the starting pack (`axe`, `pick`, `shovel`, `fishing_rod`…). Five verbs are tool-gated and `RequiredToolRule` refuses them outright without one; the starting kit is random, so their success test is unwritable otherwise. Note the rule accepts only `Action.CombinedItem` — **carrying** the tool is not enough, the script must combine it with the action (`click action 0` → `choose 1` → pick the item → `click action 1`). |

A typical invocation:

```bash
dotnet run -- --playground --skip-childhood --debug --seed 42 \
              --cli-timeout 180 --cli-script test.cli 2>&1 | grep '^\[cli\]'
```

The process exits **non-zero** if any `expect` assertion failed or any `wait` timed out, so a
script doubles as a regression test.

**Never write `new Random()`.** Every generator comes from `GameRng` (`src/Rng.cs`): `Stream("tag")`
for anything drawn from repeatedly, `For("tag")` for a one-shot, `DerivedSeed("tag")` when something
else wants a plain seed. One unseeded generator anywhere in the pipeline makes `--seed` a lie, and
the symptom — a script that passes four times and fails the fifth — never points at the cause. The
seed is resolved at the very top of `Program.cs`, before any other flag is parsed, because touching
a mode class runs its static initializers; a reader that beats the parse locks in a time-based seed
and `Initialize` will say so on stderr.

### Command vocabulary

Run `help` for the authoritative list. The essentials:

```
  state                     mode + phase flags (loading, dice, noetic, history/total lines)
                            plus carry[cur/max] and the travel blocker when overloaded, and
                            observed[done/total] — objects this narration phase has already
                            looked at, which are withheld from every later observation choice
                            of the phase and released again when the phase ends
  dump [--color]            the terminal grid as text; --color tags each row dim/mix/lit
  regions                   what is actionable right now — the handles `click` accepts
  world                     avatar vertex, biome and location
  destinations              vertices bordering the avatar
  destinations all [filter] every vertex inside the (stat-derived) travel range, not just the
                            neighbours; filter by biome/location name — `destinations all village`

  click menu <label>        main-menu button
  click continue            protagonist-creation Continue, or a settled dice overlay
  click keyword <name>      a narration keyword
  click action <n>          a narration action
  click option <n>          a dialogue reply
  click skill <name>        a fighting skill, by name. Self-targeting skills (every buff, and
                            parry/dodge/the postures) execute at once; an attack arms targeting
                            and you follow with `click fighter`
  click fighter <name>      a fighter's map cell — the target step for an attack
  click end-turn            the fight's END TURN button
  click button              the footer button (LEAVE / INTERRUPT / END / CONTINUE)
  choose <n>                answer the visible popup by index
  travel here               enter the location the avatar is standing on — what most scripts
                            actually want, and one command instead of four
  travel <vertex|name>      plan a route to a vertex (bypasses 3D picking entirely). Naming
                            somewhere else only PLANS the route and leaves the travel box up:
                            follow with `travel-go`, wait for WorldView, then `travel here`
  travel-go                 commit the plan and set out — the TRAVEL button
  travel neighbour          plan a route to any bordering vertex — leaving, without naming where
  travel back               plan a route to the last location entered that is not this one.
                            Together these two are how a script makes a ROUND TRIP, which is the
                            only way to reach routine replay (a routine replays on ARRIVAL): the
                            vertex a run starts on is whatever the seed put under the avatar, and
                            `travel <name>` prefers the vertex underfoot — it would walk straight
                            back into the location it is trying to leave
  routines                  list the routines the planned destination offers (opens the box)
  routines <n>              replay routine n there — picks it and sets out
  routines continue         press CONTINUE on the post-replay outcome box, which applies the
                            phase the routine ended on (narration, a dialogue, trade, or work)
  manage [tab]              open/close the protagonist screen; with a tab name
                            (Anatomy / Inventory / Memory / Humors / …) open it there
  select [item name]        show a carried item's info panel; bare `select` lists what
                            is carried (open `manage Inventory` first). The starting kit
                            is random but seed-stable, so discover the names on one run
                            and they hold for every later run at that seed
  scroll up|down [n]        scroll the shared history buffer

  clock <days>              push the world clock forward, then run the wound-healing sweep and
                            report how many wounds closed. The clock otherwise only moves on
                            travel arrival and work stints, so this is the only way to see a
                            wound taken in play heal within a script
  strategy <succeed|fail-dice|fail-plausibility|auto>
  goal <verb-id|none>       pin the playground's goal draw to one verb, the way `strategy` pins
                            the dice — set it BEFORE the keyword click whose thinking phase
                            should land on it. `goal none` hands the choice back to the RNG.
                            Same switch as `--goal-only`; a command because a script that
                            appeases a beast and then tames it needs two different goals
  fight-end <victory|death|runaway>
  advance [presses] [secs]  settle, then press the preview box's CONTINUE until it is gone.
                            USE THIS, not a bare `click continue`, to get from a keyword click
                            to the action list — see the trap below
  wait [secs] | wait mode <GameMode> [secs]
  inspect [subject]         the game state an outcome can change, by STABLE id — items, coins,
                            where, party, wounds, skills, npcs, pois, routines, or all. What
                            cli/outcome/ reads: `expect` scans the SCREEN, this reads the world.
                            `routines` is answerable with NO narration in progress, because a
                            session's trailing routine is finalised as that session ENDS — reading
                            it from inside the location reads a moment too early
  expect-state <subj> <text> | expect-no-state <subj> <text>
                            assert `inspect <subj>` does (or does not) report a line containing
                            <text>. The outcome range's assertion
  expect-outcome <id> | expect-no-outcome <id>
                            assert an outcome of that id was applied this run. `expect <chip>`
                            proves what the player was TOLD; this proves what ran — the only way
                            to assert the outcomes that show no chip at all
  expect <text> | expect-not <text>
  quit
```

**Name things, never coordinates.** `click keyword hearth` survives layout changes and reads as
intent; `click cell 34 12` breaks on the next UI tweak. Use `regions` to discover handles rather
than reading a `dump` and counting rows. (`click cell` exists as an escape hatch for UI that has no
region support yet.)

**`wait` is the load-bearing command.** Observation generation, dialogue replies and travel are all
asynchronous — a script that acts and immediately dumps reads a half-built frame. `wait` blocks
until the game is settled (no LLM task in flight, not mid-travel, no dice rolling). Use
`wait mode <GameMode>` for the parts of startup that advance on their own, and note that some
screens **never** advance without input: `ProtagonistCreation` needs `click continue`, and
`WorldView` needs a `travel`.

### Forcing outcomes with `--debug`

`--cli` owns stdin, so `--debug`'s normal console prompts would steal commands and deadlock.
Under `--cli` they are suppressed and the preset strategy answers every decision instead:

```
strategy succeed        # plausibility + difficulty pass, dice succeeds
strategy fail-dice      # checks pass, dice fails
strategy fail-plausibility
strategy auto           # no override; LLM/RNG decide
```

`strategy custom` degrades to `auto`, since per-node prompting is interactive by nature.
Set the strategy *before* the action that should be affected.

**A forced narration success caps the difficulty at the dice pool.** A verb harder than the chain
that reached it — `tame` is difficulty 4, and a keyword→think→act chain of three unlevelled modi
mentis rolls three dice — needs more sixes than there are dice, so no arrangement of the pool
succeeds. `strategy succeed` asking for one used to **hang the game outright** (the forced-values
generator spinning for a six it had nowhere left to put, window alive and answering nothing). The
roll now caps the demand at the pool and says so on stdout, the same way the fight path guarantees a
forced success at least one die.

**Fight dice honour it too.** `strategy succeed` pins every attack die to a six and every defence
die to a one (and `fail-dice` the reverse), so a script can make a blow land instead of waiting for
luck. A forced success also guarantees at least one die: a fighter with an unlevelled organ and no
contributing modus mentis rolls an empty pool, which would otherwise miss no matter what was asked
for — and every consequence downstream of a hit (wounds, bleeding, knockdown, healing) would be
unreachable.

### What a fight is made of

Three rules the rest of the fight code assumes, all of them recent:

- **A buff costs vital heat, not dice.** `FightingSkillEffect.Buff` — the five viscera skills plus
  Sprint and Jump — has no target and nothing to hit, so it rolls nothing. It costs
  `clamp(10 − level, 1, 10)` vital heat, which means **level makes a buff cheaper, not stronger**,
  and its whole effect is the `FightStatusEffect` returned by `CreateBuffEffect`. A consumption box
  plays for `VitalHeatBoxDuration` and hands the turn straight back — no Continue button, because
  there is nothing to decide.
- **Defence is a number of dice, so guards are bought rather than rolled.** Parry, Dodge, Cover and
  Defensive Posture cost CP and immediately grant `+TotalDice` to the defence pool for the turn.
  Feint is the one self-targeting skill that still rolls (someone has to be convinced) and its
  sixes become bonus attack dice. **No self-targeted roll can ever produce a wound** —
  `FinishAttackResolution` routes them to `CreateRolledEffect` instead. That guard is not
  theoretical: it is why a parry used to be able to injure the person parrying.
- **Adjacency is 8-way, including for attacks.** `FightResolver.IsInSkillRange` is the single
  definition, used by both the player's target highlighting and the AI's candidate filter. Range is
  Euclidean (so a bow's reach stays circular) *plus* the eight surrounding cells always count.
  Without that exception a diagonal neighbour sits at distance 1.41, out of melee range 1, and two
  fighters could stand side by side unable to swing at each other — which is what made enemies walk
  up to someone and then pass their turn.

A "this turn" effect expires in `Fighter.EndTurn`, not `StartTurn`: the start-of-turn pass only
comes round after every other fighter has acted, which would stretch it to a full round. Cold Blood
genuinely wants the round (it fires while enemies attack) and so leaves `OnTurnEnd` alone; Blood
Lust lasts the whole fight and expires nowhere.

### Wounds and healing

A `Wound` is a **catalogue entry and immutable**; `WoundInstance` is one injury on one body.
`WoundRegistry.All` hands out a single shared template per wound type, so anything settable on
`Wound` would be shared by every character in the process — which is exactly what went wrong when
glyph positions lived there. Per-injury state (the infliction day, the resolved art cell) belongs
on the instance.

`WoundInstance.InflictedOnDay` being **null means historical** — an NPC's trait scars, a
protagonist's starting injuries — and historical wounds never heal. Everything created during play
uses `WoundInstance.Inflicted`, which stamps `GameClock.Days`. Get this wrong in either direction
and one long work stint erases every NPC's backstory.

Only Low and Medium wounds heal, over `wound_healing` days (100–1000, from the viscera). Progress is
*derived* from the clock rather than ticked, the same trick `PartyMember.BirthTimeDays` uses for
age: nothing to keep in sync, and correct whenever it happens to be read. The sweep runs on entry to
the world view, before the old-age check — healing restores HP and lifetime is wound-aware, so a
heart wound that closed on the journey must not still be counted a line later.

### Verbs, actions and outcomes — the three types, and what each is for

The narration loop has exactly three nouns. They were once eight, several of them named "outcome",
which is why the word alone never told you which end of the pipeline you were at.

| type | what it is | when |
|---|---|---|
| `Verb` | the abstract definition — GRAB, MOVE, ATTACK. Hand-registered in `VerbRegistry`, queried before anything exists (`IsPossible`, `DifficultyFor`, `IsIllegal`, `ExpandViews`) | — |
| `VerbAction` | a verb **in context**: verb + verbatim + target + variant. What the thinking phase picks as a goal (`PreselectedOutcome`) and what a click executes | before |
| `Outcome` | a **consequence**: `Apply(OutcomeContext)` changes the world, `Text` is the chip, `Verbatim` is the phrase the narrator gets | after |

Two supporting types, both deliberately thin:

- **`INarratable`** — `DisplayName` + `ToNaturalLanguageString()`. All the outcome narrator needs.
  An interface, not a base class, because three unrelated families implement it and inheriting from a
  shared *base* implied kinship they do not have — a fight's entry payload derived from something
  called `OutcomeBase` purely to be passable to `NarrateOutcomeAsync`, and so read as a consequence
  for years.
- **`NarrativeAnchor`** — a place a click can land, with exactly two subclasses: `ObservationObject`
  (look at it) and `VerbAction` (do it). Both live in the same `SubOutcomes` / `PossibleOutcomes`
  lists because the thinking phase draws from them together: "look closer at that" and "grab the
  flower" are both answers to "what do you want to do?".

**Do not reintroduce a second class for one event.** Every consequence used to exist twice — once as
the chip the player sees, once as a narratable the LLM is told about — written separately and free to
disagree. `WoundOutcome`/`WoundInflictionOutcome`, `HumorOutcome`/`HumorChangeOutcome`,
`FightOutcome`/`FightTriggerOutcome`, `DialogueOutcome`/`DialogueTriggerOutcome`. `Outcome` implements
`INarratable` (`DisplayName => Text`, `ToNaturalLanguageString() => Verbatim`), so one object serves
both roles, and the mode-entry payloads are now the trigger outcomes themselves.

**A conversation's effects are ordinary outcomes too.** `IDialogueOutcome` is gone: its ten
implementers are `Outcome` subclasses whose `Apply` reads `ctx.Npc` and `ctx.PartyMemberId`. The only
thing that had kept them separate was an incompatible `Apply` signature, which `OutcomeContext` now
carries for both. Because such an outcome settles its own wording at apply time (an affinity move has
to phrase "Stranger → Distant Acq.", and only the code making the change sees both sides), **a tree
hands out a fresh set per access** — `SuccessOutcomes => new[] {…}`, expression-bodied, never
`{ get; } = …`. Trees are singletons in `DialogueTreeRegistry`, so an initialised auto-property would
share one mutable outcome object across every conversation in the process.

`Report(text)` settles that wording and sets `Reported`; an outcome that never reports changed nothing
observable, and `ShowInUI` follows. That replaced returning `null` from `Apply`.

### The economy of a narration phase: noetic points and the two ledgers

A phase's budget is **noetic points**, one spent per observation, thinking or item combination, and
the pool refills at the phase boundary (`CloseNarrationSegment`). Three rules decide how far that
budget reaches, and they were designed together — changing one alone re-breaks what the others fix.

- **The pool is the encephalon score divided by three** (`NoeticPointsStat`), **rounded up**. At one
  point per level a segment was never really spent: a player could work through every object in a
  room and still have attempts left, so nothing had to be chosen over anything else. Rounded up
  because flooring lands a starting character on exactly 1, which buys the one thought and nothing
  else — no focus observation, no Speak-About hand-off (that spends the speaker's own point).
- **Combining a tool costs nothing.** It is part of the action already on screen, and only one item
  may ever be combined with it. Charging a point made the five tool-gated verbs (`dig`, `mine`,
  `fish`, `cut_wood`, `break`) *impossible* once the pool shrank: the thinking phase that produces
  the action spends the point "Use Tool" then wants, so the option was offered and permanently
  greyed out.
- **A failed action does not refill it.** `CloseNarrationSegment(refillNoetic:)` is false on exactly
  one path — the CONTINUE that closes a segment after a failed action (`_pendingSegmentSucceeded`).
  Refilling there meant a miss cost nothing, since the very next press handed the whole budget back.
  Every other boundary (an area move, a fight, a conversation, a hand-off) refills as before.
- **Nothing is offered twice in one phase.** Two ledgers, same lifetime, cleared together in
  `CloseNarrationSegment`:
  - `ObservationLedger` — what has been *looked at*. Keyed by **instance**, so two same-named things
    stay two things (which is what makes a second corpse reachable). Note `SceneFactory` merges
    same-named PoIs within an area at build, so ordinary scenery never has twins.
  - `ActionProposalLedger` — what has been offered a **button**. Keyed by **verb + target +
    variant**, *not* by instance: `RefreshSceneVerbs` re-expands every scene verb list immediately
    before each thinking request, so the `VerbAction` recorded and the one filtered are never the
    same object. Recorded when the button reaches the screen, so a goal the *action* modus mentis
    refused stays available — that refusal was one mind's, and asking again with another is the move
    that should work.

The compensations for a smaller pool are that last rule plus a **visible decline**: the goal choice
offers "do something else entirely" to the persona itself, not only to the match critic as a hidden
catch-all. With the goal list shrinking request by request, a mind left holding two leftovers it
cares nothing for needs a way to say so instead of committing to one. When the list empties
completely the ignore branch fires with no question asked at all — the same "nothing here worth
doing" line in the thinking modus mentis's voice.

**A player who spends the pool without succeeding can only leave.** Keyword clicks are gated on
`ThinkingAttemptsRemaining > 0`, so at zero the footer LEAVE button is the way out. That is the
intended shape, not an oversight — but it is why LEAVE must stay ungated by anything except a Visual
threat.

### Checking the outcome catalogue

```bash
dotnet run -- --outcome-audit
```

The consequence-side counterpart to `--verb-audit`. The catalogue is built by **reflection** over
`Outcome`, so a new one is covered the moment it is written and there is no list to maintain —
`OutcomeId` is derived from the type name (`ItemAcquisitionOutcome` → `item_acquisition`). What
*produces* each is found by sweeping real scenes the way `--verb-probe` does, then reading every
dialogue tree's outcome sets.

It prints the catalogue, who produces each outcome, and the same table **per verb and per tree** —
which is the direction somebody writing a test actually asks in ("what should my `success.cli`
assert?"). The sweep covers a verb's whole surface, not just its successes: `FailureReports`,
`FailurePenalties` (which is where `wound_infliction` hides — a wound is *sampled*, so nothing in the
reports lists shows a verb can hurt you) and `ResolveGrantedModusMentisId` (the lesson every verb
teaches, applied by `NarrativeController` rather than returned by the verb — the two most common
outcomes in the game were missing from the table until it read this).

It warns about three faults that are silent at runtime:

- an `Outcome` subclass nothing produces — dead content;
- a verb whose `SuccessReports` comes back empty everywhere. It still rolls, still prints SUCCESS,
  still satisfies `expect-verb`, and changes nothing;
- an outcome with **no `cli/outcome/<id>/` folder** — nothing proving its chip corresponds to a real
  change. Deliberate exceptions go in `OutcomeAudit.NoTest` with a reason; the five there all belong
  to the childhood and get-up phases, which every script skips with `--skip-childhood`.

Two details keep those warnings worth reading. The sweep calls the **view-aware** `SuccessReports`
overload once per expanded view, because a verb that splits one target into several actions decides
its outcome from the view's `Variant` — `introduce_me` needs to know *which* third party, and the
target-only overload comes back empty. And outcomes reached by paths the sweep cannot walk (a phase
transition, a failure branch, `tame`'s appeased beast) are declared in `OutcomeAudit.Elsewhere` with a
reason, rather than left as noise to be ignored.

### Corpses

A kill spawns a `CorpsePointOfInterest` in the area it happened in, through
`Scene.AddPointOfInterestToArea` — the one door both routes come through, the `slay`/`attack` verb
applying `NpcSlaynOutcome` and a won fight spawning one body per dead enemy. A human additionally
leaves a plain belongings PoI holding what they carried. Tiny creatures leave nothing.

**A corpse is an ordinary area PoI, not a place you enter.** Its `Items` are the harvestable parts,
and `SyntheticObservationObject` folds a PoI's items into its own sub-outcomes — so one keyword on
the body offers "cut the wolf fang" and "cut the animal hide" together, in a single phase. Identical
parts collapse to one goal (`PersonaChoiceSelector` de-dupes by label): a pig with five `PorkMeat`
offers one "cut the pork meat", and each cut removes one instance while the goal remains.

The verb split needs no per-item reasoning: everything in a `CorpsePointOfInterest` is flesh, so
`cut` takes it and `grab`/`steal` refuse it (`ItemPickup.FindHoldingPoI` filters the type, and
`StealVerb` repeats the filter); everything in the belongings PoI beside it is cloth and steel, so
the pickup verbs take it and `cut` — which requires a `CorpsePointOfInterest` — does not.

Two rules the rest of it depends on:

- **A spawned PoI is not yet observable.** The narration graph is built once, from the areas as the
  factory left them, so anything the game spawns during play has no observation object — and an
  object with no observation object cannot be looked at or acted on, however correct it is in
  `area.PointsOfInterest`. `NarrativeController.SyncSpawnedObservations` reconciles the two before
  every observation phase and every thinking request (it runs from `RefreshSceneVerbs`). That sync is
  why a corpse is reachable at all; without it the body existed and the player was never told.
- **A corpse opens the next narration phase, alone.** `PendingCorpseObservations` collects what fell,
  `GenerateObservationsAsync` drains it, and `GenerateCorpseObservationAsync` observes every body in
  the order it fell — first plainly, each later one through a transition — with no persona choice and
  no length gate, the way the post-dialogue opener imposes the person just spoken to. It runs *ahead*
  of the threat opener: a corpse is a one-shot event consumed on the spot, while an enemy still
  standing leads the phase after this one anyway. The list is drained whether or not it is used, so a
  body left in an area the player has since walked out of cannot open a phase two moves later.

Two bodies in one room are two same-named PoIs, deliberately unmerged. They behave like any other
pair of identical objects — the observation choice list keeps one representative per phase and the
ledger retires instances, so the second becomes observable once the first has been seen. This is the
one place `Scene` does *not* follow `SceneFactory`, which merges same-named PoIs at build time.

Historical note: a corpse used to be an enterable `Spot` holding one PoI per body part, and that was
the only content the `Spot`/`PoV.InSpot` axis ever had — no factory built one. The axis is gone.
Nothing in `design/` ever planned other spot content; the `# spots` sections in the location design
docs use the word in its pre-`c3fef5a` sense, meaning what is now a `PointOfInterest`.

### Crime: what makes an act illegal, and what it costs

**Legality is contextual, and `Verb.IsIllegal(scene, pov, target, actor)` is the only place it is
decided.** Sealed, like `IsPossible` and for the same reason — the setting test (standing in a private
area makes *anything* trespass) applies to every verb at once, and three call sites could each forget
it. The verb's own half is `protected IsIllegalFor`. What that buys:

- `attack` / `slay` against somebody who **already counts you an enemy** is not a crime — the quarrel
  was declared before the blow. `murder` (on a sleeper) stays unconditional: a sleeper has declared
  nothing;
- `unlock_door`, `slip_into` and `break` ask `PrivacyModel.ReachesPrivateArea(scene, target)`, **not
  `pov.Where`**. A house door is listed in the street's PoIs *and* the room's, so reading the actor's
  own area would call a burglary from the street lawful. A public storehouse's lock is nobody's
  privacy.

`PrivacyModel` answers "does this object reach into somebody's private space?" — from a connector's
own two endpoints where it has them, else from whichever areas list the PoI.

**Two rule families, and the split matters.** `ActionRulesChecker` (`IActionRule`) judges an action
already chosen: it *rejects*, the player is told why in the modus mentis's voice, and it costs a
noetic point. `ChoiceRulesChecker` (`IGoalChoiceRule`, `IWillingnessRule`) narrows what is *offered*:
it *withholds*, silently, and nothing is spent because nothing was refused. Character belongs in the
choice rules ("that never occurred to me"); circumstance belongs in the action rules ("that man is
watching"). Both are ordered lists of tiny classes — a new rule is one file and one line.

Morality now has four distinct behaviours, two per side of the chain:

| | thinking MM | action MM |
|---|---|---|
| **High** | crimes are dropped from its goal list (`HighMoralityAvoidsCrimeRule`) — filtering everything away is a legitimate result and reads as ignore | refuses outright (`IllegalActionHighMoralityRule`) |
| **Low** | if any goal on offer is a crime, **only** the crimes are offered (`LowMoralityPrefersCrimeRule`) | cannot refuse — the decline option is withheld, leaving eager/willing/reluctant (`LowMoralityNeverRefusesCrimeRule`) |

Both sides are needed because the goal and the means are two separate LLM decisions: a scrupulous
skill can still be picked to carry out a goal some other modus mentis chose. Note `MoralLevel` is read
**only** off these two — R13 makes a non-Medium value on anything else fatal.

**Getting caught is deterministic, and it is two separate questions.** Keeping them apart is what
makes discreteness mean something:

1. **May I act at all?** Asked by the coded rules against **raw** proximity. Somebody in the room
   blocks a non-discrete MM outright. A **discrete** MM may always attempt — that permission is the
   whole of what `ActsDiscretely` buys at close range (and `CanBeUsedUnderThreat` verbs are likewise
   never blocked).
2. **What does failing cost?** Asked against **effective** proximity — `ProximityModel`, where
   discreteness applies. It silences you *through a wall*, never *in the same room*: `Audio → None`,
   `Visual → Visual`.

Three rungs of one ladder, identical for witnesses and enemies except in what the top rung is:

| effective | witness | enemy |
|---|---|---|
| **Visual** | caught red-handed → the tree | they attack, on their initiative |
| **Audio** | **they come to look** — `Scene.DrawNpcTo` moves them into your area | same |
| **None** | nothing | nothing |

**The Audio rung is an approach, not a verdict.** Nothing is confronted yet; what it costs is that
they are now standing in the room, which closes the free exit and makes the *next* slip a caught one.
That needs real state, because position is resolved from `NpcSchedules` and
`SceneNpcPlacement.PlaceForPeriod` re-derives every NPC observation object from it on **every**
segment — so somebody moved any other way is put straight back one phase later, exactly as a tamed
beast's stale observation object is. `Scene.DisplacedNpcs` is consulted inside `GetNpcsAt` and
`GetAreaOf` instead: one override in one lookup, and the placement, the verb gates, both selectors and
the exit button follow without knowing it exists. A displaced NPC also reads as going nowhere all day,
so `stalk` correctly declines to follow them. One visit's business — what outlasts it is whatever the
confrontation turned into.

An arrival opens the next observation phase, **ahead of the corpse opener** (both are one-shot events,
but an arrival is the newest and has just changed what is safe to do), and only if they are still in
the player's area — announcing somebody who came to a room nobody is in reads as materialising.

A catch opens `CaughtRedHandedTreeFactory`'s tree, and **its failure outcome is always a fight** — the
witness's nerve used to be a per-archetype `IsBrave` flag whose timid branch wrote the same affinity
level the success branch did, so against a third of the game losing that conversation cost exactly
nothing. `IsBrave` is gone; `AuthorityLevel > 0` now carries the two things it also did (who joins a
fight as an ally, and the fight AI's aggression).

The consequence **lasts**, because `NpcEnemies` persists (see "What survives a visit"). Walking out
of the fight ends the fight, not the enmity: come back and they are still a Visual threat, the footer
button is RUNAWAY, and any failed action under threat starts it again.

**Only a Visual presence gates LEAVE.** `ComputeExitContext` tests `== Visual` on both branches, so
neither the Audio tier nor discreteness touches the footer button — the exit is a property of who is
in the room with you. The witness branch additionally needs a *reason*: either the area is private, or
**they came in here after you** (`DisplacedNpcs`). Without that second condition the three crimes that
happen in the open — `pickpocket`, `stalk`, `attack` — would draw a witness across a public square who
then watched you stroll off.

There is deliberately **no criminal record**. One existed — written, escalated and cleared, with not a
single reader anywhere — and `CriminalAffinityType` survives only to word the confrontation.

**Earshot is not the area graph.** `SceneAdjacency.WithinEarshot` is the shared definition used by
both `WitnessSelector` and `ThreatSelector` (which each had a private copy of it). It walks graph
edges in both directions *and* the far side of every connector — because a gate connector carries no
`AreaGraph` edge by design, and reading the graph alone made two rooms joined by a door non-adjacent.
That erased the whole Audio tier indoors: of 3618 sampled private-area situations, **every** witness
was Visual (blocked outright) and **not one** was Audio, so the confrontation could not happen inside
a building at all. A door is a gate, not a wall. `--crime-audit` prints the tier distribution and
fails if Audio is ever empty again.

### Checking the crime system

```bash
dotnet run -- --crime-audit
```

Assertion-shaped rather than statistical, because these are rules with right answers. It checks
contextual legality against real built scenes (a verb × context truth table), the choice rules at each
morality, the two-question discreteness table (may I act × what failing costs), the witness-tier
distribution, the approach (a drawn-in NPC is really there, at every period, and survives the
placement pass), and that enmity survives both a scene rebuild and a `ToJson`/`FromJson` round trip.
Run it after touching a verb's legality, a choice rule, `ProximityModel`, `PrivacyModel`,
`SceneAdjacency`, `Scene.DisplacedNpcs` or `LocationInstanceState`.

`cli/crime.cli` is the live counterpart — it drives a full illegal chain from inside a bedroom — but
read its header first: `--playground` cannot exercise the willingness rules (it answers the persona-fit
question without asking it), and which witness tier fires is seed-dependent. The audit is what covers
those.

### Landscapes: the one way to move without walking

A `LandscapePointOfInterest` is **a road you can see but cannot yet be on**. It hangs in an area you
had to climb to, points at another area of the same location, and `voyage_toward` walks it. It is a
`ConnectorPointOfInterest` like a door or a stair, with two differences:

- **listed at the viewpoint only** (`AttachToViewpoint`, not `AttachTo`), because a road visible from
  a rooftop is not visible from the street. Travel is therefore **one-way**: you come back by the
  ordinary graph;
- **`AllowsGraphEdge` is true**, unlike every other connector. Those are *gates* — crossing them is
  meant to cost a roll, so a graph edge beside one silently makes them decorative, which is what the
  building audit checks for. A landscape gates nothing; it is a shortcut earned by the climb, and the
  destination is expected to be walkable by road as well.

Observing one offers the journey. There is **no looking step**: this replaced a pair of verbs where
`observe_horizon` searched a horizon to record what could be seen and `go_toward` walked to it. What
that extra roll bought was a set of area ids written onto the `PoV` — and `RefreshSceneVerbs` builds a
*throwaway* `PoV` to re-gate verbs against, so the reveal worked, the knowledge was written, and the
verb that depended on it read an empty set every time. `go_toward` was unreachable in the shipped
game. Searching a scene for what is worth looking at is the narration system's job and it already
does it; a landscape is an object in an area, refreshed like any other, with no second copy to
disagree with.

**Placement is manual, per factory** — there is no automatic pass. A view over a location is a claim
about that location, and the one thing automatic placement reliably produced was a cave entrance
reporting that "the country opens out below". Today: a mountain's and a peak's topmost area, a
forest's giant-tree crowns, and **every building's roof** (reached by scaling its outside wall, seeing
every other outside area — roofs do not see other roofs). Caves have none by design. Climbable areas
that had nothing to offer once climbed were deleted rather than given a view.

Examining a landscape teaches `topographia`; contemplating one teaches `aesthetic`; the voyage itself
teaches `voyage`.

### Recruiting: taming a beast, and the two-step it belongs to

Both routes into the party — `tame` for a beast, the `propose_to_join` dialogue for a person — end in
the same three writes: the NPC's `EnemyCombatant` is added to `Protagonist.CompanionParty` (nothing
is copied: an `NpcEntity` wraps the combatant, which *is* a `PartyMember`), `NpcEntity.IsAlive` is set
false so `Scene.GetNpcsAt` stops returning them, and the `SceneNpc` is dropped from `scene.Npcs` and
`scene.NpcSchedules`. `RecruitedOutcome` does it for the verb, `RecruitFromDialogue` for the tree; the
ceiling (`max_companions`, heart-derived) is re-checked in both, because a companion can be picked up
between the offer and the roll and quietly exceeding the cap is worse than declining.

Two things make the removal actually show:

- **`tame` requires an appeased beast, not merely a non-hostile one.** `AffinityTable.IsAppeased` is
  "not an enemy *and* at Suspicious" — which only `appease` and the reconcile tree produce. So a wild
  beast takes two rolls at two separate phases, and `tame` is not even offered until the first lands.
  A beast that was never hostile is not tameable at all: it sits at Stranger, which `appease` (enemy
  or AnnoyingAcquaintance only) will not touch either.
- **The observation object goes with the NPC at the next segment boundary, not at the outcome.**
  `SceneNpcPlacement` owns one `SyntheticNpcObservationObject` per scene NPC and re-places them all
  in `PlaceForPeriod`, which runs from `ApplyTimePeriod` — and `StartObservationPhaseWithHistory`
  re-applies the current period precisely so every new segment re-places NPCs. Within the segment
  that tamed it the stale object is still in the node; it is harmless (every verb gate reads
  `GetNpcsAt`, so only IGNORE survives the refresh, and the segment is over anyway), and the phase
  after it is gone. `cli/tame_beast.cli` is the regression script — `state` drops one observable
  across the tame, and `--observe-only` on the beast reports nothing matching afterwards.

### What survives a visit

A scene is **rebuilt from its factory on every arrival** and thrown away on leaving, and the build is
a pure function of the location id (`CreateSeededRandom(locationId)`) — so the same rooms, the same
people and the same animals come back *identical*, down to names, stats and belongings. Anything the
player changed therefore has to be recorded in `LocationInstanceState` or it did not happen.

That class is the whole of a location's permanent memory, and a save file will read and write exactly
its fields. Four rules govern anything added to it:

- **plain data only** — dictionaries, sets and lists of primitives or enums, so it round-trips
  through `ToJson` with no custom converter. A `Guid` is re-minted by the next build and would point
  at nothing;
- **keyed by a stable id** — `INpcEntity.PersistentId` for a person or an animal (the name-derived
  `{archetypeId}_{name}`, *not* `NpcId`, which carries a random number for anyone non-persistent),
  `ItemElement.DepletionKey` for a picked slot;
- **mutable in place, shared by reference** — `LocationInstanceState.AttachTo(scene)` hands the live
  collections to the scene, so gameplay writes land in the saved state with no save step. This is why
  it is a class and not a record: a `with`-copy would silently detach a store the scene still writes;
- **wired in one place** — a new fact is one property plus one line in `AttachTo`.

Four facts live there today: `NpcAffinity`, `NpcEnemies`, `ItemDepletions`, and `DepartedNpcs` —
everyone this location has lost for good. Corpses, wounds dealt and forced doors are deliberately
*not* there: they are one visit's business.

`NpcEnemies` is separate from `NpcAffinity` because enmity is not a point on the affinity ladder —
an enemy has an affinity level as well, and the two move independently (the reconcile tree clears the
flag and leaves you `Suspicious`). Both are handed to the same `AffinityTable` by `AffinityFor`, which
shares *both* backing stores. Sharing only one is what made running away the dominant answer to every
fight: the scene was rebuilt on arrival, the grudge was not rebuilt with it, and the man who drew
steel on you last visit greeted you as a mild acquaintance.

**Every departure goes through `Scene.RemoveNpcFromPlay`** — the slay/murder verbs, a won fight,
`TameVerb`'s `RecruitedOutcome` and the `propose_to_join` tree. It does the three writes that make
someone gone from this visit (not alive, out of `Npcs`, out of `NpcSchedules`) and the fourth that
makes them gone from every later one (the id into `DepartedNpcs`). `SceneFactory.Build` drops those
ids straight after `BuildNpcs` and before `PlaceBeastSign`, so a departed beast leaves no tracks to
follow to nothing. Departure is **permanent**: a replacement drawn from the same seed would be the
very individual that left, so the spot in the world stays empty.

**A scene factory is built per scene, never reused.** Each one keeps working state while it builds —
the area list it wires paths through, a village's workshops and houses — and none of it is meant to
outlive the scene. `_sceneFactories` therefore holds `Func<SceneFactory>`, not instances. Sharing one
instance let that state pile up: the second build of a plain ran its path wiring and its beast
placement over eight areas, four belonging to a scene that no longer existed, and since the two
builds produce identically-named areas the corruption was invisible in the log. The audits never saw
it because they always built one factory per location — which is now what the game does too, so a
green audit finally covers the path the game takes.

### What a body can do

A companion may be a beast, and after a Speak-About hand-off a beast **narrates**: it observes,
thinks, chooses goals and acts. Two things therefore decide what a party member may learn and do, and
they are decided separately:

- **Organs — free, no authoring.** Every id in a modus mentis's `Organs` must resolve on the body.
  A human cannot learn a `fangs` skill; a beast cannot learn a `hands` one. Note the ids are shared
  where the anatomy is: `LegsOrgan` and `BeastLegsOrgan` both answer `"legs"`, `BeastClawsOrgan`
  answers `"claws"`, and both trunks answer `"trunk"` — so anything reasoning about this by class
  name is wrong. `ModusMentisAnatomy.SourcesOf(anatomy)` is the list, built from the anatomy factory.
- **Capabilities — authored.** A wolf owns a tongue and a cerebrum, so nothing structural keeps
  rhetoric or lockpicking away from it. `AnatomyCapability` is three flags — **Speech** (conversation,
  not voice), **Handcraft** (tools, locks, carrying, fine work), **Abstraction** (letters, number,
  institutions) — declared **per anatomy** on `IAnatomyFactory` (Human has all three; Beast has none)
  and **required** per piece of content (`ModusMentis.RequiredCapabilities`,
  `Verb.RequiredCapabilities`, both defaulting to `None`).

The direction matters: content declares what it *needs*, anatomies declare what they *have*. Adding
an anatomy is one line on its factory — no revisiting 180 modi mentis and 54 verbs.

Three consequences worth knowing:

- **`Verb.IsPossible` is sealed**; the per-verb condition moved to `protected IsPossibleFor`. The
  capability test lives in the sealed half so the routine replay engine, the verb audit and the debug
  window cannot each forget it. The whole `DialogueVerb` family declares `Speech` once, `ExtractionVerb`
  declares `Handcraft` once.
- **The gate reads the acting member, not the protagonist.** `RefreshSceneVerbs` passes
  `_activePartyMember`, and the actor parameter widened from `Protagonist?` to `PartyMember?`
  throughout (`Scene.View`, `VerbRegistry.GetApplicable`, `IVerbRefreshable.RefreshVerbs`). The two
  places that genuinely need the protagonist — the `max_companions` ceiling in `TameVerb` and
  `propose_to_join` — do `actor as Protagonist`. `RoutineReplayEngine` used to pass
  `ActingMember as Protagonist`, i.e. **null** for every companion, skipping every actor-dependent gate.
- **Learning is refused, not capped.** An MM naming an absent organ contributes +0 to the level cap,
  so it used to be grantable and stuck at level 1 — held, useless, unexplained. Every grant path now
  asks `ModusMentisAnatomy.IsLearnableBy` first: `NpcContentGenerator` filters the three pools *before*
  sampling (so an archetype still gets the count it asked for), `NpcSkillGrant` refuses, and
  `ModusMentisGrantOutcome` teaches nothing when the acting body cannot hold the lesson. A global
  personality trait offering a skill the drawn anatomy lacks is normal and logged; an *archetype's own*
  trait doing it is a content fault, and `--npc-audit` names it.

`cli/beast_verbs.cli` is the regression: the same flower patch, looked at by the protagonist (offered
"gather a daisy") and then, after the hand-off, by the cat (`goal gather` finds no such goal, and the
phase runs on "examine the flower patch closely" instead). `--verb-audit` prints what each anatomy is
barred from — 25 of 54 verbs for a beast.

### A worked example

This is the script that verified the phase-transition refactor. The assertion is in the `state`
lines either side of the transition: `history` must go from 0 to non-zero (old text greyed into
history) and `noetic` must return to full.

```
wait
click menu New
wait mode ProtagonistCreation 20
click continue
wait mode WorldView 60
travel here                 # enters the location under your feet; never hard-code a vertex
                            # number — it moves the moment anything upstream of spawn changes
wait mode LocationInteraction 60

strategy succeed
click keyword attention     # keyword -> Think -> pick a modus mentis
choose 0
choose 0
advance 8 120               # drain the preview box; the action list is behind it
state                       # → preview=none, and `regions` now lists actions

click action 0              # execute -> dice -> confirm
choose 0
advance 10 150
click continue
advance 10 150
expect Skill learned        # every successful verb teaches a modus mentis
dump
quit
```

### The test suite

```bash
./run_tests.sh              # every audit, then every CLI script
./run_tests.sh audits       # the headless audits only (seconds) — eight of them
./run_tests.sh cli          # the CLI scripts only
./run_tests.sh cli gather   # just cli/gather/
```

Exit code is the number of failures. `--playground` makes every animation instant
(`Config.AnimationsAreInstant`: the 5-second dice tumble, the vital-heat bar, the fight AI's pause,
movement framing), which takes a script from ~45 s to ~12 s — the difference between a suite you run
and one you don't.

**Layout: three ranges, and each answers a different question.**

```
cli/verb/<verb-id>/success.cli    the verb carried through   (strategy succeed  → expect SUCCESS)
cli/verb/<verb-id>/fail.cli       the verb attempted, missed (strategy fail-dice → expect FAILURE)
cli/verb/<verb-id>/success2.cli   a second script where the verb has genuinely different outcomes
cli/outcome/<outcome-id>/success.cli   one per entry in the outcome catalogue
cli/system/                       scripts that test a system rather than a verb or an outcome —
                                  the preview box, the Speak-About hand-off, the crime chain,
                                  routine recording and replay
```

`./run_tests.sh cli verb` runs a whole range; `./run_tests.sh cli gather` runs one folder inside one.

**The two ranges compose, and that composition is the point.** A verb test asserts that the verb
declares the right outcome; an outcome test asserts that the outcome really changes the world. Put
together they say the verb changes the world — without either range having to know both halves:

```
cli/verb/gather/     GRAB the bramble  →  "Item received: …" is printed
cli/outcome/item_acquisition/          →  after that chip, the item IS in the pack
```

They overlap on purpose: the same scenario, asked two different questions.

**Where the composition needs care.** It covers the *mechanism* but not the *arguments*.
`AreaMoveOutcome(destination)` takes its destination from the verb, so a verb producing
`AreaMoveOutcome(wrong_area)` would satisfy both halves. That is why a verb test asserts the **whole
chip**, `Moved to: Test Wood`, and not just its prefix — the test location's names never change, so
the argument is assertable there even though it is not in the real world.

**Every new verb arrives with its folder and at least those two scripts; every new outcome arrives
with a `cli/outcome/<id>/` folder.** Both are enforced: `--verb-audit` names a verb nothing offers,
and `--outcome-audit` names an outcome with no test folder. Add a third verb script when the outcome
genuinely forks (`cli/verb/gather/beast_barred.cli` is the anatomy gate, which is neither a success
nor a failure of the roll).

**The test location.** Every script in the suite runs in one place: `--location-type test`, built by
`src/game/scene/test/TestSceneFactory.cs`. It is a kitchen sink — ten areas holding one of every kind
of thing the game has, and covering **44 of the 54 registered verbs** from a cold start.

It exists because the alternative did not survive development. Scripts used to run against the real
factories and name what they aimed at (`--start-area "Alehouse Store"`, `--observe-only "Shelving
Rack"`), and those names are *rolled*. Adding a single `rng.NextDouble()` to the middle of
`BuildingFactory` — one new content roll — re-seeded every draw after it, renamed the buildings and
the people in them, and broke six unrelated tests. Moving the roll to the end of the method fixes it
exactly once, until the next piece of content.

So **nothing in this factory is rolled.** The `rng` handed to `BuildSections` is deliberately never
touched: every area, object and description is written out, and a script pinned to `"Test Yard"` stays
pinned to it forever.

Two things follow from that, and both matter:

- **It is not content, and the audits do not sweep it.** `--verb-audit` covers the nine real factories
  only. If a verb becomes reachable *only* in the test location, the audit's dead-verb warning is
  exactly what should say so — whether the real world places a lockable door, an ore seam or a
  sleeping NPC is that audit's job, not a verb test's.
- **The types are the content.** Verbs gate on a *subclass* or a reference lemma — `dig` wants a
  `DiggableGroundPointOfInterest`, `mine` an `OreVeinPointOfInterest`, `cut_wood` a lemma of
  `tree`/`log`/`stump`, `fish` a water crossing with `HoldsFish`, a sleeper a bed whose lemma is
  `pallet`, and every extraction verb also wants items still on the target. A plain `PointOfInterest`
  named "Test Ore Seam" reads correctly and is offered to nobody; the first draft of the file was
  written that way and covered 25 verbs instead of 44.

The ten it does not cover are situational rather than missing: `get_up` and `remember` are phase-only,
`cut` needs a corpse and `tame` an appeased beast (their scripts build that first), and the six
affinity-gated dialogue verbs are invisible to `--verb-probe` because affinity is applied when a scene
is attached to its instance state, long after the factory build the probe inspects.

Four things that are not obvious until a script hits them, all of them learned by a test failing for
a reason that had nothing to do with its verb:

- **A hostile beast opens the next observation phase wherever it stands**, ahead of `--observe-only`.
  A wolf in the wood therefore hijacked every phase that started there, and `cut_wood`, `scale_up` and
  `stalk` all ran `appease` on it instead of the verb they name. The wolf has a den of its own now, and
  only the scripts that want it go in.
- **A verb that produces an item is refused by the carry rule before it reaches the dice** if nothing
  carried can hold the result — "I have nowhere left to put a log", with no roll and no banner. A `Log`
  is `ItemSize.Large` and needs a container, so `cli/cut_wood/` grants `axe,sack`. This is not about a
  full pack: emptying it changes nothing.
- **A landed `attack` prints no outcome banner.** It starts a fight, and the fight screen replaces the
  narration before the banner can be read, so `wait mode Fighting` is the assertion. `expect SUCCESS`
  is right for the *failed* attack, which stays in narration.
- **Never `--observe-only` a generated name.** `--verb-probe` reports an NPC's display name because
  that is what it has, but names come from the name generator — content, free to change. Use the
  archetype id (`brewer`, `weaver`, `plowman`), which is what `TargetingAliases` accepts — including
  for a **sleeper**, who is observable only as the merged `SleepingNpcPointOfInterest` and now hands
  through the species and archetype id of the person inside it. Same for assertions:
  `cli/tame/success.cli` asserts the `P A R T Y` header rather than the beast's name.

**Adding a verb? Add what it needs to the test location, and its `cli/<verb>/` folder.**

**Each script carries its own flags** on a `# FLAGS:` header line, which is what the runner executes:

```
# FLAGS: --playground --skip-childhood --debug --seed 42 --no-encounters \
#        --start-at mountain --start-area "Boulder Field" --period dawn --observe-only "Boulder"
```

A test that needs a particular biome, room, hour or object says so in the one place a reader looks.

**Finding those flags: `--verb-probe`.** Do not hunt for a seed.

```bash
dotnet run -- --verb-probe
```

It sweeps every factory × location id × area × period × object and prints, per verb, the situations
that offered it — as the flags themselves:

```
── gather
     [12/12 ids] --start-at mountain --start-area "Boulder Field" --period dawn --observe-only "Boulder"
```

Two things about that output are load-bearing:

- **The `[12/12 ids]` count.** Area and object names are *per location id* — a village rolls a Chain
  or a Hub layout with different rooms — so a combination found in one id opens somewhere else on
  another seed. Prefer the ubiquitous ones; anything thin is a script that will fail when something
  upstream of spawn changes.
- **It reports the observable, not the target.** An item is never observed in its own right
  (`SceneViewAdapter` folds an item's verbs into its holding PoI's action list), so `--observe-only`
  only ever matches the holder. Pinning to `"Rock"` when the rock lives in a `"Boulder"` matches
  nothing and silently opens on the whole scene.

It also lists the verbs no cold start reaches, with the reason (`tame` needs an appeased beast, `cut`
needs a corpse, `request_job` needs an acquaintance). Those need a script that builds the situation
first — `cli/tame/success.cli` appeases before it tames — or a debug flag that sets it up.

**Assert the verb, never the banner.** `expect SUCCESS` matches the outcome banner of *any* action,
so a script aimed at `break` that opened on a path, followed the path and succeeded, passed. Sixty of
them did. **`expect-verb <verb-id>`** reads the verb id off the action that actually ran, and is what
makes a verb test a verb test. It cannot survive a scene rebuild (a new `NarrativeController` resets
what it recorded), so anything crossing a location boundary must assert some other way.

**And assert the outcome, not just the roll.** The banner is printed from `result.Succeeded` — the
dice — and says nothing about what the verb *did*. A verb whose `SuccessReports` stopped returning
anything would still roll, still print SUCCESS, still satisfy `expect-verb`, and gather nothing. So
every `success.cli` also asserts the chip its outcome puts in the outcome block:

| outcome | chip | verbs |
|---|---|---|
| `ItemAcquisitionOutcome` / `ItemGrantOutcome` | `Item received:` | gather, grab, steal, cut, and the four extraction verbs |
| `AreaMoveOutcome` | `Moved to:` | move, the climbs, the crossings, voyage_toward, track, stalk |
| `NpcSlaynOutcome` | `Slain:` | slay, murder |
| `DoorUnlockOutcome` | `Door unlocked` | unlock_door |
| `RecruitedOutcome` | `Joined you:` | tame |
| `AffinityChangeOutcome` | `Appeasement:` | appease |
| `PoiReplacementOutcome` | `Broken:` | break |
| `SleeperRousedOutcome` | `Woken:` | wake_up |
| `TimeShiftOutcome` | `Time passes:` | sit_and_wait, hide_and_wait |
| `TinyCreatureRemovedOutcome` | `Caught:` / `Crushed:` | catch, crush |
| `CoinGrantOutcome` | `Coins received:` | pickpocket |
| `DialogueTriggerOutcome` | `Conversation:` | the eleven verbs that open one |
| the modus mentis grant | `Modus mentis` | examine, listen, smell, contemplate — no scene outcome of their own |

Match the **prefix** only; what follows is a name or a room and moves with the content. `Modus mentis`
is deliberately cut short of its noun: a first grant reads `acquired`, a repeat reads `learned`
(`ModusMentisPracticeOutcome`, whose `ShowInUI` is false when nothing moved).

Four verbs assert something else instead, and for a reason: **`attack`** lands in a fight, and the
fight screen replaces the narration before any chip can be read — `wait mode Fighting` is its
assertion. **`get_up`** and **`remember`** are phase transitions. **`ignore`** does nothing by design.

**Put the assertion after the verb under test, not after the setup.** `expect` scans the whole
terminal, greyed history included, so a chip left by an earlier action reads exactly like the one you
meant. `cli/cut/success.cli` slays before it cuts and asserts `Slain:` on the kill and `Item
received:` on the cut — putting both after the first action would have passed while proving nothing
about the verb the folder is named for.

**A narrowing flag that narrows nothing fails the run.** `--start-at`, `--start-area`,
`--observe-only` and `--goal-only` all *fall back* when they match nothing — open where the factory
did, offer the whole scene, draw from every goal. That is right for a person poking at the game and
disastrous for a test, which then exercises something else and reports whatever that did. Every miss
now goes through `DebugFlagAudit` and fails the run naming the flag. A script that is testing an
**absence** (`cli/gather/beast_barred.cli` checks a beast is *not* offered `gather`) declares it with
`allow-flag-miss <flag>`.

**Dialogue.** `--playground` stubs `DialogueTreeAdapter`'s LLM slot — it was the one place in the
dialogue path still reaching for the server, so before that every tree in the game was unreachable
under playground: `Setup` threw, the conversation "completed" instantly, and a verb test watched a
dialogue open and close without a word being said. `advance` drains whichever preview box is up
(narration's or the conversation's), and `strategy` now reaches the resolution roll, which it never
did — so the failure branch of a tree is finally testable. Verb tests pass `--auto-dialogue` and
assert about their verb; `cli/_systems/dialogue_*.cli` drive a real tree and assert on the branch's
success vs failure replica.

**`click keyword <n>` for pinned phases.** Which word an observation highlights comes from the prose
(under `--playground` the object's own noun), not from its display name, so a test that has pinned the
phase with `--observe-only` still cannot spell the handle — `"Brew Barrel"` is not `barrel`. Clicking
by index means "whatever this phase opened on". Name-matching stays right for hand-written scripts:
it reads as intent and survives reordering.

### Extending the CLI for a new feature

When you add UI, add its handles too, or it will not be testable:

1. Expose the hit regions your renderer already tracks (see `MainMenuRenderer.CliButtons`,
   `NarrativeUI.KeywordRegions`, `DialogueTreeController.CliOptions`).
2. Surface them on `LocationTravelGameController` as a `Cli*` member — that class is the single
   seam the driver talks to.
3. List them in `CliDriver.CmdRegions` and handle them in `CmdClick`.

If the new phase is asynchronous, make sure `LocationTravelGameController.CliIsIdle()` accounts for
it, otherwise `wait` will return while the phase is still building.

**The trap that bites first**: the narration **preview box** sits between a keyword click and the
action list. It is generated in segments — the goal, the modus mentis chosen for it, the persona's
willingness — and its CONTINUE clears *one segment per press*. A script that presses once lands
mid-stack, and `click action 0` then reports "no action 0 on screen" with nothing to say why. Use
`advance`, which settles and presses until the box is gone. `state` reports
`preview=none|generating|ready` so a stuck script can be diagnosed from its own output.

**A second trap**: `NarrativeState.IsDiceRolling` rests at `true` when no roll is active
(`ClearDiceRoll` sets it that way), so always gate it behind `IsDiceRollActive` when testing for
business. This already caused one bug in `CliIsIdle`.

### Adding a debug flag when a feature is hard to reach

**Adding new debug-only command-line options is expected, not a last resort.** If the thing you
changed sits behind a rare biome, a particular time of day, a specific roll or a long approach, do
not hunt for a seed that happens to line it up — add a flag, use it, and document it here so the
next person can reuse it.

The rules:

- Put the switch on `Config.Debug` (`src/Config.cs`) and parse it in `Program.cs` beside `--seed`.
- **It must be inert at its default.** A run without the flag has to behave exactly as if the option
  did not exist; every one of these reads `?? <the normal behaviour>`.
- Add it to the `--help` block, to the flag table above, and say what it is *for* — the next reader
  needs to know which problem it solves, not just what it sets.

`--start-at` and `--period` were both added this way: the first because villages rarely spawn within
travel range, the second because the night door rule fires in one period out of six. `destinations
all` exists for the same reason — the plain `destinations` list is only the handful of vertices
bordering the spawn point, while travel range reaches far further.

### The affinity ladder, and what gates a conversation

`AffinityLevel` is an ordinary 0–5 ladder (Stranger → Close Friend) whose value is the bonus dice a
conversation gets — **plus `Suspicious` at 6, which sits off the ladder and grants 0**. Three
consequences the arithmetic will get wrong if you let it:

- **`Adjust(delta)` is a named step, never `level ± 1`.** A step down from Stranger clamps to the
  `min` (Annoying Acquaintance), and a step either way from Suspicious would land on nonsense — so
  Suspicious names both its neighbours explicitly (up = Distant Acquaintance, down = Annoying
  Acquaintance) and **`delta == 0` returns without touching the table at all**. That last one was a
  real bug with a long tail: `AffinityIncrementOutcome(0)` means "this conversation left things where
  they were", read as a step *down*, and filed the player as an irritant on first meeting — which
  then offered `reconcile` against somebody there had never been a quarrel with.
- **Suspicious must be escapable.** Refusing to move it made it a permanent dead end: an NPC talked
  down out of hostility could never be improved again, so every later conversation with them changed
  nothing.
- **A won conversation must not leave the player worse off.** `reconcile`'s success drops an *enemy*
  to Suspicious, which is right; imposing it on somebody who merely found you annoying is a
  downgrade in dice dressed as a win, so `SuspiciousAffinityOutcome(onlyWhenHostile: true)` steps a
  non-enemy up instead. It runs **before** `ClearEnemyOutcome` in the tree's outcome list — the flag
  it reads is the one that outcome removes.

**The introduction is the gate for conversation.** `meet_stranger` is what turns a stranger into a
distant acquaintance, and everything else sits behind it: trade and work through `TradeGate`,
`strengthen_relationship` and `gather_knowledge` through their own stranger check,
`introduce_me` because asking somebody who does not know you to vouch for you cannot work in that
order. `SocialDialogueVerb.RequiresAcquaintance` is the switch; **`beg_for_coin` and `provoke` opt
out**, because each is *about* addressing somebody who does not know you — behind an introduction,
begging would leave a destitute player with nothing to say to anyone, and provoking would be
unreachable against exactly the people worth using it on.

A verb that joins this set must also be named in `VerbAudit`'s `unreachable` table and
`VerbProbe.WhyUnreached`: the sweeps build scenes with no instance state, so their stand-in actor is
a stranger to everyone and an affinity-gated verb is *correctly* never offered.

### Checking dialogue trees without running the game

Tree shape is not something a `--cli` script can see — a script walks one branch. `--dialogue-audit`
prints the whole picture instead, headless and in about a second (no LLM, no window, no world):

```bash
dotnet run -- --dialogue-audit
```

Per tree it reports player replies, NPC lines, branch ends, and branch length (min / average / max,
counted in **player replies** from the greeting to the dice check). Then it warns about the things
that are invisible until a player hits them:

- a branch shorter than 2 or longer than 4 replies;
- a `{scope:field}` token that `DialogueTemplate` cannot expand. Three scopes exist: `{you:*}` for
  the player, `{npc:*}` for whoever is being spoken to, and `{third:*}` for a person the conversation
  is *about* rather than *with* — the master an apprentice offers to present you to. A third party is
  handed to the adapter through `NpcEntity.PendingIntroductionTarget` and is name-faked like any
  other, so the LLM never sees a real name for the one person the conversation is entirely about;
- a `{scope:field}` token that `DialogueTemplate` cannot expand — or that expands to *nothing* for
  some archetype, which it checks by spawning one NPC of every speaking archetype and expanding
  every token used by any tree against it;
- a resolution whose authored difficulty matches neither `BranchDifficulty` ladder at the depth it
  is actually reached at (almost always a miscounted depth);
- duplicate node or option ids, an NPC line with no replies, or a cycle.

`--dialogue-view` remains the way to *read* a tree; this is the way to check one. Run it after
touching any tree, any archetype's dialogue flavour, or `DialogueTemplate`.

### Checking NPC generation

`--npc-audit` spawns a sample of every speaking archetype — twice each — and reports the shape of
what came out, headless:

```bash
dotnet run -- --npc-audit
```

Per archetype it prints the range of organ totals, skill counts, skill levels, item counts and
wounds across the sample, plus whether generation was **repeatable**. That column is the important
one: every NPC is generated twice from the same id and compared name-by-name, organ-by-organ,
skill-by-skill and item-by-item. Anything but `yes` means an unseeded RNG has crept back in and the
same person will differ between visits.

It also checks that every trait's modus-mentis and organ-part ids resolve (a typo grants nothing,
silently), that the pools are the intended 60 global + 6 per archetype with no duplicate ids, that
every skill is filed in a memory module and within its organ-derived cap, that no NPC holds a skill
its own anatomy cannot learn, that no **archetype-specific** trait offers one (a shepherd trait
reaching for a beast's scenting teaches nothing — global traits are exempt, being dealt to every
anatomy), and that sex agrees with the genitories score. It finishes with one fully-generated NPC printed in full, which is the quickest
way to see whether a trait you just wrote actually reads well on a person.

Run it after touching `NpcContentGenerator`, any archetype's generation block, or any trait.

### Checking the modi mentis

`--mm-audit` prints the whole MM catalogue and the health of its content rules, headless:

```bash
dotnet run -- --mm-audit
```

The hard rules (R1–R13, listed on `ModusMentisRuleValidator`) are **fatal**: `ValidateOrThrow` runs
them at startup, so a rule-breaking MM can never reach the game — the audit is how you read what
broke without launching. They cover function combinations, memory-type agreement, fighting-skill
cross-references, and the rules that are easiest to get wrong:

- **R5** — an MM's `Organs` is **exactly 1 body region XOR exactly 2 distinct organs**, canonical ids
  only, never a mix. There is no "primary" entry: every one contributes to the level cap through its
  `IMaxLevelContributionStat` (organ +0..+3, region +0..+6), so code reading only `Organs[0]` is a bug;
- **R10** — every organ and region owns exactly one correctly-scoped contribution stat. Without it
  `GetMaxLevelForModusMentis` contributes +0 and silently caps every MM related to that source at
  level 1 — a wrong number in the memory menu and nothing anywhere to say why;
- **R11** — every organ and region of **every anatomy** keeps at least 3 MMs that anatomy can learn.
  The counterweight to `RequiredCapabilities`: barring a beast from speech and letters is right, but
  done freely it leaves a wolf's heart with nothing to spend itself on;
- **R12** — every MM is learnable by at least one anatomy. Five were not when this rule was written —
  `fangs` paired with `teeths`, `claws` with `arms` — which reads perfectly well and reaches nobody;
- **R13** — a MM with neither `Thinking` nor `Action` is `MoralLevel.Medium`. Morality is only ever
  read off the thinking MM (which crimes it is offered) or the action MM (whether it will do one), so
  a principled *observation* MM is a claim nothing can honour: it reads as character in the memory
  panel, skews the 20/60/20 target, and changes nothing in play. Six were like that.

Then the soft targets, which only warn: the ~80/20 two-organ vs one-region split, the morality and
memory-type distributions, ~10% discrete, and per-organ/region coverage (R6 makes fewer than 5
related MMs fatal, so the coverage table is really about spotting the ones scraping the floor). The
report also prints a **per-anatomy** table — what each anatomy can actually learn, source by source,
which is a much smaller set than the catalogue-wide one and the only one R11 reads.

Run it after adding or editing a modus mentis, a fighting skill, an organ, or a body region.

### Checking the item catalogue

`--item-audit` reads `ItemRegistry` and reports the whole catalogue, headless. Coverage is
automatic — any item with a public parameterless constructor is audited the moment it is written:

```bash
dotnet run -- --item-audit
```

It prints the census by `ItemCategory` and subcategory, the wearable table (max armour dice per body
section, garments per social standing), liquid/vessel reachability, trade-tag coverage, the weight
distribution and what it means at each backbone tier. Then it warns about:

- duplicate `ItemId`s or `DisplayName`s, and any `Info` line that repeats the description (which
  would print it twice in the panel);
- a wearable that neither protects, nor flatters, nor holds anything — dead weight on an anchor;
- a liquid no vessel accepts, which under the strict pickup rule can never be picked up at all;
- a category or subcategory thin enough that the player sees no variety (weapons exempt: two per
  fighting medium is deliberate);
- a body section whose armour ceiling has crept past ~3 dice. **Armour is uncapped in code** — this
  line is the only thing standing between layered garments and an unhittable torso, so read it
  after touching any `DefenseDice`.

It also samples `FightResolver.PreRollHitLocation` 60k times and compares the result against each
body part's share of the wound pool. Armour needs the hit location *before* the dice, so unaimed
attacks now pre-roll one; that check is the proof the pre-roll did not change where blows land.

Run it after touching any item, `WearableItem`, `ArmorSections`, or the weight tiers.

### Checking what there is to DO in a location

`--verb-audit` is the counterpart to `--building-audit`: that one checks a location is *built*
correctly, this one checks it is *playable*. Headless, no LLM, no window:

```bash
dotnet run -- --verb-audit
```

It builds every factory across 40 location ids, asks every registered verb whether it applies to
every observable **at every time period** (presence, locks and schedules all move with the clock),
and reports verbs-per-observable against the design targets: **80% of observables reachable by ≥2
verbs, 50% by ≥3**, and about half observable by at least one sense. A figure short of its target is
marked `*`.

Then it warns about the things that are silent at runtime:

- an observable no verb accepts at any period — it is prose and nothing else. This was the state of
  most of the game's furniture: the anvil, the loom, the trestle table, the ice formation all read
  well and offered nothing but IGNORE;
- a registered verb no sampled location ever offers — dead content, usually because nothing places
  its target yet;
- a verb that teaches no modus mentis, or teaches one that does not resolve in `ModusMentisRegistry`
  (a typo grants nothing, silently — the same failure `--npc-audit` guards for in traits);
- a `ReferenceToolIds` entry no item matches, which makes a tool-gated verb permanently *impossible*
  rather than merely hard;
- a scale point or cliff whose **top** area holds nothing — a climb that costs a roll and arrives
  somewhere with no points of interest at all. This replaced a check that counted landmark areas,
  which the landscape refactor made meaningless.

It closes with the **anatomy** table: what each anatomy may attempt and what its body rules out
(`Verb.RequiredCapabilities`). Never a warning — a beast barred from 25 of 54 verbs is the design —
but it keeps the cost of that design visible, and makes the next anatomy's poverty one line to read.

Run it after adding a verb, a connector type, or a batch of scene content.

### Checking buildings, scenes and NPC schedules

`--building-audit` generates every location type across 60 location ids and checks the structural
invariants that fail *silently* in play — nothing throws, the scene just quietly misbehaves:

```bash
dotnet run -- --building-audit
```

Per factory it prints the range of areas, NPCs and doors (and how many are locked at noon), plus the
sections that were generated and how often. Then it warns about:

- an area in two sections or in none. Sections must **partition** the areas: every lookup is a
  "first section containing this area", and an area in none crashes the fight path outright;
- two areas sharing a node-id slug (`display name → lower_snake`). The narration graph keys nodes by
  that slug, so a duplicate overwrites and one room ends up with **no node** — unreachable, and
  invisible to NPC placement. This is why `BuildingFactory` prefixes every room with its building;
- two PoIs in one area sharing a display name. Observation candidates are de-duplicated by name, so
  the second is never observable and anything inside it is unreachable. `SceneFactory` merges these
  automatically now, so a warning here means something bypassed that pass;
- an `AreaGraph` edge duplicating **any connector** — door, stair, cliff, crossing, water, scale
  point. That gives `MoveToAreaVerb` (difficulty 1, never fails) a way around the gate, which is
  exactly how the village's one interior door used to be decorative — and how, until this check was
  generalised over `ConnectorPointOfInterest`, *every cliff in the game* was either bypassed by an
  edge or pointed at its own area and moved nobody;
- an entry door that is not locked at night, or a door with no rolled description;
- an NPC with `[Night] = null`, sleeping in a room with no bed, or a room sleeping more people than
  it has beds. Wilderness factories are exempt — a wolf is allowed to sleep rough;
- a public hall with nobody in it during a day period: a shop with no one behind the counter;
- stable keys that are missing, colliding, or **different across two builds of one location id**.
  That last one matters because procedural descriptions are seeded from the key, so a key that moves
  re-rolls how every door in the place looks between visits.

It finishes by printing real door prose from both sides and at night, and by driving a
`SyntheticObservationObject` directly to prove the viewing area and the period stamp actually reach
the description — a door that lost either would still read perfectly well, just always from outside
and always by day.

Run it after touching `BuildingFactory`, any scene factory, `NpcSchedule`, or the observation
description path.

### What `--cli` cannot check

- **Anything about pixels**: camera framing, the glyph atlas, sky/cloud rendering, and whether the
  3D sphere looks right. `dump` sees the terminal cell grid, not the framebuffer.
- **Ray picking**: `travel` injects a vertex index directly, bypassing
  `GlyphSphereCore.FindVertexByMagentaRayIntersection`. If that math breaks, scripts stay green
  while the game becomes unclickable. Keep one manual click-through in your release routine.
- **Narration quality**: `--playground` produces placeholder prose. A passing script proves flow,
  state and layout — never that the prompts still produce good writing.
- **Which keyword a real observation offers**: under `--playground` the clickable keyword is the
  object's **own** noun (`pig` for the pig), because placeholder prose is one frame reused for every
  object and the real rule — pick the noun most *associated* with it, excluding its own — ranked the
  frame's vocabulary instead. Every observation in a phase came out as "attention", a block maps a
  keyword to the first outcome that claims it, and so **only the first object observed was ever
  clickable**. Scripts can now click any observed object by name; what a real run highlights is still
  a different word, and only a real run shows it.
