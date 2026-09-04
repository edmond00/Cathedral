# Cathedral

**"Cathedral" is the development name.** The game is published as **Proscribed
Palimpsest** (edmond00.itch.io/proscribed) and every player-facing surface says so: the window
title, the main menu, the shipped executable, the download. The repository, the namespaces, the
Debug binary and `%APPDATA%\Cathedral` keep the old name deliberately — see "Naming" in the
`release` skill.

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

## Rules that always apply

Six prohibitions that hold everywhere in the codebase. Every one of them fails *silently* —
nothing throws, nothing warns, and the symptom surfaces far from the cause. They are kept here,
always loaded, rather than in the skills and nested files that carry the rest of their chapters.

**Never write `new Random()`.** Every generator comes from `GameRng` (`src/Rng.cs`): `Stream("tag")`
for anything drawn from repeatedly, `For("tag")` for a one-shot, `DerivedSeed("tag")` when something
else wants a plain seed. One unseeded generator anywhere in the pipeline makes `--seed` a lie, and
the symptom — a script that passes four times and fails the fifth — never points at the cause. The
seed is resolved at the very top of `Program.cs`, before any other flag is parsed, because touching
a mode class runs its static initializers; a reader that beats the parse locks in the boot default
and `Initialize` will say so on stderr.

Two `new Random(<constant>)` calls are deliberate, both in `SkyCloudRenderer`: the star sphere
(`Config.SkyCloud.SkySeed`) and the cloud drift axes. They are seeded but *not* from `GameRng`, and
that is the point — the sky has to be the same sky in every run on every machine, because a moon in
it names a world, and a player picks their world by clicking one. A sky seeded from the master seed
would rearrange itself the instant it was used. Neither makes `--seed` a lie: both are fixed, and
neither touches gameplay.

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

**Name things, never coordinates.** `click keyword hearth` survives layout changes and reads as
intent; `click cell 34 12` breaks on the next UI tweak. Use `regions` to discover handles rather
than reading a `dump` and counting rows. (`click cell` exists as an escape hatch for UI that has no
region support yet.)

**Never enable trimming.** The audits reflect over `Outcome` and `Verb` subclasses to build their
catalogues, and Catalyst and MSAGL load types by name. A trimmer removes precisely those, and it
fails at runtime on a player's machine rather than at build.

**Shader sources must stay ASCII.** An em dash in a `//` comment inside the sphere's vertex shader
took the game down at startup with `VS compile: error C0000: syntax error, unexpected $end at token
"<EOF>"`. GLSL tokenizers reject the non-ASCII bytes even inside a comment, and report it at the end
of the source rather than at the offending line — so the message points nowhere near the cause.

**The grid is not a setting and must not become one.** `GlyphScale` grows the glyph inside its cell
because cell size is the window divided by `MainWidth`/`MainHeight`, and every renderer hardcodes
its rows against that 100x100 — the grid is layout. Fullscreen is the player's lever on cell size.
The scale ceiling is where the quad's overflow shows on box-drawing glyphs (side rails, popup
frames, the solid volume bars), which fill their cell to the edge where letters do not.

## Verifying a change (read this before testing anything)

The game is an OpenGL app driven by mouse clicks, so "run it and see" is not something an
automated agent can do directly. Use **`--cli`**: the game still opens its window and renders
normally, but it also accepts commands on stdin and can print any screen back as text.

Everything the driver prints is prefixed `[cli]`, so filter with `grep '^\[cli\]'` to separate it
from the game's (very chatty) diagnostic logging on the same stdout.

**Invoke the `verifying` skill before writing or running any `.cli` script.** It carries the
reproducibility flags, the full command vocabulary, `--debug`'s forced outcomes, the test-suite
layout, `--verb-probe`, and what `--cli` cannot check. **Invoke `audits` after touching content**
— it carries the eleven headless audits and says which one covers which change.

When the user wants to *play* the change rather than read `[cli]` output — anything visual, anything
about feel, "let me try it" — **invoke `playtest`**. It carries the baseline launch command, the flags
that put them in front of a given feature, and the standing rule that a run is `--cpu` unless the GPU
itself is what is being tested.

## Where the rest of it lives

This file used to carry everything. It was ~204,000 characters, five times the size at which a memory
file starts being truncated, and all of it was re-read at the start of every session whether or not it
had anything to do with the work at hand. It is now split by *when it applies*. Nothing was deleted —
every line that left is the same text, moved.

| | loads | what is in it |
|---|---|---|
| `src/game/CLAUDE.md` | when working with files under `src/game/` | saving and the save contract; `HandleEscape` and the phases it must answer; fights, the first blow, victory, death, companion death; wounds and healing and the modi mentis a wound takes away; verbs/actions/outcomes; the noetic economy; tools and the four gates; emotions; corpses; crime; landscapes; recruiting; what survives a visit; what a body can do; the senses; the affinity ladder; circumstance and dialogue lessons |
| `verifying` skill | on invocation | `--cli`, the reproducibility flags, the command vocabulary, forcing outcomes, the test suite and `cli/` layout, `--verb-probe`, extending the CLI, adding a debug flag, what `--cli` cannot check |
| `playtest` skill | on invocation | launching the game for the user to play by hand: the baseline command, which debug flags reach which feature, the CPU/GPU rule, when `--playground` belongs on |
| `audits` skill | on invocation | `--outcome-audit`, `--crime-audit`, `--dialogue-audit`, `--npc-audit`, `--mm-audit`, `--item-audit`, `--verb-audit`, `--mm-grant-csv`, `--mm-reach-csv`, `--llm-probe-audit`, `--building-audit` |
| `runtime` skill | on invocation | `models/model.gguf` and the llama.cpp backends, `-ngl`, the first-run probe, the connection-pool and streaming contracts, server-start fallback, `log.txt` and the `logs/` tree, the crash report, the Settings screen |
| `release` skill | on invocation | the ten release steps, and now the packaging, publishing, naming and shipped-build-verification reference behind them |
| `manual` skill | on invocation | the player manual's style guide, chapter map and procedure |
| `models` skill | on invocation | maintaining the `models/` folder across machines |
| `mm-grants` skill | on invocation | writing the modus mentis grant audit by hand |

**When a change alters a rule a player is subject to, invoke the `manual` skill** — that has not moved
and is stated in full above.
