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

And one that reaches past the party, because a field is stored **as-is** rather than captured:

- **An abstract or interface-typed field must declare `[JsonPolymorphic]` and a `[JsonDerivedType]`
  per subclass.** `System.Text.Json` writes such a field by its *declared* type, so the subclass's own
  fields go missing in silence, and it refuses to read one back at all — which
  `SaveFile.Read` catches as corruption, so **one such field makes the entire save unloadable**.
  `RoutineStep.Constraints` was exactly this: every saved routine lost its constraint data, and the
  first player to record a routine lost the save. Only `cli/system/routine_record_replay.cli` reaches
  a routine, so it is the script that carries the `save roundtrip` covering that branch —
  `save_roundtrip.cli` stacks breadth of party state by flags, and no flag records a routine.

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
of a scene. See "Escape, and the phases it must answer" below for what it does per phase.

Two things to keep in mind when touching that keyboard:

- **Gate the branch, not the chain.** In `LocationTravelModeLauncher`'s `KeyDown`, the D and G
  branches test `DeveloperKeys` as part of their own condition. Short-circuiting the whole
  `else if` chain instead would swallow every non-Escape key before it reached the final `else`,
  which is what forwards keys to fight and dialogue modes — taking the keyboard away from gameplay.
- **`--no-developer-keys` makes a development build behave like a shipped one.** Keys cannot be
  driven by `--cli` at all, so this flag plus the `Developer keys: …` line printed at startup is
  the only way to check the shipped keyboard without building and hand-testing a shipped exe.

### Escape, and the phases it must answer

**`LocationTravelGameController.HandleEscape` is the whole rule, and every `GameMode` is listed in
it — including the ones that deliberately do nothing.** The main menu is the only screen carrying an
Exit button, so a phase Escape does not answer is a phase from which the game cannot be quit; and
the gap this replaced was a phase nobody had thought about rather than one somebody had decided
against. Silence is therefore not allowed to mean "no": a mode that should not reach the menu says
so, and says why.

| | Escape |
|---|---|
| Fighting | cancels the armed skill or move target; pauses when nothing is armed |
| LocationInteraction, ChildhoodReminescence, GetUp | closes the thinking popup; otherwise pauses **as an overlay**, leaving narration standing |
| Dialogue, Working, Trading, EncounterPrompt, WorldView | pauses. None of them is cancelled by it — walking out is the footer INTERRUPT / LEAVE / ENGAGE button's job |
| ProtagonistCreation | pauses. Not a running phase, but a screen with no other way off it |
| ProtagonistManagement, Settings | back to the main menu — the same press as their Back button |
| MainMenu | resumes `MenuReturnMode`, or nothing at all before a run has started |
| LLMLoading, Traveling, Death, DialogueDemo | **nothing, on purpose** — see the reasons on each case |

Three things that had to move with it, and each was its own bug:

- **The CLI's `pause` calls `HandleEscape`.** It used to carry a second, simplified copy of the rule
  ("anything that is not the menu opens the menu"), which is a test that cannot fail: `pause` opened
  the menu during childhood reminescence, so a script saw a working pause menu in the one phase
  where a player pressing Escape got nothing. Two copies of a mode list means the CLI is testing a
  paraphrase. `cli/system/pause_early_phases.cli` and `pause_later_phases.cli` walk the phases a
  script can reach.
- **`MenuReturnMode` covers the narrative phases separately.** Exploration, childhood and get-up all
  run on one `_narrativeController` with `_isInNarrativeMode` set, and are otherwise
  indistinguishable — so the flat `LocationInteraction` it used to return would have resumed
  childhood *into exploration's mode*. `_narrativePhase` records which of the three is live. Two
  more phases are derived from the object that draws them: `_protagonistCreationRenderer` and
  `_pendingEncounterNpc`.
- **`OnEnter…` must distinguish resuming from starting.** Every phase whose `OnEnter` builds a scene
  throws the previous one away on a second entry — so resuming childhood from the menu would have
  restarted it from its first reminescence. `ResumeLiveNarrativePhase` is that guard, and it is the
  same one `OnEnterLocationInteraction` has always carried inline.

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
**`EmbeddedResource`s** — they travel inside the assembly, so a shipped build has them without the
folder. `ShaderSource.Load` reads the file from disk when it is there (a shader stays editable
without a rebuild) and from the manifest otherwise.

**That arrangement replaced three hand-maintained copies, and the story is why it is worth the
csproj entry.** The shaders used to fall back to string literals in *both* renderers, so the vertex
shader existed three times: the file, `TerminalRenderer`'s copy, `PopupRenderer`'s. Since `src/` is
not in the payload, a package ran the copies — and `TerminalRenderer`'s had drifted, missing
`uGlyphScale` entirely. **Every shipped build drew its main terminal text at scale 1.0 while
development drew it at 1.2**, and the popup, whose copy was correct, disagreed with the terminal
beside it. Nothing reported it, because a missing uniform is silent by design:
`GL.GetUniformLocation` returns -1 and `GL.Uniform1(-1, …)` is a defined no-op, so the value was set
every frame into nowhere. It surfaced only when the Settings screen made the value changeable and
the SIZE row did nothing in the package. Drift is now not fixed but impossible — there is one copy.

Two things that outlive the bug:

- **`TerminalRenderer.Uniform(name)` complains once when a lookup returns -1**, which turns that
  whole class of silent failure into a log line. Worth extending to any renderer that gains a
  uniform-heavy shader.
- **Reproduce a shipped build's shader path by renaming `src/terminal/Shaders/`.** That is the only
  way to exercise it, because `ShipArguments.Filter` strips `--cli-script` and a packaged build
  therefore cannot be driven by a script at all. Both paths should start the game with no
  `no uniform` line in the log.

A new shader file needs no code change — the csproj item is a wildcard, and `LogicalName` names the
resource for the file (`Shaders/terminal.vert`) rather than for its namespace-mangled path.

The zip lands around 2.2 GB, nearly all of it `model.gguf`, which is already-compressed
quantised weights and does not shrink. That is over itch.io's browser upload limit, so releases
go through `publish.ps1`.

### The manual in a release

`package.ps1` runs `python tools/build_manual.py` **before anything else** and stages the result at
the root of the package, beside the executable. The chapters are the source and the PDF is a build
artefact, so shipping whatever PDF happened to be in the working tree would eventually ship a
manual describing rules the game no longer has — with nothing about the file to say so. A build
failure there stops the package rather than falling back to the stale copy; `-SkipManual` overrides
that for a machine without Chrome, and says what it costs.

**`publish.ps1` deliberately does not upload it.** butler only ever replaces builds in channels it
owns, so it cannot touch the PDF that sits on the page — that was uploaded through the web form and
has no channel. It *could* push the manual as a channel of its own, but itch then serves it as an
archive rather than a one-click PDF, which is a worse page for a document. So the manual is a
hand-upload, and `publish.ps1` closes with a reminder naming the freshly built file and its full
path, because the failure this guards against is shipping a new build against last month's manual.

