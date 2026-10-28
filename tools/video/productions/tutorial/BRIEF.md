# Brief: the scripted run behind the tutorial video

You are working in a cloud session on the branch `video-tutorial`. **This branch is never merged**,
so small code changes that make the scenario reachable or replayable are allowed (see "Code changes"
below). Commit and push to `video-tutorial` often: every working milestone, so nothing is lost if
the session ends.

## The deliverable

One script, `tools/video/productions/tutorial/tutorial.cli`, that **plays the whole scenario below
by itself**, unattended, under `--record`, from a cold start to the closure, plus:

- `tools/video/productions/tutorial/NOTES.md`: the exact launch flags (seed, organs, humors and the
  rest), what each section of the script relies on (which seed/world/vertex, which childhood
  choices, which NPC gives the job, which shop sells the boots), every code change you made and
  why, and every known fragility.
- A `mark <section>` at the start of every section of the outline and a `clip <name>` … `clip end
  <name>` span around each one, named after the outline (e.g. `clip organs_dialogue`, `clip
  fight_1`, `clip trade`), so the footage can be cut later without reading the timeline by hand.
- Generous `hold`s wherever the narration will need to explain something on screen (each organ, each
  submenu, each fight turn, the humor queue after the consumable), with `cursor rest` before them.

**Out of scope:** manim graphs, voice-over, the storyboard, the final recording. Only the script.
Do not write `storyboard.json` or `scenes.py`.

## Before anything

1. Read `CLAUDE.md`, then invoke the **`verifying`** skill (the CLI vocabulary, the reproducibility
   flags, `--debug`, forcing outcomes) and the **`video`** skill (`--record`: only what a player can
   do; a target that cannot be located on screen is refused). Read
   `tools/video/productions/cities/city.cli` as the worked example of a `--record` script.
2. Set up the machine: `bash tools/linux/cloud-setup.sh`, then the GloVe vectors and the model
   (`tools/linux/fetch-assets.sh --glove --model`; Stanford may be blocked: the `music` skill's "A
   fresh cloud session" section gives the gensim GitHub mirror). **Check the model's SHA-256 against
   `models/README.md`**: two different files both call themselves Qwen2.5-3B Instruct Q4_K_M, and
   only the shipped one is what the game is tuned for. The install check prints a mismatch at
   startup. Report it rather than work against the wrong model.
3. `dotnet build Cathedral.csproj -r linux-x64`; run everything under `xvfb-run -a`, with
   `export ALSOFT_DRIVERS=null`. `tools/linux/README.md` covers the rest. The video tooling's
   `setup.ps1` is Windows-only; on Linux make a venv from `tools/video/requirements.txt` yourself
   (`.venv/bin/python` where the skill says `.venv/Scripts/python`).
4. The model runs on the CPU and llvmpipe renders; a passage can take minutes. Use long timeouts on
   every `wait`/`advance`, and run long runs in the background.

## The rule that matters most: it must replay elsewhere

The script will be **recorded later on the developer's Windows machine**, on a different CPU and a
different llama.cpp build. `--seed` fixes everything *except the LLM's text*, which carries no seed.
So a script that only works because the model happened to write a given keyword, or happened to
offer a given dialogue option, **will break on replay**.

- Steer by **names and types**, never by the model's wording or by coordinates (`click keyword`,
  `click option`, `regions`, `elements`; see the CLAUDE.md rule "Name things, never coordinates").
- Wherever progress depends on LLM output (a keyword to click, a dialogue branch, a job offer), make
  the step robust: pick a target the game guarantees (a point of interest or NPC that exists by
  type), use `--debug` forced outcomes where the skill allows it, or add a small debug hook (below).
- **Prove it:** once the script runs end to end, run it **at least three more times from scratch**
  and confirm each run reaches the closure. If you can, vary the run so the LLM text actually
  differs between runs (another sampling temperature, or an LLM seed if the runtime exposes one)
  while the game seed stays fixed. A script that only passes with identical prose is not done.
- Finish with one full pass under `--record` (via `make_video.py record`), because `--record` refuses
  targets that `--cli` alone would accept. Report its duration and confirm `game.mkv` and
  `timeline.jsonl` are written. Do not commit the recording (`build/` is ignored).

## The scenario

Played with the real model (no `--playground`, which stands in for the model and would make the
footage meaningless). Full childhood, **not** `--skip-childhood`. Encounters **on**: the two fights
during travel are part of the video. If the natural encounter rate cannot be made to produce exactly
one fight on each of the two journeys reliably, add a debug flag that forces it (below).

```
- start "new" run
- select a world: [the video will talk over world generation, history generation, the lore]
    -> the world-selection screen must be shown, so the run cannot use --seed's skip of it.
       See cli/system/world_selection.cli (reproducible because the sky is drawn from a constant)
       and how the seed then has to be fixed some other way. Hold on the sky, point a moon, pick it.
- protagonist creation
    - visit each organ one by one, hovering or selecting it, with a hold on each
        - order = grouped by the main system they feed (dialogue, narration, fighting, ...)
          -> derive the grouping from the code (which systems read which organ score), write it
             down in NOTES.md with the source location for each, and follow it in the script
    - the explanation stays abstract here; systems are shown concretely later
- childhood / get up sequence
    - play the childhood reminiscence for real, holding on each step
    - a childhood path that ends with a WEAPON and a CONSUMABLE in the inventory but NO FOOTWEAR
      -> find the choices (and the seed) that guarantee this; NOTES.md says which and why
- normal run start
- open the anatomy submenu: hold
- open the memory submenu: hold
- open the inventory submenu: hold, then consume the consumable
- open the humors submenu: hold on the queue that shows the consumable's effect
- travel to a farm / field
- a fight during the journey: played turn by turn with holds, won with the weapon
- continue the journey
- reach the farm / field
- first task = find a job. Talk and act through narration and dialogue until a job is obtained
- job menu: hold
- travel to a village / city
- a fight during the journey, won with the weapon
- continue the journey
- reach the village / city
- new task = buy boots
- trade menu: hold, buy the boots
- inventory: wear the boots
- no coins left: the reason to go back and work at the farm / field. Show the routine / mental map
  system that makes this possible (hold on it; actually doing the work is optional)
- closure: end on a calm frame and a long hold
```

Where the outline names something the game does not have under that name (the branch is
`biome-overhaul`, which changes settled places: a "farm/field" or "village/city" may now be a
section of a sprawl), use the nearest real equivalent and say so in NOTES.md. The coins from the job
must be enough for the boots, then run out. Arrange that with the seed, the starting kit or a debug
flag if it does not happen naturally.

## Code changes

Allowed, because the branch is never merged, but keep them small, behind flags where possible, and
listed in NOTES.md. Good examples: a `--force-encounter <n>` debug flag, a flag to fix a childhood
draw, a CLI command or region handle for a UI element that has none (the `verifying` skill covers
extending the CLI and adding a debug flag), a guaranteed job-giver at the farm.

Do **not**: write the outcome of a scene into the script by hand-editing game state, bypass the real
pointer path, or make the footage show something a player could not see. The video presents the real
game. The six rules in CLAUDE.md "Rules that always apply" hold here too (no `new Random()`, types
not strings, etc.).

When something cannot be made to work, say so plainly in NOTES.md with what you tried. Do not paper
over it.

## When you finish

Push the branch. Your last message lists: the final flags, the script's length in real time on the
cloud machine, how many from-scratch replays passed out of how many tried, every code change, and
every step you consider fragile on replay.
