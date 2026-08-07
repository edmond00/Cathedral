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
| `--seed <n>` | Fixes the master RNG so the run is repeatable — world, spawn, dice, the starting kit, and every choice `--playground` stands in for. Two runs with the same seed produce byte-identical `[cli]` output; only LLM text (which carries no seed) is exempt. |
| `--skip-childhood` | Skips the childhood + get-up phases and fills starting skills/items randomly — from the master seed, so the same seed gives the same kit. |
| `--debug` | Lets `strategy` force action outcomes. Under `--cli` it never prompts — see below. |
| `--start-at <name>` | Spawns the protagonist on the first biome or location matching `<name>` (`village`, `farm`, `field`, `cave`…). Without it you get wherever the seed puts you, which is usually plain or forest — testing anything village-specific otherwise means hunting for a lucky seed. |
| `--start-area <name>` | Opens narration in the first area of the location whose name matches `<name>` (`pigsty`, `smithy`, `hall`…). `--start-at` picks the location, this picks the room. Without it you arrive in whichever area the factory built first — a farm's courtyard — and reaching any other one costs an observation, a think and an action **per step**, with the persona choosing what is observable at each. Anything that lives in one room (a pigsty's pigs, a smithy's anvil) is otherwise a long walk away. Inert when the location has no such area, so it can be left on across a multi-location run. |
| `--observe-only <name>` | Restricts what an observation phase may look at to objects matching `<name>`. **This is what makes a specific object reachable at all.** A phase opens on ONE object the persona chose out of a dozen, so without it a script that wants to act on the pig is waiting on a coin flip it cannot influence — and re-rolling seeds until the right object comes up first is not a test. Whole words match before substrings (`pig` means the pig, not the Courtyard–Pigsty Track), and a phase where nothing matches falls back to the whole scene rather than narrating nothing. |
| `--no-encounters` | Suppresses random travel encounters. **Pass this in every script that travels.** An encounter puts the game in `EncounterPrompt`, where a script waiting for `LocationInteraction` sits until its timeout and reports a failure that has nothing to do with what it was testing. |
| `--period <name>` | Pins the arrival time of day (`dawn`…`night`) instead of drawing one at random. Needed for anything period-gated: every building's entry door shuts at `night`, and a random draw reaches that one visit in six. |
| `--fill-party` | Fills the companion roster to its heart-derived ceiling (`max_companions`) right after the childhood phase, with NPCs generated from random archetypes — the **last** slot a beast, every slot before it a human. Recruiting even one companion in play takes a conversation and a check (or an appease *and* a tame), which is a long approach for anything that just needs a populated party. Note the ceiling is the *heart* score, and a protagonist accepted straight out of creation has a heart of 1 — so a plain script gets one beast and nothing else. For humans too, raise the heart first (`click cell 94 24` on the creation screen bumps it, four presses reaching 5). |
| `--start-fight <creature>` | Drops straight into a fight on reaching the world map (`wolf`, `bear`, `bandit`, `brigand`). **The only way a script can reach fight mode at all** — the real routes in are a random travel encounter (which every script disables with `--no-encounters`, precisely because it fires unpredictably) and provoking a location NPC through a conversation and a check. |
| `--grant-mm <id[,id…]>[:lvl]` | Grants the named modi mentis at `lvl` (default 1) after character creation. Fighting skills are gated behind their modi mentis, so this is what makes a given skill reachable — and since a **buff's cost falls as its level rises**, it is also how you exercise both ends of that curve. Unknown ids are reported on stderr. |
| `--advance-days <n>` | Pushes the world clock forward `n` days on first arrival at the world map. The clock only moves on travel arrival and work stints while a wound needs 100–1000 days to close, so without this nothing about healing is observable. Fires once, before anything has happened — for healing *a wound taken in play*, use the `clock` command instead. |

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
  fight-end <victory|death|runaway>
  advance [presses] [secs]  settle, then press the preview box's CONTINUE until it is gone.
                            USE THIS, not a bare `click continue`, to get from a keyword click
                            to the action list — see the trap below
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

### Corpses

A kill spawns a `CorpseSpot` in the area it happened in, through `Scene.AddSpotToArea` — the one door
both routes come through, the `slay`/`attack` verb applying `NpcSlaynOutcome` and a won fight
spawning one body per dead enemy. Tiny creatures are the exception and leave nothing.

Two rules the rest of it depends on:

- **A spawned spot is not yet observable.** The narration graph is built once, from the areas as the
  factory left them, so anything the game spawns during play has no observation object — and an
  object with no observation object cannot be looked at, entered or acted on, however correct it is
  in `area.Spots`. `NarrativeController.SyncSpotObservations` reconciles the two before every
  observation phase and every thinking request (it runs from `RefreshSceneVerbs`). That sync is why a
  corpse is reachable at all; without it the body existed and the player could never be told.
- **A corpse opens the next narration phase, alone.** `PendingCorpseObservations` collects what fell,
  `GenerateObservationsAsync` drains it, and `GenerateCorpseObservationAsync` observes every body in
  the order it fell — first plainly, each later one through a transition — with no persona choice and
  no length gate, the way the post-dialogue opener imposes the person just spoken to. It runs *ahead*
  of the threat opener: a corpse is a one-shot event consumed on the spot, while an enemy still
  standing leads the phase after this one anyway. The list is drained whether or not it is used, so a
  body left in an area the player has since walked out of cannot open a phase two moves later.

Note what is **not** solved: the narration graph is area-shaped, and `RefreshSceneVerbs` gates verbs
with `new PoV(area, period)` — no `InSpot`. So while the real PoV does enter a corpse, the graph never
does, and the body-part PoIs inside it never become observations. `CutVerb.IsPossible` requires
`pov.InSpot is CorpseSpot`, so **harvesting a corpse is currently unreachable through narration**;
the corpse can be observed and entered, and that is where it stops.

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
every skill is filed in a memory module and within its organ-derived cap, and that sex agrees with
the genitories score. It finishes with one fully-generated NPC printed in full, which is the quickest
way to see whether a trait you just wrote actually reads well on a person.

Run it after touching `NpcContentGenerator`, any archetype's generation block, or any trait.

### Checking the modi mentis

`--mm-audit` prints the whole MM catalogue and the health of its content rules, headless:

```bash
dotnet run -- --mm-audit
```

The hard rules (R1–R10, listed on `ModusMentisRuleValidator`) are **fatal**: `ValidateOrThrow` runs
them at startup, so a rule-breaking MM can never reach the game — the audit is how you read what
broke without launching. They cover function combinations, memory-type agreement, fighting-skill
cross-references, and the two anatomy rules that are easiest to get wrong:

- **R5** — an MM's `Organs` is **exactly 1 body region XOR exactly 2 distinct organs**, canonical ids
  only, never a mix. There is no "primary" entry: every one contributes to the level cap through its
  `IMaxLevelContributionStat` (organ +0..+3, region +0..+6), so code reading only `Organs[0]` is a bug;
- **R10** — every organ and region owns exactly one correctly-scoped contribution stat. Without it
  `GetMaxLevelForModusMentis` contributes +0 and silently caps every MM related to that source at
  level 1 — a wrong number in the memory menu and nothing anywhere to say why.

Then the soft targets, which only warn: the ~80/20 two-organ vs one-region split, the morality and
memory-type distributions, ~10% discrete, and per-organ/region coverage (R6 makes fewer than 5
related MMs fatal, so the coverage table is really about spotting the ones scraping the floor).

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
- a location with fewer than two landmark areas, so a horizon observation has nothing to name.

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