If that trade ever looks different — automation mattering more than the one-click download — the
push is one `butler push <pdf> user/game:manual` away.

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
| `ProscribedPalimpsest.exe`, staged folder, zip | the dev-only launchers (fight area, image-to-text, music PoC) |
| `%APPDATA%\ProscribedPalimpsest\` (save + settings) | `%APPDATA%\Cathedral\` for a development build |
| the itch page | the repository name and the csproj file name |

**Only the shipped executable is renamed**, by an `AssemblyName` conditioned on the same `Ship`
flag. `run_tests.sh` guards the suite with `Get-Process -Name Cathedral`, and that guard is what
stops a leftover run from racing a new one; renaming the development binary would break it
*silently*, because the guard would simply stop finding anything and read as "all clear".

**The data folder is named per build** (`AppData.FolderName`, conditioned on the same SHIP
constant): `%APPDATA%\ProscribedPalimpsest` when shipped, `%APPDATA%\Cathedral` in development.
They shared one folder until it became clear that testing the packaged game was never testing a
clean install — a save left by a `dotnet run` session lit up Continue in the shipped build, and a
compute device probed by one was inherited by the other.

There is deliberately **no migration** between them. Copying development data into the shipped
folder is the coupling this removes; a shipped build starting empty is the point. The cost is that
the first launch after this change re-probes the compute device, because the new folder has no
probe result in it.

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

### A dead pooled connection, and why only POSTs see it

A phase dies for no reason, and the exception says "An error occurred while sending the request" —
the same sentence as every other transport fault. Underneath it is
`HttpRequestError.ResponseEnded` / `HttpIOException: The response ended prematurely`, on a request
that failed in **single-digit milliseconds** with both health probes answering 200. That combination
means one thing: the request went out on a socket that was already gone. Never a busy server, never
a slow one.

**`.NET` already recovers from this by itself — but only for requests it may safely replay, and every
request we make is a POST.** That asymmetry is the whole shape of the bug. Against a server that
drops the second request on each connection, a GET never fails: `SendWithVersionDetectionAndRetryAsync`
re-sends it on a fresh connection and nothing is reported. The identical scenario as a POST fails
every time. **So probing this with a GET proves nothing** — three attempts to reproduce it that way
came back clean because the runtime was hiding the failure.

Two changes, and it is worth knowing which is which:

- **The retry is the fix** (`IsDeadOnArrival`). Verified against a server that reproduces the failure
  on demand: 5 of 6 POSTs hit it, the predicate matched every one, the retry recovered every one.
  It is narrow by design — never a timeout, never an HTTP error status, never `ConnectionRefused`,
  never mid-stream, once only — and every occurrence is logged and counted in the crash report,
  because a session quietly retrying twenty times is the same news as one crashing twenty times.
- **The pool timeout is prevention.** llama-server closes an idle connection after 5 seconds — it
  says so in every response (`Keep-Alive: timeout=5, max=100`) and a socket probe confirms it at
  5.02s. `SocketsHttpHandler` ignores that header and defaults to 60, so the pool keeps a supply of
  dead connections. `LlamaServerManager.PooledConnectionIdleTimeout` is **2 seconds** to remove the
  supply. Note this is *not* sufficient on its own evidence: 12 POSTs at 7-second gaps against a real
  server with the 60-second default produced **no** failures here, because `.NET`'s liveness check
  catches a dead connection on Windows. The reporter's machine was Wine at ~10 FPS, where that check
  evidently loses its race. **If llama.cpp changes its keep-alive, this must stay below it** — the
  crash report prints both numbers side by side, the server's read live from the header.

**Do not diagnose this from log ordering.** `[llama]` lines reach `log.txt` through a redirected pipe
and lag our own writes, so a `Reset instance N` printed before llama's `stream ended` proves nothing
about what actually happened first. The first diagnosis of this bug was built on exactly that and was
wrong. The in-process request table is the evidence; the interleaving is not.

### A streamed response must be read to its end

`GenerateConstrainedStringStreamingAsync` **drains past `[DONE]` instead of breaking on it**, and
that is not tidiness. `[DONE]` is the last thing the caller wants but not the last thing on the
wire: the chunked body still has its trailing blank line and terminating zero-length chunk to come.
Breaking there disposed the response stream mid-write, which leaves the connection unusable — and
because `HttpClient` pools connections, the **next** request fails at the socket with a bare "An
error occurred while sending the request", naming whatever innocent call happened to come after.

The symptom is a phase dying for no reason, at a rate low enough to look like bad luck: 2 failures
in 228 requests in the run that reported it. It is diagnosable from `log.txt` only because
llama-server's `--verbose` output interleaves with ours — a `Reset instance N` printed *before*
llama's `stream ended` is the signature, and every one of the 40 clean teardowns in that run was
followed by a working request while 2 of the 7 inverted ones were not. That correlation is now a
recorded field (`eof=`) in the crash report rather than something to be re-derived by hand.

Two things this depends on:

- **The drain is bounded** (`DrainAfterDoneTimeout`, 5s), and only after `[DONE]` — generation
  itself may legitimately take minutes. A server that never closes the stream would otherwise turn
  the drain into a ten-minute hang, which is a worse bargain than the bug being fixed. If it trips,
  behaviour degrades to what it was before and the trace says `DRAIN-TIMEOUT`.
- **The response is disposed.** It was not, which held its connection until the finalizer ran.

**None of this path is reachable under `--playground`**, which returns before any request is made —
so the whole CLI suite runs without ever exercising it. Verifying a change here means a real run
with a real server, and the check is that every streaming request in the crash report's table reads
`eof=y`.

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

### Logs: one file for a player, a tree for a developer

`log.txt`, beside the game, is **the** log: everything the game prints and everything llama-server
prints, interleaved, in one file a player can attach to a bug report. It is **truncated at every
launch** — the question being asked is always "what happened *this* time", and a log that grows
forever is one nobody reads.

`GameLog.Initialize()` runs as the third line of `Program.cs` and tees `Console.Out`/`Error` through
a file writer, so ordinary `Console.WriteLine` is captured with no call-site changes.
`GameLog.WriteToFileOnly` is the other half: output that belongs in the file but not on screen.
llama-server's stdout/stderr goes through it prefixed `[llama]` — two thousand lines of tensor
bookkeeping per launch, unreadable on a console and exactly what is wanted when a model fails to
load. `AutoFlush` is on, because the run worth reading is the one that crashed.

**`Config.Debug.VerboseFileLogging` (false in a shipped build) governs everything else.** The
`logs/` tree — a directory per LLM session, a subdirectory per slot, another per request holding
prompt, context, reply and timings, plus the narration-graph dumps — is a development instrument
worth thousands of files and ~71 KB per request, and it contains the full text of everything the
model was asked. A shipped build writes none of it: `TryCreateLogDirectory` returns null, which
switches off every writer downstream because they all already test for a null session directory.

| | `log.txt` | `logs/` tree | `log-crash-*.txt` |
|---|---|---|---|
| development | yes | yes | on a failed phase |
| shipped | yes | **no** | on a failed phase |

### When a phase fails: the crash report

A narration phase that cannot finish is **terminal on purpose** — no retry, no way to carry on. That
is not harshness, it is what produces a usable report: `log.txt` is opened `FileMode.Create`, so a
tester who plays on for another hour buries the failure in a file that reached 4 MB in 25 minutes,
and the moment they relaunch to see whether it recurs, the only record of it is gone. Escape still
reaches the pause menu, so the run can be quit; it cannot be resumed.

**`CrashReport.Capture` is what a failure owes the player.** It writes a diagnostic block through
`Console.Error` (so it lands in `log.txt`), then copies the whole log to `log-crash-<stamp>.txt`,
which no later launch will touch, and the error screen names that file and asks for it.

What is in it, and why each line is there — every one answers a question the report that prompted
this could not:

- **the whole exception chain**, with `SocketError` and `HttpRequestError`. Every transport failure
  says "An error occurred while sending the request" and nothing else; the code underneath separates
  a connection reset (a pooled connection reused after the server closed it) from a refusal (the
  server is gone) from a timeout (it is alive and wedged);
- **two health probes**, one over the shared `HttpClient` and one over a throwaway with its own
  pool. A fresh connection succeeding where the pooled one fails says the server is fine and *our
  connection* was not. That distinction was unanswerable before;
- **the server's keep-alive terms beside our pool's idle timeout.** Read live from the response
  header, because the failure they diagnose is the gap between the two numbers and neither means
  anything alone — see "The connection pool must expire before the server's keep-alive does";
- **the last 16 requests**, with `eof=` per streaming request — see the note on the drain below —
  and the idle gap before each, which is what a keep-alive timeout would show up as;
- **whether this is Wine**, read from `ntdll!wine_get_version`. It was inferable only from a
  `Z:\home\…` path in an unrelated startup line, which is a poor way to learn the most important
  fact about a report.

Subsystems add their own sections with `CrashReport.AddProvider(name, () => text)`; a provider that
throws costs its own section and nothing else. Registering a name twice replaces it.

**`crash-report` in the CLI forces one**, and asserts on its own behalf by reading the preserved copy
back and failing named on any missing section. That command is the only coverage this code can have:
a real trigger is an exception a script cannot arrange (the fault it was built for struck twice in
228 requests), and `--playground` answers every LLM call without making it, so no script can provoke
one. `cli/system/crash_report.cli`.

`publish.ps1` deletes `log-crash-*.txt` from the staged folder for the same reason it deletes
`log.txt` — it is a copy of exactly that.

**The console is not the log.** Output that is mechanical repetition goes to `WriteToFileOnly`
rather than being deleted — the glyph-atlas rebuild (~90 times a session, and a third of everything
printed), the camera-centring geometry, the graph adjacency sample, the item catalogue
(`PrintWorldStructure`, 282 lines naming every item type). A short session went from 790 console
lines to 296 while `log.txt` kept all of it. Prefer moving a line to the file over deleting it: the
reason it was printed usually still exists, it just was not worth a third of the screen.

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
`%APPDATA%\Cathedral\settings.json` — volumes, the dither toggle, fullscreen, the four glyph
settings, and the compute settings. They live together because the file is rewritten whole, not
merged: two classes writing it would each silently discard the other's fields.

The screen is **three groups**: audio (volumes), video (fullscreen, dither, and the glyph block),
and language model. Everything in the first two applies at once; nothing in the third does,
and the boundary is what lets the "changes above take effect at the next launch" note under the
model rows mean that group rather than the whole screen.

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

**Never render the Settings screen except through `SyncAndRenderSettingsScreen`.** The renderer
holds its own copy of every value, so a bare `Render()` faithfully repaints whatever it was last
told — which is the hardest kind of no-op to spot, because the call is right there in the diff. F11
pressed with the screen open did exactly that: the row went on reading WINDOW over a fullscreen
window until the player left and came back. The same call was also ungated by mode, and the renderer
is built once and kept, so F11 pressed *anywhere* after that screen had been opened once repainted
the whole settings screen over the live mode. One helper now owns both halves; anything that changes
a setting from outside the screen calls it, guarded by `_currentMode == GameMode.Settings`.

Which source each row syncs from is the rule that makes this worth centralising: **read the thing
that obeys the setting, not the store** — the renderer for dither, `WindowMode` for fullscreen,
`Config.Terminal` for the glyph rows. All three can be moved by something that never writes the
setting (a flag, an F-key, F11), and the screen's job is to show where the switch *is*. Volumes and
the model rows have no second route in, so they come from `UserSettings`.

**A test that touches this screen mutates the developer's real `settings.json`** — there is no
`--settings-path` the way there is a `--save-path`, so F11 and every row click write straight to
`%APPDATA%`. That is why no settings script is wired into `run_tests.sh`: an interrupted run would
leave the next launch fullscreen, and a script asserting the SCREEN row's start state would be flaky
against whatever the last run left. Verify by hand, or add the flag first.

**The two glyph rows are the player's only lever on how readable the text is, and one of them is
half-rastered.** `Config.Terminal.GlyphScale` and `GlyphAlphaGamma` are uniforms set on every frame,
so writing them is the whole of applying them; `GlyphStrokeFactor` is baked into the atlas when a
glyph is drawn, so a write there must be followed by **`GlyphAtlas.Rebuild()`** — which exists
because `BuildAtlas` early-returns on an unchanged glyph set, and the set *is* unchanged when only
the pen width moved. Forget it and the row still does something, just the subtler half of what it
claims. That is also why the three are no longer `const`.

**Size and weight are asked twice — of the UI and of the world — and the two pairs are separate
mechanisms, not one applied to two surfaces.** The screen shows them as a 2x2 block (SIZE and
WEIGHT down the side, `U I` and `W O R L D` across), because the first thing a player needs to
settle is which surface they mean. Underneath, both halves differ:

- **size** — a terminal glyph is scaled inside a fixed cell, so `GlyphScale`'s ceiling is that
  cell. A world glyph has no cell; it is a quad on a sphere bounded only by its neighbours, so
  `WorldGlyphScale` is a uniform on the quad and its ends are where the surface gaps (0.70) or
  swallows its own markers (1.60);
- **weight** — the terminal blends alpha, so its weight is a gamma on that alpha. **Every sphere
  fragment shader thresholds the atlas luminance instead** (`if (texLuminance > cutoff) … else
  discard`) and is fully opaque or gone, so there is no alpha there for a gamma to act on. The
  threshold *is* the weight, and `WorldGlyphWeightSteps` pairs it with a raster pen the sphere
  atlas did not previously have at all. NORMAL reproduces the old hardcoded `0.1` exactly.

**`WorldGlyphScale` is read by ray picking as well as by the shader**, through
`GlyphSphereCore.WorldGlyphPickRadius`. A vertex is picked by proximity within
`QuadSize * VertexShaderSizeMultiplier`, so a scaled drawing against a fixed radius means clicks
stop matching the picture — and picking is the one thing `--cli` cannot check, since a script
injects a vertex index and never runs that maths. One property feeds both.

**Shader sources must stay ASCII.** An em dash in a `//` comment inside the sphere's vertex shader
took the game down at startup with `VS compile: error C0000: syntax error, unexpected $end at token
"<EOF>"`. GLSL tokenizers reject the non-ASCII bytes even inside a comment, and report it at the end
of the source rather than at the offending line — so the message points nowhere near the cause.

