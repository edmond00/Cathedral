---
name: playtest
description: Launch the game for the user to play by hand, with the flags that put them straight in front of whatever this session changed. Use whenever asked to run, start, launch or open the game, to "let me try it", "see it in action" or "test the feature myself" — and to decide which debug flags a hands-on run needs, whether it should be CPU or GPU, and whether --playground belongs on. Not for scripted verification, which is the `verifying` skill.
---

# Playtesting a change by hand

The user plays; you launch. This skill is the *hands-on* counterpart to `verifying`: no script, no
assertions, just the game opened on their desktop with the flags that make the thing they want to
look at reachable in under a minute. Everything here is about **which flags** — the launch itself
is one command.

`verifying` is the other half. Use it, not this, when the answer should come from `[cli]` output
rather than from the user's eyes. Use this when the change is visual, when it is about feel, or
when the user says they want to try it.

## The baseline command

```bash
dotnet run -- --skip-childhood --view --mm --seed 43 --fill-party --cpu
```

This is the user's habitual run and the default. Do not shorten it without a reason.

| Flag | Why it is in the baseline |
|---|---|
| `--skip-childhood` | Skips the childhood reminescence and get-up phases, filling starting skills and items from the seed. Those phases are several minutes of LLM narration before the game proper starts. |
| `--view` | Opens the LLM and scene viewers, without `--debug`'s console prompting for every decision. This is what makes a hand-played run legible — you can see what the model was asked and what it answered. |
| `--mm` | Fills every empty memory slot with random unheld modi mentis, so verbs gated behind a modus mentis are actually offered. |
| `--seed 43` | Same world, same spawn, same kit every time — the user recognises where they are, and a second run reproduces the first. It also skips the moon-selection screen. |
| `--fill-party` | Fills the party to max_companions (last slot a beast). Anything about companions, party narration or the acting member needs bodies in the party. |
| `--cpu` | **The GPU crashes this machine.** See below. |

## CPU is the default, and it is not a performance choice

**Always pass `--cpu` unless the session is specifically about GPU behaviour.** Running the model on
the GPU has crashed the user's desktop. This is a stability rule, not a speed preference — a run
that is merely slow is not a reason to reach for `--gpu`.

Offer `--gpu` only when the run is *about* the GPU: a change under `src/LLM/`, a backend swap in
`models/llama/backends/`, `-ngl` and layer offload, the first-run compute probe, or the compute row
of the Settings screen. When you do offer it, say plainly that it is the flag that has taken the
desktop down before, and let the user decide. Never add it silently, and never carry it forward to
the next launch — the next run goes back to `--cpu`.

`--no-llm-probe` is the harmless neighbour: it skips first-run device detection and uses whatever is
saved. Add it when repeated launches are spending time re-probing and the device is already right.

## `--playground` on or off

`--playground` replaces every LLM call with instant placeholder text. No model load, no server, no
waiting — the game reaches its first observation in about two seconds instead of the better part of
a minute.

**Turn it on** when the session's work does not depend on generated prose: UI and layout, the glyph
sphere, the camera, the sky, the world map and travel, menus and settings screens, inventory,
fight-mode mechanics, save/load, the party panel. The user gets to the screen in question in
seconds, and the placeholder text is beside the point.

**Leave it off** when the prose *is* the thing: prompt changes, persona rewriting, the narration
preview box's content, dialogue text, keyword extraction and ranking, anything under `src/LLM/`, and
any judgement about whether the writing is good. A `--playground` run can never answer that — see
"What `--cli` cannot check" in `verifying`, which applies identically to a hand-played run.

When the session touched both, ask. It is one question and it saves a wasted launch: *"Playground
for a fast look at the layout, or the real model so you can read the prose?"*

## Flags for what the session actually changed

Pick these on top of the baseline from what this session worked on. The point is that the user
should not have to walk to the feature.

| If the session touched… | Add |
|---|---|
| A village/farm/cave-specific scene, an area, a building | `--start-at <biome>` and `--start-area <room>` — spawn in the location *and* the room. Without the second one it is an observation, a think and an action per step to walk there. |
| A scene factory regardless of biome | `--location-type <name>` — builds every location with that factory whatever the terrain. `--location-id <n>` pins the exact scene. |
| A verb that is tool-gated | `--grant-item axe,pick,shovel,fishing_rod` — the starting kit is random, so the verb is otherwise refused outright. |
| A verb or skill behind a modus mentis | `--grant-mm <id>[:lvl]` when a *specific* one is wanted; `--mm` (already in the baseline) when any will do. |
| Fighting, wounds, weapons, the first blow | `--start-fight wolf` (or bear, bandit, brigand) plus `--weapons`. `--encounter-on-arrival <creature>` instead when the arrival-step case matters. |
| An NPC, dialogue, affinity, recruiting | `--npc-affinity <level>` to skip earning acquaintance, `--npc-static` to pin everyone to one room, `--auto-dialogue` to settle conversations instantly, `--npc-hostile` for enmity. |
| A beast: appeasing, taming | `--spawn-beast wolf` — a wilderness factory rolls one in only 10-40% of visits. |
| Time of day, the night door rule, routines | `--period <name>` (dawn…night). |
| Healing, ageing, anything on the world clock | `--advance-days <n>` — a wound takes 100-1000 days to close. |
| Starvation, humors | `--black-bile`. |
| Travel and the world map | `--no-encounters` to stop random encounters interrupting, `--allow-reentry` to re-enter your own vertex. |
| The world's division into regions | Nothing to add -- press **R** on the world map. It repaints the sphere by region instead of by biome (gray and ochre for the land, the sea left purple) and R again puts the biomes back. A developer key, so it is off under `--no-developer-keys`. |
| The world-selection sky | Drop `--seed` — the flag names the world outright and skips that screen. The sky is drawn from a constant, so it is still the same sky. |
| A save-format change | `--save-path <file>` against a scratch file. Never let a playtest write the real save while a format change is in flight. |
| A shipped-build behaviour | `--no-developer-keys` — the developer shortcuts (D/M/F/G/H/J, R, C/V, W/S) are off in a shipped build. |

