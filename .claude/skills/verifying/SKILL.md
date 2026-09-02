---
name: verifying
description: Drive and test the game from a script: the --cli driver, the reproducibility flags, the command vocabulary, forcing outcomes with --debug, the test suite layout and the cli/ ranges, extending the CLI for a new feature, and adding a debug flag when a feature is hard to reach. Use whenever writing or running a .cli script, adding a debug flag, verifying a change by running the game, or asking how to reach a phase, verb or object from a cold start.
---

# Verifying a change

The game is an OpenGL app driven by mouse clicks, so "run it and see" is not something an
automated agent can do directly. Use **`--cli`**: the game still opens its window and renders
normally, but it also accepts commands on stdin and can print any screen back as text.

Everything the driver prints is prefixed `[cli]`, so filter with `grep '^\[cli\]'` to separate it
from the game's (very chatty) diagnostic logging on the same stdout.

> Two rules from the root `CLAUDE.md` bear repeating here, because they bite hardest in scripts:
> **never write `new Random()`** (it makes `--seed` a lie), and **name things, never coordinates**.

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