**The clouds follow the world pair** and take the cutoff live, but their atlas is built once at
startup and is not re-rastered, so the pen half of a weight change reaches them at the next launch.
A decorative layer was not judged worth a mid-frame rebuild; the sphere's own atlas, which carries
everything a player reads, is rebuilt properly through `RebuildGlyphAtlasForWeight`.

**A glyph can also be drawn larger than its cell without touching any setting**, through
`Config.GlyphSizeFactors`. Two knobs there, and the distinction is the trap: `Factors` is the
*raster* size inside the 35px atlas cell, and it stops buying anything once the ink fills that cell
— past which it silently crops the glyph and bleeds into the neighbouring cell. The dice faces sat
past it for a long time, drawn 3px taller than their cell and missing the bottom rule of the box.
`QuadScales` is the way past that ceiling: it stretches the cell over more of the screen, so it
suits a glyph standing in space of its own (the dice sit two cells apart) and not one set in prose
— which is why the difficulty numerals, which open action lines, are raster-only.

Stroke and gamma are **one player-facing row**, not two: they are two halves of one fix biting at
opposite ends of the cell-size range (stroke carries a large cell, gamma a small one), and a player
cannot tell which half their panel needs. `Config.Terminal.GlyphWeightSteps` owns the pairing and
its ends are the documented failure modes — past ~0.04 of stroke the counters of `@ 8 %` close, much
below 0.7 of gamma the text turns blocky. `GlyphWeightStep` is the live truth the screen reads back,
because there is no reverse lookup from a stroke/gamma pair to a step.

**The grid is not a setting and must not become one.** `GlyphScale` grows the glyph inside its cell
because cell size is the window divided by `MainWidth`/`MainHeight`, and every renderer hardcodes
its rows against that 100x100 — the grid is layout. Fullscreen is the player's lever on cell size.
The scale ceiling is where the quad's overflow shows on box-drawing glyphs (side rails, popup
frames, the solid volume bars), which fill their cell to the edge where letters do not.

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
| `--encounter-on-arrival <creature>` | Forces a travel encounter on the **final step** of the next journey, once. `--start-fight` reaches a fight with no journey under it and a random roll almost always fires mid-path; this reaches the third case, and it is the one that broke. The last step raises `ProtagonistSteppedToVertex` and `ProtagonistArrivedAtLocation` from **one** `UpdateMovement` call and clears the path between them, so the fight begins with the arrival already announced and nothing left to walk. Honoured **ahead of** `--no-encounters`, so a script can suppress the random rolls and still stage this one. |
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
| `--grant-item <id[,id…]>` | Puts named items in the starting pack (`axe`, `pick`, `shovel`, `fishing_rod`, `knife`…). Seven verbs are `ToolUsage.Required` and `RequiredToolRule` refuses them outright without one; the starting kit is random, so their success test is unwritable otherwise. Note the rule accepts only `Action.CombinedItem` — **carrying** the tool is not enough, the script must combine it with the action (`click action 0` → `choose 1` → pick the item → `click action 1`). Grant the verb's own **reference tool**, which auto-passes with no critic call: under `--playground` the item critic picks its verdict at random, so a substitute makes the test a coin flip. |

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

**Never identify content by a string.** A content kind is a **type**: points of interest are
`src/game/scene/PoiKinds.cs`, areas are `AreaKinds.cs`, and both derive their `Lemma` from the class
name so the two cannot drift. Gate on the type — `ctx.Target is CorpsePointOfInterest`,
`ctx.Pov.Where is AlehouseArea` — never on a display name, a description or a reference lemma.

A string names something that need not exist and nothing notices: half the modus mentis lesson
conditions in the game once matched words like `withy`, `altar` and `padlock` that no factory built,
and the modi mentis behind them shipped unreachable. Substring matching went wrong in the other
direction too — `cross` matched "Crossing", `barrow` matched "narrow". As a type it is a build error,
and `--verb-audit` reports any kind no factory constructs. The same applies to verb gates:
`CutWoodVerb` used to accept `ReferenceLemma is "tree" or "log"`, which would have stopped offering
the verb the day a factory renamed a lemma.

Two exceptions, both deliberate: the "Broken X" replacement takes its lemma from whatever was broken,
and modus mentis ids stay strings because every one of them is checked (fatally at startup by R5/R10,
or by `--verb-audit`, `--npc-audit`, `--dialogue-audit` and `JobRegistry`).

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
  click engage              the travel encounter prompt's ENGAGE button — its only button, so
                            without this a staged encounter is a dead end and the fight behind
                            it is unreachable
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
  fight-deplete [enemies|companions|<fighter>]
                            bleed a fighter's humors dry — the OTHER way to die in a fight, which
                            ends it without touching hit points. Takes `enemies` rather than a
                            name because a narration fight pulls in every nearby NPC with
                            authority and their names are generated content
  fight-wound [enemies|companions|<fighter>]
                            wound one fighter to death. The only way a script can kill a COMPANION
                            — `fight-end` settles the whole fight and cannot single anybody out
  wound [protagonist|companions|<name>] [n]
                            wound a party member OUTSIDE a fight, as a failed act's penalty does.
                            No count means enough to kill. A wound is SAMPLED one slot in five, so
                            a script cannot ask a failed act to injure anybody on demand
  click companion-death     the companion-death notice's CONTINUE. Modal over every mode, so a
                            script that does not press it cannot get past a companion dying;
                            `state` reports `companion-death=shown`
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
  crash-report [text]       force a crash report and preserve log.txt under a name the next launch
                            cannot overwrite. Asserts on its own behalf — it reads the preserved
                            copy back and fails naming any missing section, because `expect` scans
                            the terminal and never sees a file
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

### Opening a fight: the first blow

**`attack` used to be a doorway and nothing else.** It rolled, it printed nothing a player could
read, and the fight began with both sides untouched — so the one thing the verb was *for*, hitting
somebody, happened only after the fight screen had replaced the narration. `FirstBlowOutcome` is the
swing itself, and it is assembled in three steps that each answer a different question.

**What can this body strike with?** `FirstBlow.MediumsFor` gathers fighting mediums, and **an
implement replaces the body rather than adding to it**: a combined weapon is the whole list (a man
who draws a sword is not also kicking), and empty hands offer every organ medium the anatomy owns
plus the body-part mediums (`upper_limbs` — seize, chokehold), each needing a score above zero and no
High-handicap wound. Note it reads the **combined** item, never the equipped one — the weapon on your
belt is not the weapon in the blow unless the player put it there, and choosing to is what the tool
combination is for.

**Which blows does that allow?** Every medium's skill list, minus everything that is not
`FightingSkillEffect.Attack` (viscera is all buffs, legs all movement and guard) and everything this
body could never keep the lesson of. **An unlearned skill is a candidate** — requiring the modus
mentis would mean somebody who has never fought cannot throw a punch, which is backwards: the first
blow is where the punch is learned. What is required is that the lesson could be *held*
(`ModusMentisAnatomy.IsLearnableBy`), so a body is never taught something its anatomy caps at level 1
for ever.

**Then one is drawn uniformly, and it decides three things at once**: the blow that lands, the wound
it leaves (`FightResolver.PickWound` against a pre-rolled hit location, exactly as `SkillAction` does
it — but **armour is not consulted**, since armour buys defence *dice* and there is no roll here), and
**what the verb teaches**. That last one is why `AttackVerb.GrantedModusMentisId` returns null: a
punch teaches Brawling and a chop teaches the axe, so the lesson cannot be declared per verb the way
every other verb declares its. `--verb-audit` names attack in its `TeachesPerBlow` exemption, since a
verb teaching nothing is otherwise exactly the dead content it exists to catch.

Four rules the rest of it depends on:

- **The verb dice are the only dice.** A landed `attack` is an automatic hit; there is no second roll
  against the target's defence. The verb was hard enough to reach.
- **A first blow can never kill.** A target down to its last hit point turns it aside — the lesson is
  still learned and the fight still begins, but no wound is dealt. Without that a corpse would be left
  standing: `NpcEntity.IsAlive` reads the hit points, so a body killed here would be dead with no
  `RemoveNpcFromPlay`, no corpse, and a fight opening against somebody already gone. Killing outright
  is `slay`'s job, at a difficulty of its own.
