---
name: runtime
description: The language model runtime and the app's runtime plumbing: models/model.gguf, llama.cpp backends and GPU support, the first-run device probe, the HTTP connection-pool and streaming contracts, server-start fallback, log.txt and the logs/ tree, the crash report, and the Settings screen with its glyph and compute rows. Use when touching src/LLM/, CrashReport.cs, GameLog.cs, UserSettings.cs or the Settings screen, or when diagnosing a failed phase, a dead connection or a slow model.
---

# The language model runtime and the app's plumbing

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


The Settings screen exposes device, GPU layers, threads and re-detect. **All of them apply at the
next launch**, and the screen says so: the server loads the model once at startup and holds it, so
applying a change in place would mean tearing it down, re-reading two gigabytes and rebuilding every
cached persona slot, possibly mid-narration. Re-detect therefore measures nothing on the spot — it
discards the saved probe signature so the probe re-runs during the next launch's model load, where
it is already behind a loading screen.

`--cpu` and `--gpu` override all of it for one run through `Config.Debug.ForcedLlmDevice`. They
deliberately do *not* write to `UserSettings`: flags are parsed before `UserSettings.Load()` runs, so
a flag that wrote there would be overwritten by the file a moment later.