`dotnet run -- --help` is the full list and is authoritative; the table above is what tends to
matter for a hands-on look.

**If nothing in the table reaches the feature, add a flag.** Adding debug-only options is expected,
not a last resort — do not hunt for a lucky seed. Propose it to the user, then follow "Adding a
debug flag when a feature is hard to reach" in `verifying`: the switch goes on `Config.Debug`, it is
parsed in `Program.cs` beside `--seed`, it must be inert at its default, and it goes in the `--help`
block and in `verifying`'s flag table with a note on *what it is for*. Add it to the table above too
if it is the kind of thing a playtest will want again.

## Launching

1. **Kill anything still running first — the runner SHELL, not just the game.** A live
   `run_tests.sh` does not merely compete for the build output. **After every script it runs
   `Get-Process -Name Cathedral | Stop-Process -Force`** — every instance on the machine, by name,
   not only its own. Its guard for that (`run_tests.sh:144`) checks only at suite *start*, so it
   cannot know about a window opened afterwards. A suite left running **will kill the playtest,
   mid-session, within a script or two.**

   Query with the PowerShell tool, never `powershell -Command` from Bash — Bash eats `$_` before
   PowerShell sees it:

   ```powershell
   Get-CimInstance Win32_Process -Filter "Name='Cathedral.exe' OR Name='bash.exe' OR Name='dotnet.exe'" |
     Select-Object ProcessId,Name,CommandLine | Format-List
   ```

   Kill the runner shell **first** (it respawns the game otherwise), then the games, then confirm
   both counts are zero:

   ```powershell
   Get-CimInstance Win32_Process -Filter "Name='bash.exe' OR Name='sh.exe'" |
     Where-Object { $_.CommandLine -like '*run_tests*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
   Get-Process Cathedral -ErrorAction SilentlyContinue | Stop-Process -Force
   ```

   **Stopping the background task is not enough.** `TaskStop` kills the tracked shell; the
   `run_tests.sh` process and its children survive it and carry on looping. Clear the suite's lock
   afterwards too (`rm -f "${TMPDIR:-/tmp}/cathedral-tests/run_tests.lock"`), or the next real suite
   refuses to start.

2. **Build, and read the result.** `dotnet build`. A playtest launched over a failed build silently
   runs the last good binary, and the user plays the old code without knowing it.

3. **Launch in the background** so the session stays responsive while they play:

   ```bash
   dotnet run -- <flags>
   ```

   with `run_in_background`. The window opens on their desktop; you are notified when the process
   ends — which is not the same as the user closing it, see below.
   If the launch is refused or sandboxed, hand them the line to run themselves — in this session,
   `! dotnet run -- <flags>` puts the output straight into the conversation.

4. **Say what you launched and why**, in one or two lines: the command, and which flags you added
   beyond the baseline for this particular change. The user needs to know what they are looking at.

## After the window is gone

- `log.txt` in the repo root is the last run's log; `logs/` holds the tree.
- `log-crash-<timestamp>.txt` in the repo root means it crashed — read it before asking what
  happened. There are a lot of these already; check the timestamp is the run you just launched.
- **A window that dies with no exception, no `log-crash-*` and exit 127 was killed, not crashed.**
  Suspect a surviving test runner before you suspect the change — check for a live `bash
  ./run_tests.sh` and for a `log.txt` still being written *after* the window died, which is the
  clearest tell that something else was running. Do not report a kill as a crash, and do not report
  it as the user closing the window: this has happened more than once, and reading it as a fault in
  the session's own code sends the next hour in the wrong direction.
- If they report something wrong, the fix is a code change verified with `verifying`, not another
  blind playtest. Reproduce it in a `.cli` script if it can be reproduced that way — a script that
  fails is worth more than a second hand-played run.

## What a playtest is not

It does not replace the audits (`audits`) or the scripted suite (`verifying`). It proves the thing
looks and feels right; it proves nothing about content coverage, determinism, or the twenty other
paths through the change. When the user is happy with what they saw, the change still needs whatever
its own verification is.