- **A miss still starts the fight, and costs the initiative.** `FailureReports` returns a
  `FightTriggerOutcome` with `EnemyInitiative`. Attack was the only crime whose victim could not tell
  it had been attempted.
- **The fight waits for CONTINUE.** `_deferredFightOutcome` holds it until the press that closes the
  segment, because `_pendingFightOutcome` is read by the game controller on the very next tick — and
  the chips (what the blow was, what it broke, what it taught) are the whole account of the swing,
  painted over by the fight screen the frame it opens. `state` reports `fight-held=yes`, since on
  screen a held fight is indistinguishable from an ordinary resolved action and a script that presses
  nothing would sit out its timeout.

**Effects outlive the narration.** Twelve attack skills carry a `FightStatusEffect` — Trip knocks
down, Flesh Tear starts bleeding — and a `FightStatusEffect` is meaningless without the `Fighter` it
hangs on, which does not exist until the arena is built. They ride on `NpcEntity.CarriedFightEffects`
and are drained by `FightModeAdapter.ApplyCarriedEffects` *after* the `FightState` is constructed
(`OnApply` logs, and a pushback needs the arena) and cleared as they are applied. Carried on the
individual, so a blow struck at one person cannot land on whoever the fight brings in with them.

**Shallow wildlife takes the same first two steps and neither of the last two**: no anatomy to wound
and no fight to have, so the blow kills and narration carries on. The kill itself is the ordinary
`NpcSlaynOutcome` reported beside it, which is what spawns the carcass.

`cli/verb/attack/` holds all three arms — the landed blow, the miss, and the chicken — and
`cli/outcome/first_blow/` proves the wound is really on the man.

### Winning a fight: what the victory has to hand back

`FightState.CheckFightEnd` is faction arithmetic and nothing more — the last living `Enemy` falling
is `PartyWon`, however many `Party` fighters are standing. **Companions are `FighterFaction.Party`
and `IsPlayerControlled`**, added by `BuildFighters` straight from `Protagonist.CompanionParty`, so a
companion neither prolongs a fight nor is waited on. `cli/system/fight_victory_companion.cli` plays a
kill out blow by blow rather than calling `fight-end victory`, because the forced end sets the result
directly and never evaluates the condition at all.

**What the victory returns *to* is the part that is easy to get wrong, and there are three cases.**
`OnFightCompleted`'s travel branch answers all three, and answering only the middle one is what
produced a reported softlock — the travel progress box left standing over a black screen:

| | after victory |
|---|---|
| encounter **mid-journey** | resume travel — the path is still there to walk |
| encounter on the journey's **final step** | deliver the held arrival (`_pendingArrivalVertex`) and finish the journey |
| **no journey at all** (`--start-fight`) | back to the world view |

The middle case is the trap. `MicroworldInterface.UpdateMovement` raises `ProtagonistSteppedToVertex`
— which is where the encounter roll lives — and `ProtagonistArrivedAtLocation` from the **same call**,
with `_currentPath` nulled between the two. So the arrival is announced while the mode has already
become `EncounterPrompt`, `OnProtagonistArrived` refuses it (it only answers from `Traveling`), and
resuming travel afterwards resumes onto a path that no longer exists: `Traveling` for ever, with only
the pause menu to leave by. The arrival is therefore **held rather than dropped** and delivered once
the encounter settles; the runaway and death branches clear it, because both end the journey.

**There are two ways to die in a fight, and only one of them used to leave a mark.**
`Fighter.IsAlive` is `CurrentHp > 0 && !IsHumorDepleted`, while `NpcEntity.IsAlive` reads the hit
points alone — so a fighter bled out by `BleedingEffect` (which raises `IsHumorDepleted` when the
humor queues go fully critical) was dead for exactly as long as the fight lasted, and alive again at
full health the moment the `Fighter` wrapper was dropped. `CheckFightEnd` counted them dead and
awarded the victory; `OnFightCompleted` then asked `enemy.IsAlive`, was told yes, and skipped them.
No corpse, no `RemoveNpcFromPlay`, no `DepartedNpcs` entry — a fight won against somebody still
standing in the room, whom the next visit rebuilt from the seed regardless.

`FightModeAdapter.SettleEnemyDeaths` carries the fight's verdict back onto the NPCs, once, as the
fight ends, and for **every** result rather than only a victory: an enemy who bled out while the
party fled is no less dead. Marking is one-way, so nothing there can resurrect anybody.

`fight-deplete` is the CLI counterpart of `fight-end` for this, and exists for the same reason: the
real route is a bleed out-lasting a dozen turns of drain against full queues, and which special
effect a blow rolls is not something a script can ask for.
`cli/system/fight_victory_humor_depletion.cli` asserts both halves — a body left, and the man gone —
because spawning the corpse without the removal behind it would satisfy the first alone.

### Death, and where it is reckoned

`TriggerDeath` is the funnel for every cause, and it now **tears down a live narrative session**
before switching mode. That is not tidiness: `Update` enters its narration branch on
`_isInNarrativeMode` rather than on the mode, so a session left standing repaints itself over the
death screen every frame. `ExitNarrativeMode` and `TriggerDeath` share `TearDownNarrativeSession`,
which is everything except the mode change.

**A fight lost is a run lost, wherever it was fought.** The narration branch of `OnFightCompleted`
used to answer a `Death` result with `ExitNarrativeMode()` — so the travel encounter was the only
lethal fight in the game, and a fight picked inside a location could be lost at no cost at all, the
protagonist walking out at zero hit points and playing on.

**Wounds and spent humors are reckoned continuously; age is reckoned on the map.**
`SettleProtagonistDeath` runs on the same tick as the companion sweep and reads `CauseOfDeath`,
taking every arm except `OldAge`. Both of the causes it takes are dealt at arbitrary moments of a
visit and neither had a check that covered those moments:

- a **wound** arrives from a blow or from the penalty a failed act *samples* — no check existed at
  all, so a mortal one left the protagonist playing on at zero health;
- the **humors** are spent by bleeding and by every buff a fight is paid for with, and all three
  starvation checks were on the travel path — so a body whose queues went critical indoors stayed
  upright until the next journey drew heat it did not have. Worse, a protagonist who bled out in a
  fight a companion then *won* walked away alive and at full health, because depletion is recorded on
  the discarded `Fighter` wrapper and nothing outside the fight reads it.

**Age stays where it is** (`CheckOldAgeDeaths`, on the world-view entry, after the healing sweep).
Sweeping it continuously would end the run mid-visit the moment the clock crossed the term — and
since a beast outlives a person, it would also make a companion's death by age unreachable, the
protagonist always falling first. There is little for a standing check to catch anyway: only a work
stint moves the calendar inside a visit. Companions are swept on all three, because their death ends
nothing.

The travel-path starvation checks are left in place and still fire first while a route is walked:
the drain notices the queues empty in the same breath as it empties them.

**One consequence to know.** `--black-bile` sours the queues at the moment the protagonist is
accepted, so that state is now lethal on sight — the run ends on reaching the world map, with no
journey attempted. `death_starvation.cli` used to prove the death happened *between two cells* and
cannot any more; it asserts the new behaviour instead. The leg-by-leg drain therefore has no
dedicated test now, which would need a way to stage a *nearly* critical body.

| | |
|---|---|
| `wound [protagonist\|companions\|<name>] [n]` | the narration counterpart of `fight-wound`. A wound is sampled one slot in five, so a script cannot ask a failed act to injure anybody, let alone three times on one body |
| `starve [protagonist\|companions\|<name>]` | its humoral twin, and why it is a command and not a flag: `--black-bile` can only sour the queues before the world has been touched, so it stages a body that was *always* starving, never one that starves partway through — which is also what lets `death_narration_humors.cli` assert that a save existed and was erased |

`death_narration_wounds.cli`, `death_narration_humors.cli` and `death_narration_fight.cli` are the
three new routes to the death screen, kept alongside `death_wounds.cli` and `death_starvation.cli`
because they reach `TriggerDeath` down different paths — and the difference between the paths *was*
the bug.

### When a companion dies

**Companions could not really die.** `CompanionParty.Remove` was reached by the old-age check and by
the heart-ceiling overflow and by nothing else, so a companion cut down in a fight walked out of it
at zero hit points and stayed in the party for ever — counted against the heart ceiling, handed the
narration by Speak About, turning up in the next fight with nothing left to lose. One bled dry was
not even dead, for the reason above.

**One question, asked in one place.** `PartyMember.CauseOfDeath` returns `Wounds`, `Starvation` or
`OldAge` — or null — and all three are **derived**: hit points are `MaxHp` minus the wound count, a
fully-critical humor set is a state every consumption path already produces, and old age is the clock
against `GetLifetimeDays()`. So a member is dead the moment they are, whenever the question happens
to be asked, with nothing to keep in sync. `LocationTravelGameController.SettleCompanionDeaths` asks
it, and `CheckOldAgeDeaths` now delegates its companion half to the same funnel.

Three things about it that are load-bearing:

- **It is swept every tick, not called at each place a companion can be killed** — because those are
  not a closed set. A fight is the obvious one, but a failed action's wound penalty lands on whoever
  is *acting*, which after a Speak-About hand-off is the companion, and the humors drain on their
  own. Derived state makes the per-tick cost a few property reads, and makes it impossible for
  whatever kills somebody next to forget to call it.
- **Never while a fight is running.** The fight holds `Fighter` wrappers over these members; pulling
  one out of the party mid-swing leaves a fighter on the board belonging to nobody. `Update` gates
  the sweep on the mode, and it fires within a tick of the fight handing back. A dead companion
  therefore does *not* end the fight — `CheckFightEnd` is faction arithmetic and the protagonist is
  still standing.
- **The body is only left when there is a scene to leave it in.** A travel encounter and the old-age
  check both happen on the world map, where there is no area and no observation graph; there the
  notice is the whole of the news, which is what it always was.

