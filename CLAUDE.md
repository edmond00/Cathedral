# Cathedral

A Windows desktop narrative RPG built in C# combining a 3D glyph-sphere world with local LLM-driven storytelling. The aesthetic blends roguelike exploration with Chain-of-Thought narrative AI.

## Build & Run

```bash
dotnet build
dotnet run                  # main game
dotnet run -- --help        # all options
```

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
| `--seed <n>` | Fixes the master RNG (world, spawn, dice) so a run is repeatable. |
| `--skip-childhood` | Skips the childhood + get-up phases and fills starting skills/items randomly. |
| `--debug` | Lets `strategy` force action outcomes. Under `--cli` it never prompts — see below. |

A typical invocation:

```bash
dotnet run -- --playground --skip-childhood --debug --seed 42 \
              --cli-timeout 180 --cli-script test.cli 2>&1 | grep '^\[cli\]'
```

The process exits **non-zero** if any `expect` assertion failed or any `wait` timed out, so a
script doubles as a regression test.

### Command vocabulary

Run `help` for the authoritative list. The essentials:

```
  state                     mode + phase flags (loading, dice, noetic, history/total lines)
  dump [--color]            the terminal grid as text; --color tags each row dim/mix/lit
  regions                   what is actionable right now — the handles `click` accepts
  world / destinations      world-map state; reachable vertices by name

  click menu <label>        main-menu button
  click continue            protagonist-creation Continue, or a settled dice overlay
  click keyword <name>      a narration keyword
  click action <n>          a narration action
  click option <n>          a dialogue reply
  click button              the footer button (LEAVE / INTERRUPT / END / CONTINUE)
  choose <n>                answer the visible popup by index
  travel <vertex|name>      world travel (bypasses 3D picking entirely)
  scroll up|down [n]        scroll the shared history buffer

  strategy <succeed|fail-dice|fail-plausibility|auto>
  fight-end <victory|death|runaway>
  wait [secs] | wait mode <GameMode> [secs]
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
travel 4597                 # clicking your own vertex enters the current location
wait mode LocationInteraction 60

strategy succeed
click keyword attention     # keyword -> Think -> pick a modus mentis
choose 0
choose 0
wait 45
state                       # → history=0  total=19  noetic=4/5

click action 0              # execute -> dice -> confirm
choose 0
wait 60
click continue
wait 60
click button                # footer CONTINUE opens the next segment
wait 60
state                       # → history=21 total=27 noetic=5/5
dump
quit
```

### Extending the CLI for a new feature

When you add UI, add its handles too, or it will not be testable:

1. Expose the hit regions your renderer already tracks (see `MainMenuRenderer.CliButtons`,
   `NarrativeUI.KeywordRegions`, `DialogueTreeController.CliOptions`).
2. Surface them on `LocationTravelGameController` as a `Cli*` member — that class is the single
   seam the driver talks to.
3. List them in `CliDriver.CmdRegions` and handle them in `CmdClick`.

If the new phase is asynchronous, make sure `LocationTravelGameController.CliIsIdle()` accounts for
it, otherwise `wait` will return while the phase is still building.

**One trap worth knowing**: `NarrativeState.IsDiceRolling` rests at `true` when no roll is active
(`ClearDiceRoll` sets it that way), so always gate it behind `IsDiceRollActive` when testing for
business. This already caused one bug in `CliIsIdle`.

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
every skill is filed in a memory module and within its organ-derived cap, and that sex agrees with
the genitories score. It finishes with one fully-generated NPC printed in full, which is the quickest
way to see whether a trait you just wrote actually reads well on a person.

Run it after touching `NpcContentGenerator`, any archetype's generation block, or any trait.

### What `--cli` cannot check

- **Anything about pixels**: camera framing, the glyph atlas, sky/cloud rendering, and whether the
  3D sphere looks right. `dump` sees the terminal cell grid, not the framebuffer.
- **Ray picking**: `travel` injects a vertex index directly, bypassing
  `GlyphSphereCore.FindVertexByMagentaRayIntersection`. If that math breaks, scripts stay green
  while the game becomes unclickable. Keep one manual click-through in your release routine.
- **Narration quality**: `--playground` produces placeholder prose. A passing script proves flow,
  state and layout — never that the prompts still produce good writing.