`CorpseRegistry.CreateForCompanion` is a second entry point because **a companion is not an
`NpcEntity`** — recruiting moves the `EnemyCombatant` into the party and drops the wrapper, so there
is no `Archetype` to read the species off and no `Combatant` to reach the pack through. Both come
off the `PartyMember` instead, and `CorpsePointOfInterest.NpcEntity` is nullable for exactly this
body. Everything downstream is unchanged, because `cut` and `grab` gate on the PoI's **type**.

**`CompanionDeathBox` is now modal over every mode**, gated at the top of `Update` rather than under
`_currentMode == GameMode.WorldView`. Old age was raised at the world map and nowhere else; a
companion now falls in a narration fight, and narration redraws itself every frame — a box gated
below the mode dispatch would be painted over before it was read. Dismissing it calls
`RedrawCurrentMode`, **not** `OnEnterWorldView`: the box can come up mid-session, and repainting the
map over a scene the player has not left is the bug that swap prevents. `RedrawCurrentMode` shares
`SetMode`'s mode-entry switch (`RunModeEnter`) because `SetMode` early-returns on an unchanged mode,
which is right for a transition and useless for an overlay.

| | |
|---|---|
| `fight-wound [enemies\|companions\|<fighter>]` | wound one fighter to death. **The only way a script can kill a companion** — `fight-end` settles the whole fight and cannot single anybody out, and steering the enemy AI onto one party member for long enough is not something a script can ask for |
| `fight-deplete` | the same, by humors |
| `click companion-death` | the notice's CONTINUE. Modal over every mode, so a script that cannot press it cannot get past a companion dying; `state` reports `companion-death=shown` for the same reason |

`cli/system/companion_death_wounds.cli`, `_humors.cli` and `_age.cli` cover the three causes — one
each, because only the funnel is shared, not how each death is arrived at. The humors script asserts
the notice names the *humors*, since a companion at full hit points reading as unhurt is exactly the
failure. The age script pushes the clock from **inside** a location: the body needs an area to lie
in, and a cat outlives a person (Nighthunter's span runs past 48,000 days against a heart's 43,200
ceiling), so on the world map the protagonist's own old-age check would always fire first.

`--encounter-on-arrival` exists to stage precisely that step — a random roll lands on the last cell
of a path about once in a hundred journeys, which is a bug that reaches players and not tests.
`cli/system/fight_victory_travel_arrival.cli` and `fight_victory_travel_dropin.cli` are the two
travel cases; `fight_victory_narration.cli` is the fourth route out, which returns through
`NarrativeController.OnFightCompleted` instead and is asserted on the **corpse**, since a scene
re-entered with the enemy still in it would pass a mode check and be wrong.

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

**A wound belongs to an anatomy, and every path that puts one on a body must ask first.**
`WoundRegistry.CanBeSufferedBy` is that question — the wound counterpart of
`ModusMentisAnatomy.IsLearnableBy`, matched by **type**, never by `WoundId`, which collides across
anatomies by design. `WoundRegistry.All` is the *human* catalogue; each anatomy factory owns its own
through `GetWoundClassMap`, and a beast has no knee, no fingers and not even a `ScarWound`.

The reason it is a rule and not a nicety is that the failure is silent twice over. A human wound on
a beast **penalises nothing** — every `Affects*` query misses an organ part the anatomy lacks, so it
costs the one hit point any wound costs and reads, if anyone looks, as a lame leg on an animal with
no knees. It then goes into the save verbatim, and `PartyState.Rebuild` resolves wounds against the
body's **own** catalogue and fails closed: one such wound, taken at generation and never noticed,
makes the whole save unloadable a dozen sessions later.

That is not hypothetical. Global personality traits are dealt to every anatomy and eight of the
sixty carry a human wound, so roughly **one beast in four** was given one; taming it and saving cost
the run. `PersonalityTrait.ApplyGameplay` now refuses the wound and logs it, exactly as
`NpcSkillGrant` refuses an unlearnable skill — expected for a global trait, a content fault for an
archetype's own, which `--npc-audit` names. There is deliberately no translation to a beast
equivalent: nothing sensibly maps a missing finger onto a wolf.

### A wound takes a modus mentis away, not just its growth

**A wounded anatomy source contributes a negative amount to a modus mentis's max level.** That is the
whole rule, and it is the one place in the game a derived stat goes below its `WorstValue`:

| the source | contributes |
|---|---|
| a **High**-handicap wound (`GetEffectiveScore` → `int.MinValue`) | **−2** |
| **Medium** wounds that have driven the effective score below 0 | **−1** |
| score 0 — wounded to nothing, or never invested in | +0, then the ordinary linear curve |

Since `GetMaxLevelForModusMentis` is `1 + sum(contributions)` with no clamp, enough damage drags the
ceiling under the base of 1 that every modus mentis starts from.
`PartyMember.GetEffectiveModusMentisLevel` is `min(stored level, that ceiling)`, and
**`IsModusMentisBroken` is that result at or below 0** — nothing left to roll with, so the modus
mentis cannot be used at all.

**Derived, never stored**, for the same reason age and wound healing are: the stored `ModusMentis.Level`
is what the character *learned* and no wound writes to it, so the reach comes back on its own when the
wound closes, with nothing to keep in sync and nothing to migrate in the save.

Before this, a wound was a **growth** penalty and never a **performance** one — the cap gated
`AwardModusMentisXp` and the memory panel's `L4/7`, while `GetTotalModusMentisLevel` summed the stored
levels straight into the dice. A ruined arm cost nothing at the table.

**Offered and refused, not withheld — except for speech.** A broken modus mentis stays in the
observation, thinking and action pools and the refusal is narrated in its own voice, naming the
organs and the wounds behind them. Filtering it out silently would make a player's skills disappear
with no account of where they went. It costs a noetic point, like every other refusal.

| where | what happens |
|---|---|
| **action** | `BrokenModusMentisRule`, **first** in `ActionRulesChecker` — ahead of every circumstantial rule, because a player told a witness is watching will walk to another room and one told their arm is ruined will not |
| **thinking / focus observation / Speak About** | `ApplyModusMentisSelection` is the one place all three player-chosen faculties pass through, so the guard is written once; `RefuseBrokenModusMentisAsync` narrates it |
| **the phase-opening observation** | the persona passes over broken ones *while any usable one remains* — nobody chose it, and there is no popup to be told anything through, so opening on a refusal would spend a phase on a choice the player never made. With **every** observation modus mentis broken the phase opens on the refusal and carries no keyword |
| **dialogue replies** | **dropped** (`GetUsableSpeakingModiMentis`). A reply is written straight into the option list with no narration frame to carry a refusal — it would have to be worded as something the character says, which is the thing they cannot do. Emptying the list falls through to `ZeroRepliesDialogueRule`, which now answers *two* questions: no voice (fluency 0) and no means (every speaking modus mentis broken), with a different sentence for each |
| **fighting** | `FightingSkill.IsBrokenFor` is the modus-mentis sum **below zero**, tested inside `IsUnlocked` so the fight AI is gated by construction. Broken skills leave `GetUnlockedSkills` and appear greyed through `GetUnusableKnownSkills` |
| **emotions** | untouched. A disposition needs no organ to be moved by something, so a broken one still fires |

**Below zero and not at it, for fighting only, and the asymmetry is deliberate.** `GetTotalMmLevel`
returns 0 for a skill the fighter never learned, and that state must stay usable — `FirstBlow` draws
from unlearned skills on purpose, since the first punch is where the punch is learned. Inside a fight
`IsUnlocked` has already required the modus mentis to be known, so a 0 there means "known and worn
down to nothing" and *does* slip through: such a skill rolls `BaseDice` plus its medium and nothing
else. Narration has no such ambiguity and breaks at 0.

Three things that had to move with it:

- **`DerivedStat.GetValue` is now virtual, and only the two max-level contribution stats override
  it.** Every other stat degrades *towards* `WorstValue` and stops, which is right for a quantity a
  body either has or lacks. Do not widen the negative reading — that method is the read path for
  carry weight, beauty, health and noetic points.
- **Absent is not wounded.** `DerivedStat.DiscoverAll()` gives every anatomy every stat, so a human
  really does carry `FangsMaxLevelStat`. It reads +0 because `GetSourceScore` answers 0 (not
  `int.MinValue`) for a missing organ and the linear curve then divides into a `MaxScore` of 0 —
  a *different path* from the wounded one. Route absence through the negative branch and every beast
  organ named by a human's modus mentis starts costing −2.
- **`NpcAudit` needs the same floor of 1 that `NpcContentGenerator.SettleSkillLevels` applies.** An
  NPC generated with a trait scar keeps the skill at level 1 and simply has it broken; without the
  floor, 37 scarred archetypes read as "over its cap of −3".

| | |
|---|---|
| `cripple <mm-id> [who]` | High-wound every source a named modus mentis draws on until it is broken. **The only deterministic way in** — `wound` draws from the catalogue at random, so landing a High wound on one named organ is a lottery. Note it reaches further than it is aimed: a visage wound disables the eyes *and* the nose, so one call breaks every modus mentis built on either |
| `inspect skills` | now reports `effective=`, `max=` and `broken=` beside `level=`. The stored level alone cannot say whether the thing can still be used, and deriving `broken` in a script from two numbers would be a paraphrase of the rule rather than a reading of it |
| `cli/system/broken_mm.cli` | the level falls, the refusal is narrated naming the organs, a noetic point is spent, and `save roundtrip` still passes — the clamp is derived, so nothing about it reaches the file |

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
- **An accepted tool costs nothing; a refused one costs a point.** Combining is part of the action
  already on screen, and only one item may ever be combined with it. Charging for the *accepted*
  case made the tool-gated verbs (`dig`, `mine`, `fish`, `cut_wood`, `break`) *impossible* once the
  pool shrank: the thinking phase that produces the action spends the point "Use Tool" then wants,
  so the option was offered and permanently greyed out. Charging for the refusal is what pays for
  never greying the row by verb — see "Tools: the three categories" below.
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

### Tools: the three categories, and the four gates

`Verb.ToolUse` is `Excluded` / `Optional` / `Required`, and the test that assigns it is **mechanical,
not aesthetic**: a combined item joins the die chain as its leaf, so the question is "can holding a
thing change how well this goes?". Speech, the senses, thought, waiting and walking on the flat all
answer no structurally — 31 verbs are `Excluded`, 14 `Optional`, 8 `Required`.

**`Required` implies `Handcraft`.** `Verb.EffectiveCapabilities` ORs it in, and `IsPossible` and the
audit's anatomy table both read *that* rather than `RequiredCapabilities`. Without it a beast is
offered `slay`, refused by `RequiredToolRule`, and charged a noetic point for a refusal it could
never have avoided — a withholding dressed as a refusal, which is the whole `ChoiceRulesChecker` /
`ActionRulesChecker` distinction. Read the declared half anywhere and the beast comes out able to
slay; that was the bug the audit fix caught.

**The "Use Tool" row is greyed only when nothing combinable is carried** — never by category. An
excluded verb still accepts the attempt and still fails it, at the cost of a point. That is the
bargain: the player is trusted to choose, and pays for choosing badly. Do not add a category test at
the `hasItems` gate in `HandleClick`.

`ToolCombinationRules.Resolve` is the whole pre-LLM ladder, and **the order is load-bearing**:

| gate | when | costs |
|---|---|---|
| `NoProficiency` | hands band is None | a point, no request made |
| `MadeForIt` | `Verb.UsesFightingMedium` and the item is an `IWeaponItem` | nothing, no request made |
| `NotAWeapon` | `Verb.UsesFightingMedium` and it is not | a point, no request made |
| `MadeForIt` | item is the verb's reference tool, **or** names it in `Item.MadeForVerbIds` | nothing, no request made |
| `NotItsPurpose` | item declares `MadeForVerbIds` and this verb is not among them | a point, no request made |
| `ExcludedVerb` | `ToolUse == Excluded` and the item is not made for it | a point, no request made |
| `AskTheCritic` | everything else | a point **iff** refused |

Proficiency first because a body that can use nothing can also not use the thing it was handed — and
**that one line is the whole of the rule that hands with no craft in them may use no implement at
all, for any verb**: `ToolUsageProficiencyStat` reads `DerivedStat.GetValue`, which returns the worst
value both for a score of 0 and for an organ disabled by a High-handicap wound, and the worst value
is `None`.

A verb struck with a **fighting medium** next, because for `attack` the question is not what the
implement was made for but whether it is a weapon — and the set of things one fights with is closed,
so both directions are settled with no request made. A sword is what attacking is done with whatever
its `MadeForVerbIds` say, and no argument about a lantern's heft makes it a weapon.

The item's own purpose next, and it settles the matter **both ways** before the verb's category is
consulted at all — `MadeForIt` and `NotItsPurpose` are one declaration read in its two directions.

**`ToolUsageProficiencyStat` replaced `ToolUsageCapStat`, and the replacement is the opposite end of
the same organ.** The cap clamped the dice a tool lent; the band decides whether the combination
happens at all. Keeping both taxed hands twice for one act, so **the item now lends its whole
`UsageLevel`**. Bands: 0 → None, 1–2 → Low, 3–4 → Medium, 5–6 → High. What each band clears is
`ToolCombinationRules.Threshold` — `is_the_tool`/`clearly_helps` at Low, `serves_well`/
`plausibly_helps` at Medium, `serves_poorly`/`detoured_use` at High, and anything absent from the
table never clears. **A re-authored critic tree that introduces a choice id fails closed**, which is
why the table is a lookup rather than a switch with a default.

**Five refusals, five neutral sentences** (`ToolFailureKind`). Told only "it did not work", the
acting modus mentis rewrites it as whichever near-miss flatters the character — so the wording
distinguishes the wrong implement, the act that admits none, the implement made for other work, the
hand with no craft in it, and the sound idea beyond an unpractised hand. Only `WrongTool` carries
the critic's own reason, because it is the only one an LLM was asked about.

**`Item.MadeForVerbIds` forbids as much as it permits, and that is the point.** A declaration means
"accepted for these acts without argument, refused for every other, also without argument" — a glass
ground to magnify cannot break ore out of a seam, and asking a critic whether it might is a request
spent to be told what the declaration already said. So it is for **single-purpose** implements only:
`Rope` deliberately declares nothing, because naming the four climbs would forbid the dozen other
things a rope does. When in doubt, leave it empty and let the critic judge.

A verb's `ReferenceToolIds` is the *other* side of the same idea and carries **no** exclusivity — a
knife is what `cut` is done with and is still an ordinary candidate for everything else. Only the
item-side list bars.

It is deliberately **not rendered anywhere**: an implement announcing what it was good for would
turn the phase into a list to be read off. It is also the only road into an excluded verb, which is
why `GetCombinableItems` admits **any** item declaring one regardless of category — reading lenses
are a garment, and are the case the mechanism exists for. `--item-audit` resolves every id, prints
how many verbs each declaration bars, and flags a pairing already implied by `ReferenceToolIds`
(redundant on the accepting side, and *not* harmless on the other).

**A combined implement is never consumed, and there is no longer a question about it.** An LLM
critic used to be asked "was this item used up?" after every successful combination. Since only
tools, weapons and special-use items are combinable and none of those is spent by being used, it
spent a request to answer "no" nearly every time — and the times it said yes were simply wrong, as
when it destroyed the knife a carcass had just been opened with. Under `--playground`, where every
critic choice is drawn at random, it also cost a script combining twice its tool about half the
time. If a consumable ever becomes combinable this comes back as a **property of the item**, not as
a question.

That removal left `ItemConstraint` with no producer, so it changed meaning rather than being
deleted: it is now recorded for **every** combined implement and **requires without spending** it.
That is what it should always have meant — replaying "work the seam for ore" without a pick is not
a routine that can be walked, whether or not the first pick survived. `Consume` is deliberately a
no-op rather than removed (the base class calls it for every constraint), and it leaves the virtual
ledger alone so two steps of one routine can both call for the same knife.

### Emotions: what an outcome does to the person who caused it

An action resolves, its consequences are applied — and then **one disposition the acting body holds
answers them**, renders 1d6 humors into the spleen, and says so in its own voice. That is the whole
system. The three nouns it adds are `ModusMentisFunction.Emotion`, `EmotionTrigger` and
`EmotionOutcome`; everything else is existing machinery reused.

**The match is on the outcome's TYPE, never on its payload**, and that discipline is the design
rather than a shortcut. Asking "was the item *fine*?" or "was the target *weaker than me*?" puts an
open-ended question in front of every consequence in the game, and the answer has to come from an
LLM — one more request per action, for a decision the player never sees. A type is a fact the
compiler already knows, so the whole resolver is type matching and two draws, with no request made.
The cost is real and worth naming: an `ItemAcquisitionOutcome` cannot tell gluttony the item was
bread, so **gluttony is not an emotion modus mentis**. That is the correct answer, not a limitation
to route around.

`EmotionTrigger.WhenSeverity` is the one concession and it reads no payload either —
`Outcome.Severity` is on the base class. It exists for `AffinityIncrementOutcome`, which is one type
carrying two opposite pieces of news (its severity is set from the delta's sign in its own
constructor), so a type-only match would make pride feel affronted by being liked.

Four rules the rest of it assumes:

- **Every modus mentis the body holds is asked, not the one that acted.** Avarice rejoices at coin
  whether or not avarice was the modus mentis that earned it. Keyed to the acting one, the emotion
  would fire in the rare case and stay silent in the common one, which is backwards — so
  `EmotionResolver.Resolve` takes a `PartyMember`, not the action's chain.
- **Exactly one emotion per action**, sampled uniformly from the matches. A wolf slain in a private
  room after a forced door matches five dispositions at once, and five blocks plus five chips for one
  press would bury the action that caused them. Uniformly rather than by level, because a level is
  how well a thing is *done* and nobody feels their strongest feeling most often.
- **The narration is a second request in a different slot**, and that is the point of the block: the
  outcome was written by the ACTION modus mentis and this is written by the EMOTION one. A longer
  first request would be one persona speaking twice.
- **The feeling is named, not implied.** `BodyHumor.FeelsLike` carries one plain first-person
  sentence per mind state, and `NeutralNarration.Emotion` joins it to the outcomes' own verbatims.
  Without it the persona is handed the word "Laetitia" and invents whichever emotion flatters its
  disposition — so the humor that reached the queue and the feeling the player read would disagree.
  `NarrationKind.Emotion`'s instruction insists the stated feeling *survives* the rewrite for the
  same reason.

**`EmotionOutcome` is an `Outcome`, and the word in this codebase has never meant "something a verb
did"** — `StateCaptureOutcome` and `GetUpTransitionOutcome` are pure bookkeeping and are Outcomes
too. What the base class means is "a thing that applies its own change and can show a chip", which is
exactly this. Being one buys three things that would otherwise each need building: the chip renderer
already draws `block.OutcomeReports` and already colours by `Severity` (taken here from the humor's
`VitalHeat` sign, so a humor added later is coloured right without anyone saying so), `ApplyTo` is
already the one door every state change goes through, and `expect-outcome emotion` worked the day it
was written.

**Own block, own CONTINUE, for free.** The coda is one more `LlmPreviewSession` part after the
outcome's, and the preview box already gates one part per press — nothing schedules it. It is
generated ahead while the player reads the outcome, and it is **silent by default**: most actions
move nothing a disposition answers, `Resolve` returns null, no part is begun and no request is made.
Both wirings swallow their own exception, and that is the one narration path in the controller that
does not call `ReportPhaseFailure`: a garnish on an already-resolved action must not become a third
way for a visit to end.

**Two paths, because five outcome types live in only one of them.** `NarrativeController` resolves
from the reports gathered before the dice; `DialogueTreeController` resolves from the tree's outcome
set plus its lessons. Alms, an introduction granted, an enmity cleared, a trade or job opened, and a
conversation that came to nothing are reachable **only** through a tree, so wiring narration alone
would leave pride unable to feel a refused alm. Note what that cost: the dialogue controller now
builds its outcome set, its lessons and its no-consequence fallback **before** the commit closure
rather than inside it — a tree hands out a *fresh* outcome set on every access, so reading it twice
would resolve the emotion against instances the commit never applies.

**R14 and R15** are in `ModusMentisRuleValidator`, fatal like the rest. R14 forbids Action + Emotion,
for the same reason R1 separates Thinking from Action: wanting, doing and being moved are three
offices a mind holds toward one event, and one modus mentis is asked to be exactly one of them.
R15 makes the function and the trigger list imply each other (as R9 does for Fighting and a skill
reference) and rejects a trigger on a humor with no `FeelsLike` — which would narrate an empty
clause.

**R14 cost nothing to satisfy, and that was not the expected answer.** All 22 Action modi mentis that
looked like emotion candidates — rage, blood_lust, sneak_art, clenched_grit, endurance and the rest —
turned out to name a *technique* ("the going on after the reasonable moment to stop") rather than a
feeling, and all 22 were additionally Procedural, so stripping Action would have broken R2 and R4 as
well. **Twinning is the answer here, not conversion**: `exultation` beside ferocity, `obduracy`
beside resolve, `sangfroid` beside cold_blood, `temerity` beside recklessness, `ruination` beside
brute_force, `impatience` against patience. The action pool is unchanged at 127.

**`--mm-audit` prints the coverage table**, and the failure it exists to catch is an absence. An
outcome type no trigger names is fine when nothing feels about it and a fault when a player sees it:
`meet_stranger` — the introduction, which gates every other conversation and is therefore the most
played tree in the game — resolves to a single `AffinityTransitionOutcome`, and nothing named that
type. The most common conversation in the game could stir nobody, the wiring was correct, every test
passed, and the only symptom was silence. That one was found by hand and would not have been found
twice.

**`AffinityTransitionOutcome` is also the clearest illustration of the type-only rule biting.** It
*sets* a level rather than stepping one, and at construction it does not know the level it replaces —
so no severity can be derived and only a **direction-agnostic** disposition may answer it. That is
why gregariousness and misanthropy carry it and pride does not.

| | |
|---|---|
| `inspect humors` | all four queues by composition, newest first. The only assertable proof an emotion landed — the chip says what the player was told, the queue is what moved |
| `--grant-mm <id>` | the starting kit is random, so whether a body *holds* a disposition is otherwise a coin flip. Every emotion test grants one |
| `cli/outcome/emotion/success.cli` | the narration half — pickpocket, avarice, Voluptas into the spleen |
| `cli/system/emotion_dialogue.cli` | the conversation half, on `meet_stranger` for the reason above |

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
the body offers "cut the pelt" and "cut the liver" together, in a single phase. Identical parts
collapse to one goal (`PersonaChoiceSelector` de-dupes by label): a pig with three `Meat` offers one
"cut the meat", and each cut removes one instance while the goal remains.

**A body yields four to eight parts; a tiny creature's `catch` yields two or three.** Below four a
carcass is one cut and a shrug, which does not pay for the kill, the knife and a noetic point per
attempt; above eight the goals run past what a phase can offer and past what the pack can hold, so
the surplus reads as litter. Duplicates count toward the eight and not toward the goals. **A human is
butchered like anything else** — meat, offal, bone, skull, hair — and their belongings are the
separate PoI beside the body, because that is grabbed and this is cut.

**The parts are one general vocabulary, and nothing in it names a species** (`BodyPartItem`, in
`src/game/narrative/items/corpse/BodyPartItems.cs`). A wolf, a cow and a man all give up `Meat` and
`Bone`. What makes a bear a bear is *which* parts and *how many* — a skull and two claws — never a
"Bear Meat" beside the `Meat` everything else gives. That middle case is what this replaced: there
was an `AnimalHide` and a `DeerHide` and a `GoatHide`, a `Feather` and a `ChickenFeather` and an
`EagleFeather`, so which item a carcass gave depended on which convention its archetype was written
under. Where a real distinction exists it is a **word**, not a prefix: `Hide`/`Pelt`/`Skin` are three
grades of one material, and so are `Fang`/`Tooth`, `Horn`/`Antler`, `Feather`/`Plume`. When adding an
animal, compose from what is there; add a part only when it is a thing no existing word covers.

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

### A verb teaches per body, not per verb

**A verb's lesson is a candidate list, not one id.** `Verb.GrantedModusMentisIds(target)` returns them
**most restrictive first**, and `ResolveGrantedModusMentisId(target, actor)` walks it — the target's
own override first, then the candidates — and takes the first one *this body can hold*. A null actor
(audits, tooling) takes the first without asking.

This exists because ten verbs taught the protagonist **nothing at all**. Climbing, scaling, stairs,
crossing, smelling, digging, tracking and catching are done by both anatomies and are not the same
lesson for both: a beast clambers with claws where a person scales with arms, and neither modus mentis
names an organ the other owns. With one id per verb, `ModusMentisGrantOutcome.For` refused the lesson
and wrote a log line — so `smell` had 86 authored declarations of `scenting`, a **snout** skill, and
every one of them taught a human nothing.

**Order beast before human.** A beast's lesson names a beast organ, so a human cannot hold it and
falls through cleanly. The reverse is not true — `spoor_eye` names eyes and anamnesis, which a beast
also owns — so putting the human lesson first silently takes the beast's own lesson away from it.

The pairs today: clambering/`scaling`, surefoot/`balance`, scenting/`keen_nose`, digging/`spadework`,
spoor_reading/`spoor_eye`, soft_mouth/`snatch`. `cross` puts the crossing's own lesson ahead of both.

**`--verb-audit` now asks this per anatomy** and fails naming any verb whose whole candidate list is
unlearnable by a body that could be offered it. The beast side of that fault is real and not yet
fixed: `VerbAudit.NoBeastCounterpart` lists the sixteen verbs a beast companion can be offered and
learn nothing from, with the reason. **Empty that list, do not extend it.**

### The senses reach living things

`SensoryVerb` used to gate on `target is PointOfInterest`, so the four senses could not touch anything
alive: you could listen to a tree and not to the lark in it, and the only verbs a bird accepted were
the ones that killed it. Three parts now:

- **`NpcArchetype.Senses`** — the same `SensoryProfile` a PoI carries. Default `Examinable`; a person
  (`NamedNpcArchetype`) rewards all four, birds rewards all four, tiny creatures examine and
  contemplate, and the two that sing (cricket, bee) add listening;
- **`NpcArchetype.VerbModiMentis`** plus `SceneNpc : IVerbModusMentisSource` — the NPC half of the
  override mechanism `PointOfInterest` has always had, declared per *kind* rather than per individual.
  A person teaches `physiognomy` and `hearkening`; every non-human teaches the naturalist's set
  (`creature_lore`, `musk_reading`, `fellow_feeling`) and a bird adds `birdsong`. Note this is keyed
  on **species, not class** — `NamedNpcArchetype` carries every wolf and bear as well as every baker;
- **a sleeper is excluded** from the NPC branch. `SceneNpcPlacement` swaps them and their bed for one
  merged `SleepingNpcPointOfInterest`, which has its own senses and its own lessons; offering both
  would be two routes to one act, differing only in which object the phase opened on.

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
| `FirstBlowOutcome` | `First blow:` | attack |
| the modus mentis grant | `Modus mentis` | examine, listen, smell, contemplate — no scene outcome of their own |

Match the **prefix** only; what follows is a name or a room and moves with the content. `Modus mentis`
is deliberately cut short of its noun: a first grant reads `acquired`, a repeat reads `learned`
(`ModusMentisPracticeOutcome`, whose `ShowInUI` is false when nothing moved).

Three verbs assert something else instead, and for a reason: **`get_up`** and **`remember`** are
phase transitions, and **`ignore`** does nothing by design. **`attack`** used to be a fourth — the
fight screen replaced the narration before any chip could be read, so `wait mode Fighting` was the
only assertion available. It now holds the fight for the CONTINUE, so its chips are readable like
anything else and `wait mode Fighting` comes *after* that press.

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

**`click keyword <n>` for pinned phases.** Which word an observation highlights comes from the prose,
not from the display name: the object's own word is preferred, so `"Brew Barrel"` usually highlights
`barrel` — but only when the persona wrote that word and the POS tagger read it as a noun, and
neither is a script's to guarantee. (Catalyst loads on a background task, so a script that runs
early gets the rule-based fallback and a different word.) Clicking by index means "whatever this
phase opened on". Name-matching stays right for hand-written scripts: it reads as intent and
survives reordering.

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
its own anatomy cannot learn, that no NPC carries a **wound** its own anatomy does not own, that no
**archetype-specific** trait offers either (a shepherd trait reaching for a beast's scenting teaches
nothing — global traits are exempt, being dealt to every anatomy), and that sex agrees with the
genitories score. It finishes with one fully-generated NPC printed in full, which is the quickest
way to see whether a trait you just wrote actually reads well on a person.

**Beasts are a second pass, and they are where the anatomy checks earn their keep.** The main sample
is `SpeakingArchetypes`, a hand-written list of humans, so for a long time the audit generated no
animal at all — and every anatomy check above was therefore asking a question it could not fail.
`CheckBeasts` runs the same per-individual checks over every non-human archetype, **found by
reflection** so a new animal is covered the day it is written. It was vacuous by construction until
a beast was in the sample: disabling the wound filter turns up 12 warnings across the seven animals,
which is the way to confirm the check still bites after touching it.

Two things differ for an animal and are handled rather than warned about: a beast archetype has no
trait pool of its own, so it is dealt the global draw alone (the trait-count check reads the pool
size rather than a constant), and it carries nothing, so the table drops the items column.

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
- a `ReferenceToolIds` entry no item matches, which makes a `Required` verb permanently *impossible*
  rather than merely hard — and the two ways that declaration can fall out of step with `ToolUse`:
  `Required` naming no tool (refused always, satisfiable never) and a non-`Required` verb naming
  tools nothing reads;
- a scale point or cliff whose **top** area holds nothing — a climb that costs a roll and arrives
  somewhere with no points of interest at all. This replaced a check that counted landmark areas,
  which the landscape refactor made meaningless.

It also prints the **implement categories** — the counts, the `Required` verbs with their reference
tools, and the `Excluded` list in full. That last one is printed rather than counted because it is
the only declaration in this audit that is pure judgement; every other warning here has an answer the
code can check, so the only review the exclusions can get is a reader running an eye down them.

It closes with the **anatomy** table: what each anatomy may attempt and what its body rules out
(`Verb.EffectiveCapabilities` — *not* `RequiredCapabilities`, which omits the handcraft a `Required`
verb implies and reports a beast as able to `slay`). Never a warning — a beast barred from 26 of 53
verbs is the design — but it keeps the cost of that design visible, and makes the next anatomy's
poverty one line to read.

Run it after adding a verb, a connector type, or a batch of scene content.

### Checking which verb teaches which modus mentis

```bash
dotnet run -- --mm-grant-csv [path]        # default mm_grants.csv
```

A spreadsheet rather than a report, because the question it answers is one nobody asks once. **The
grant rule has two halves and neither is readable from one place**: a verb declares a default
(`Verb.GrantedModusMentisId` — EXAMINE teaches scrutiny) and the object may say better
(`IVerbModusMentisSource` — an anvil teaches metalcraft). So what a lesson is worth depends on what
the world *places*, and how often, which is only visible by sweeping. It uses the same factory ×
location id × area × period × object space `--verb-probe` does and writes one row per distinct
(verb, object, modus mentis), with the reproducing flags on each row.

Four grants do not hang off a target and are named by their route rather than dropped, since each
would otherwise read as an absence:

| `GRANT_SOURCE` | |
|---|---|
| `verb-default` / `target-override` | the two halves of the rule |
| `dialogue-tree:<id>` | the conversation's own lesson, **on top of** the verb's — begging is social interaction *and* beggary. `gather_knowledge/topic` is the third one, drawn from the topic and the person |
| `per-blow` | ATTACK declares null on purpose: the lesson is the fighting skill `FirstBlow` drew |
| `in-fight learning check` | a fighting skill's modus mentis, reachable in a fight and by no verb |
| `not-taught-by-any-verb` | the other direction, which no per-verb reading can answer. Not a fault by itself — a skill dealt at generation or in childhood is a different thing from one the player can practise, and nothing else states which is which |

`MM_RESOLVES` is the same check `--verb-audit` fails on (a typo grants nothing, silently) and
`MM_LEARNABLE_BY` is the anatomy gate, so a lesson no body can hold reads `NOBODY`.

Run it after adding a verb, an object's `VerbModiMentis` override, a dialogue tree, or a batch of
modi mentis.

### A lesson from the circumstances, not only from the act

`CircumstanceGrants` answers a different question from the verb's own grant: not "what did I just
practise?" but **"what did doing it under these conditions teach me?"** — and the conditions vary far
more than the targets do. Forcing a door is `forcing` whoever does it; forcing a door with an enemy
watching, at a difficulty well past your dice, while already wounded, is three different lessons and
none of them is about doors.

Three rules make it safe to have at all:

- **It is additional, never a replacement.** The practical lesson still lands and this is a second
  chip. Displacing the verb's grant would cost the player the craft knowledge they were going for,
  which would make every disposition read as a punishment.
- **At most one per action**, first match wins, so the ordering *is* the design: the body's condition
  outranks being watched, being watched outranks the difficulty, and the plain per-verb table at the
  bottom is what fires on an ordinary day.
- **Anatomy is not consulted here.** `ModusMentisGrantOutcome.For` refuses a lesson the body cannot
  hold and logs it, exactly as it does for a verb's own grant.

Three layers, in order: **conditional rules** (wounded, watched, illegal, private, night, at
difficulty 5), then **target rules** matched on the object's *name* — because the distinctions that
matter are content ones and not code ones, and one `DiggableGroundPointOfInterest` covers peat, a
garden bed and a grave — then the **per-verb table**, which is simply the other half of what each act
is. A miss at any layer falls through, so an unnamed object is never worse off.

The rules are `record Rule(string Id, Func<Circumstance,bool> When)` rather than lambdas returning
ids, so **the ids are data**: `AllGrantedIds` enumerates what the class can teach without executing
anything, which is what lets `--mm-reach-csv` count this as a route instead of guessing at it.

### A conversation's lesson depends on the branch and the person

`DialogueTree.AdditionalGrantedModusMentisIds(npc, resolution)` had one implementer and now has
thirteen. Begging a reeve teaches `humility` where begging a farmhand teaches `weeping`; a provocation
aimed at authority teaches `insolence` where one aimed at a peer teaches `boasting`.

**Mind the difficulty ladder when gating on it.** `BranchDifficulty.Hard` only reaches 3 at depth 4,
so a `Difficulty >= 3` gate inside a tree whose branches are two deep is a branch that never happens
— which is exactly how `insolence`, `dramaturgy` and `open_mindedness` were written, wired and
unreachable. `--dialogue-audit` prints each tree's branch lengths; read them before choosing a gate,
or key on the person instead, which every tree can answer.

**The caught-red-handed trees are built by a factory and never registered**, so anything reading
`DialogueTreeRegistry.All` misses them — and they are the trees a thief meets most. `MmReachAudit.AllTrees`
adds them back explicitly.

### Checking how much of the modus mentis catalogue a player can reach

```bash
dotnet run -- --mm-reach-csv [path]        # default mm_reachability.csv
```

The question `--mm-grant-csv` raises and cannot settle. **A lesson arrives by five routes and a verb
sweep sees one of them**, so "62 modi mentis are taught by a verb" reads as an answer and is not
one. This gathers all five per modus mentis — **childhood** (every reminescence fragment's granted
skill types), **fight** (the modus mentis unlocking any fighting skill on a medium the body owns,
which is what the learning check teaches and what `attack`'s first blow draws), **action** (shared
with `--mm-grant-csv`, so the two cannot disagree), **dialogue** (a tree's own lesson, including
GATHER KNOWLEDGE's topic grants, which reach every archetype's `TradeModusMentisId`), and **work**
(the three every job pays in).

**Anatomy is the second axis and is not optional.** `ModusMentisGrantOutcome.For` *refuses* a lesson
a body cannot hold — an absent organ contributes nothing to the level cap, so granting anyway would
file a skill stuck at level 1 for ever. So a route naming a modus mentis is only half of reaching
it, and the report splits `protagonist` from `beast companion only` for that reason. Counting them
as one is what makes an audit of this say 183 where a player can hold 122.

**The failure it exists to catch is a verb whose lesson its own most likely user cannot learn**, and
that is silent twice: the refusal is a log line, and a `success.cli` asserting `expect Modus mentis`
still passes on the *practice* chip the dice chain leaves. Ten verbs were in that state — climbing,
scaling, both stair verbs, `cross`, `smell`, `dig`, `track` and `catch` all taught beast lessons to a
human who cannot hold them — and are now anatomy pairs (see "A verb teaches per body"). The beast
side of the same fault is real and listed in `VerbAudit.NoBeastCounterpart`.

### Checking which compute device the LLM will run on

`--llm-probe-audit` re-runs the first-launch hardware probe and prints the whole comparison, ignoring
the cached answer and writing nothing back:

```bash
dotnet run -- --llm-probe-audit
```

It benchmarks the CPU and every installed GPU backend on **both halves of inference** and prints a
table of prompt-read rate, generation rate and the derived cost of one representative request, then
the verdict and the margin. When the CPU wins despite a GPU generating faster, it says so explicitly.

**Both rates matter, and the game is prompt-heavy.** Cathedral answers several-hundred-token prompts
with a handful of tokens — a persona choice is 4, a critic 20–60 — so prompt processing is most of
the wait. `LlamaProbe` scores candidates on `WorkloadPromptTokens`/`WorkloadGenTokens` (400/80)
rather than on either rate alone, and a GPU must beat the CPU by `RequiredGpuSpeedup` to be chosen at
all, because the GPU path has strictly more ways to fail.

That weighting is not theoretical. A Qualcomm Adreno X1-45 generates at 11.9 tok/s against the same
machine's CPU at 5.4 — a clear win — while reading prompts at **1.1 tok/s**, twelve times slower than
that CPU. Scoring generation alone picked Vulkan, and the game then sat on its loading bar
indefinitely: nothing errored, nothing timed out, the first 495-token batch simply never finished.

Run it after touching `LlamaProbe`, and on any machine where the game is inexplicably slow to
generate — this is the report that says whether it picked the wrong device and by how much.

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
- **The keyword ranking.** The *first* keyword is the object's own word wherever the prose contains
  it (`pig` for the pig), so that much is the same under `--playground` and is scriptable. What a
  script cannot see is the ranking behind it — the second keyword of a long observation, and the
  whole of any sentence that named its object some other way. Two reasons: placeholder prose is one
  frame reused for every object, so there is no real vocabulary to rank; and the ~6s `WordEmbedding`
  load, which a real run hides behind the model load (it is started in the controller's constructor
  precisely so it can), has nothing to hide behind under `--playground` — a script reaches its first
  observation in ~2s and ranks by word length instead. That second one is inherent to the mode, not
  a defect. Verify a ranking change against the vectors directly, not through a script.
