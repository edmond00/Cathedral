---
name: audits
description: The seventeen headless audits that check content rather than code: --outcome-audit, --play-audit, --crime-audit, --dialogue-audit, --npc-audit, --mm-audit, --will-audit, --will-lesson-audit, --item-audit, --verb-audit, --world-variant-audit, --history-audit, --mm-grant-csv, --mm-reach-csv, --llm-probe-audit, --building-audit and --biome-audit. Use after adding or editing a verb, an outcome, a kind of play or where plays are placed, a modus mentis, a will's targets or places, an object's lesson (VerbModiMentis), an item, an NPC archetype, a dialogue tree, a scene factory, a world variant, the world history generator or the lore catalogue, or a batch of scene content, and to find out which audit covers a change.
---

# The audits

Each one is headless (no LLM, no window) and runs in seconds to a minute.
`./run_tests.sh audits` runs them all.

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

### Checking where PLAY is offered

```bash
dotnet run -- --play-audit
```

PLAY's content is spread across three routes — a factory's own objects, creature archetypes and
`PlayPlacement`'s pass over a finished scene — so no one file says how much of the world can be
played in. This builds every factory the verb audit sweeps (up to 20 ids each) and counts the areas
where `PlayVerb` accepts something at some hour, for a default protagonist. About a minute.

- **The share** — mean of the factories' shares, held to 20–32% (✗ outside), with the pooled share
  over every area beside it. Cities and citadels are hundreds of rooms, so the pooled figure moves with
  the indoor rules (dormitories, halls, roofs) and the mean with the wilderness; keep both near 25%.
- **Kinds of play** — areas offering each `PlayActivity` subclass; ✗ on one no scene offers.
- **What carries them**, **how each is worded** (one line per carrier and name — read it after adding a
  `{0}` template; a plural object under a singular pronoun shows here), **lessons taught to a person**.
- **Each play's lessons** — ✗ when no body the play is offered to can hold any of them.
- **By kind of area** — areas, areas with a play, share. What the chances in `PlayPlacement.Rules`
  were tuned against.

Run it after adding a kind of play, a plaything, a play on a factory object or an archetype, or after
changing a placement chance or a factory's areas.

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
- a line whose expansion opens a sentence in lower case. A token's value is written for one place
  and spliced into many — an opinion is authored to sit mid-sentence ("of the work I think {…}") and
  is also set as a sentence of its own — and the seam is where it shows ("thank you for asking. my
  back."). `DialogueTemplate.Expand` capitalises any token at a sentence start, and fits an opinion
  to both sides (lower case mid-sentence, a stop before a following sentence); this is the check
  that it did, for every archetype;
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

The **wills** section fails (✗) on two shares, not one: wills in the catalogue (36–44%), and wills
among **the lessons the offered acts teach** (28–42%) — every act the sampled locations offer counts
its `ResolveLesson` once, so `grab`, offered everywhere, weighs as it does in play. The second is the
one a player feels and the first cannot see: it read 56% while the catalogue read a third, because
nearly every verb's own lesson had been made a will of that verb. Below it the 25 lessons offered
most, wills marked. Before making a modus mentis a will, look it up there: one near the top moves
the learned share by points.

It lists every target of every will and fails (✗) one that no location offers.

The **persona wording** section (`PersonaLint`) fails (✗) on a `StyleInstruction` or
`PersonaReminder2` written as things: two or more concrete nouns, or a style opening a props list
("images of", "narrate the"). It loads the GloVe vectors to tell a concrete noun, so this audit takes
a few seconds more than it did. The rules and why are in the `choice-judge` skill ("Writing the
voice"); `tools/prompts/persona_lint.py --text "…"` checks a wording before it goes in.

Run it after adding or editing a modus mentis, a fighting skill, an organ, or a body region.

### Checking where wills have something to do

```bash
dotnet run -- --will-audit [out.json]          # the report; the JSON feeds reports/multi-target-wills.html
dotnet run -- --will-trace <will-id> [LOCATION] # why one will has, or lacks, something to do there
```

`--mm-audit` asks whether a will's targets are offered *anywhere*; this asks the question a player
meets, with the planner itself (`ActionPathPlanner.Plan`): standing here, is there a way to anything the
will wants? Every location factory the audits sweep is built for three ids (one for a town or city), and
every will planned from the entry area and from the first area of a kind it declares it belongs in
(`ModusMentis.WillPlaces`, rule R19), at morning and at night. About three minutes. Three numbers:

- **coverage** — the share of (will, start) with a plan, overall and per location kind;
- **related coverage** — the same from the will's own places: the headline, since a will idling where a
  player expects it to work is the playtest complaint. **✗ under 60% overall** (64.5% over 114 one-target
  wills before they had several; 64% over 159 after, and 69% for the original 114); a will under 60%
  in its own places is marked `!`;
- **breadth** — the share of location kinds where the will has something to do. **✗ above 75% for a
  will with several targets**: added targets are how a desire grows into a general mind (`cross`,
  `scale_up`, `provoke`, fuel or wood as a second target did exactly that). A single target whose act
  is everywhere (to sit, to smell) is marked `~`, not failed. Breadth is judged on the will's **own**
  targets: one bound to its lesson (`.Teaching<…>()`, below) reaches only what the content already says
  teaches the will, so it neither counts toward "several" nor toward the breadth that fails.

Wills whose every target needs a state play brings about (a conversation that needs an acquaintance,
a beast to calm, a corpse) and that no fresh scene shows are listed `·` and left out of the shares. The
actor carries nothing, so a will whose act needs an implement counts only where one can be taken up or
bought on the way. Run it after changing a will's targets or places, a location factory, or the planner.
The last section prints the area kinds each location kind builds — what to read when declaring places.

### Checking that a will wants what teaches it

```bash
dotnet run -- --will-lesson-audit
```

The playtest complaint behind it: examining the petroglyphs teaches **archeology**, the player picks
archeology to think about the petroglyphs, and archeology — a will to dig, to study a relic, to fetch a
spade — says it has no interest in them. Every other audit was green: the lesson resolves, the will is
offered somewhere, and the refusal is the planner working as designed.

The relation is read off the content, never judged: **an object's lesson** — its declared
`VerbModiMentis` (`IVerbModusMentisSource`) and what `Verb.ResolveLesson` gives a human for an act on it.
Every location factory is built for up to four ids, every area at morning and at night, and every
observation object (a point of interest with its items, a person, an area — what a keyword reaches) is
asked, for each will an act on it teaches, what the thinking phase asks: does anything it offers begin a
shortest path to the will (`ActionPathPlanner.PathThrough`)? About a minute.

- **✗** — the object **declares** the will its lesson and offers nothing the will wants or passes on its
  way. The fix is almost always the will's lesson-bound target: it ends on
  `WillTarget.To<ExamineVerb>(purpose).Teaching<ThisModusMentis>()` (`ToAny` for several verbs), which
  matches exactly the objects declaring that lesson for those verbs. If the will has none yet, that is
  one slot of its five. If the will plainly should not want the object, the lesson is what is wrong.
- **note** — the lesson is the verb's own (its default, or a contextual case in its `Lessons`): about the
  act wherever it is done, not about the object, so it is listed and not failed. Six today (gossip
  taught by sitting on a bench, vantage by scaling a giant tree…).
- A pairing that is right as it stands goes in `WillLessonAudit.Accepted` with the reason; an entry that
  no longer refuses is itself a ✗, so the list cannot go stale.

It read 167 ✗ (63 wills) when written; every one was closed by a `Teaching<>` target. Run it after
adding or editing a will's targets, after giving a point of interest or an archetype a `VerbModiMentis`
that names a will, and after a batch of scene content. `cli/system/will_lesson_object.cli` is the
reported case, played.

### Checking the item catalogue

`--item-audit` reads `ItemRegistry` and reports the whole catalogue, headless. Coverage is
automatic — any item with a public parameterless constructor is audited the moment it is written:

```bash
dotnet run -- --item-audit
```

It prints the census by `ItemCategory` and subcategory, the wearable table (max armour dice per body
section, garments per social standing), liquid/vessel reachability, trade-tag coverage (each tag's sellers, buyers and items — read it
when a trader's shelf looks wrong for their craft, see "What a trader sells" in `src/game/CLAUDE.md`), the weight
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

**The weapon rules fail the suite** (✗ lines, unlike the warnings above), and the weapons table
prints each weapon's size, weight, level and the arm level that holds it:

- a weapon smaller than medium — two would share a nine-slot hand. `WeaponItem` seals `Size` behind
  `WeaponSize`, which has no small value, so this only fires for an `IWeaponItem` written outside it;
- a weapon not `IsImprovised` whose `Level` does not exceed the best human striking limb (read off the
  anatomy, 3 today) — no better than a fist. `WeaponItem.StrikingLimbCeiling` must agree with it;
- the real weapons unbalanced across the three hold tiers — fewer than a fifth of them light, medium
  or heavy (an arm of level 0/1/2 holds up to that weight), or any insignificant weapon.

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

### Checking the world variants

```bash
dotnet run -- --world-variant-audit
```

A world variant is eleven numbers — three feature scales and eight thresholds — read through
`WorldShape` to turn three Perlin fields into terrain. **Every way of getting them wrong produces a
world that generates without complaint**, which is what this exists for:

- a waterline half a point too high leaves an archipelago of six-cell islands, and the run ends when
  the player opens the travel map and finds nowhere to go;
- a treeline half a point too low buries the livable ground — and since the settled country sprawls
  only over livable cells (`SettlementSprawl`), that world has almost no people in it at all;
- a coast band raised without the waterline under it leaves the shore below the sea and the deep
  ocean above the shallows, which still generates, just with no shoreline anywhere.

None of those throws. The audit builds the terrain of every variant on the first sixteen moons a
player could pick it on, and per world checks land share, spawnable share, region count, and — on
three of the sixteen, the ones it also divides into regions and gives a history (`PeopleWorld`) — the
**settled country**: at least 150 sprawled locations in the world, at least 40 on the richest
landmass (the size of a home worth waking in), and at least 100 **viable spawns**, the cells beside
the sprawl that `SpawnRule` draws the waking place from. The `dead` column is the share of otherwise
spawnable ground the spawn rule throws away; it is informational and properly high in a world of
islands.
It also checks the table itself: ids unique, every `WorldVariant` subclass registered in
`WorldVariants.All`, every variant reached by some moon in the sky, the shape orderings sound, every
name and blurb short enough for the moon box — and **every biome and location kind drawing in a
glyph of its own** (`BiomeDatabase.SettledGlyphs`), since two kinds sharing one are two kinds the map
cannot tell apart.

Headless by construction: it builds the mesh through `IcosphereGeometry` and classifies through
`WorldShape`, both of which the running game uses too, so what it measures is the world a player
would walk.

**It classifies through `WorldClassifier`**, the one pipeline the game and `HeadlessWorld` also use
(terrain layers, then `CoastRule`, then `ClimateRule`), and checks the spawn through `SpawnRule`,
which `InitializeProtagonist` calls — so neither can drift from the game. It faults any land cell on
the water that is neither shore nor sea ice (`CoastRule` makes the shore exactly the land on the
water). Per variant it prints the hot and cold shares of the land and the **zone counts** — connected
hot or cold stretches of 30+ cells, water included — for reading, not judging. It faults two share
bounds: a temperate variant (offset 0) may have no more than 6% hot or cold land; a hot or cold
variant must be at least 45% its own climate.

Run it after touching a variant's numbers, `WorldShape`, the biome thresholds, `SpawnRule`,
`SettlementSprawl` or `SettlementTable`, `WorldVariant.SettlementDensity`, a settled glyph, or anything
in `WorldRegions`. Walk what it
reports with `--world-variant <id>`.

### Checking the world histories

```bash
dotnet run -- --history-audit
```

Every world carries a generated history (`src/game/history/`, see its `CLAUDE.md`), thousands of
events long, and **every way of getting one wrong is silent**: a ruler crowned after his death, a
port in a landlocked region, a realm that still holds land after it was dissolved, a Register numeral
with a gap below it, a lore beat whose seed was sown after the present and so never sprouted, a
world written into the shared empire catalogue (a death or a child on a lore figure), and above all
a history that comes out differently the second time, which Continue refuses with nobody the wiser.

Two halves:

- **The empire.** The hardcoded lore (`EmpireLore`): the Register of Blood (every numbered Ban name
  has every lower number, in birth order), no one dies before being born or is born to a parent under
  twelve or long dead, no one accedes outside their own life.
- **The worlds.** Every named lore moon, two unnamed members of each lore group (Oox's, Fogun's) and
  the first four generic moons of each imperial status, built headless through `HeadlessWorld` (the
  same terrain and regions the game builds) and **generated twice**; the two hashes must agree. Per
  world: ownership table and realm sets agree, standing realms at the present between three and half
  the regions, every place on ground it can stand on, every war ended, every `ScriptedSeed` sprouted,
  the imperial chapter matching the status (a held world has contact, conquest, Inquisition and
  Stranding; an unvisited one has none), the lore name and forced variant right, and generation under
  two seconds. Last, the empire catalogue's fingerprint is compared before and after all of it.

About forty worlds in a minute or so. It prints a table (regions, events, figures, realms ever and
standing, faiths, places, wars, milliseconds, hash) and a sample of Belune's lore beats.

Run it after touching anything under `src/game/history/`, the lore catalogue, `LoreMoons`, the region
division, or a world variant a lore moon is forced to.

### Checking the wilderness: diversity, layout and exploration

```bash
dotnet run -- --biome-audit [biome]        # e.g. --biome-audit mountain, --biome-audit hotsteppe
```

`--building-audit` checks that a place is *built* right; this one checks that a wild place is *worth
arriving at more than once*. It builds 40 locations of every wilderness biome (`BiomeAudit.Biomes`:
the six temperate factories and every `ClimateSceneFactories` entry) and scores three things:

- **diversity** — the share of every triple of samples whose members all differ from one another, in
  content (area kinds and section names, Jaccard distance ≥ 0.34) *and* in layout signature (section
  sizes, edges, loops, branch points, climbs). Three forests should be three forests. `jac` is the mean
  pairwise distance, `lay` the number of distinct layouts in 40;
- **coherence** — every area reachable, every section one connected piece, and the factory held to
  the shape it states. `WildSceneFactory` subclasses set `Profile` and give each section a `Tier`:
  **Pyramid** (bands that narrow as they rise, meeting only their neighbours, a summit of at most
  two), **Descent** (levels meeting only their neighbours, one mouth), **Radial** (every section
  touches the hub), **Spine** (every section touches the line it hangs off), **Mosaic** (three or more
  sections meeting on at least as many borders). `chain%` is the share of builds that are a bare
  corridor;
- **exploration** — the movement kinds besides a walk that a visit offers: climb, scale, cross, swim,
  slip in, follow a path, voyage toward a landscape. `move` per visit, `3+%` of visits with at least
  three, `seen` how many a biome ever offers.

It fails (✗) a biome under 40% diversity or outside 5–10 areas, and lists every layout fault as a
warning. **Read the per-biome block, not just the row**: it gives each area kind's and section's
frequency, and a kind at 100% is usually what holds diversity down — the cure is another kind of
place, not reweighting, because larger places drawn from the same pool overlap more.

Before the overhaul the wilderness averaged 5% diversity (ten biomes at 0%) with two sections each;
after, 70%, every profile honoured. Run it after touching a wilderness factory, `WildSceneFactory`, a
wild area kind, or `OutdoorLayout`.

### Checking buildings, scenes and NPC schedules

`--building-audit` generates every location type across 60 location ids and checks the structural
invariants that fail *silently* in play — nothing throws, the scene just quietly misbehaves:

```bash
dotnet run -- --building-audit
```

It sweeps `SettledSceneFactories.ForAudit` — every location the sprawl and history can put on the
map (forty-odd agriculture, stock, settlement, city, historical and ruin keys) plus the same factories
forced onto other ground they can stand on (a mountain city, a snowfield burg), since the climate
changes what they are built of. It first runs `CropCatalog.Validate`, so a crop the location table
names and the catalogue lacks fails here rather than on a player's arrival.

Per factory it prints the range of areas, NPCs and doors (and how many are locked at noon), plus the
sections that were generated and how often. Then it warns about:

- private ground entered from public ground by anything but a locked door or an intrusion (slip-in,
  scale, climb, crossing): an open way, a stair or an unlocked door into private, a door locked from
  its private side, or a public area reached only through private ones (`CheckPrivateBehindLocks`).
  The player must never open a plain door into a room where every act is trespass;

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
- a settled location whose arrival area is empty at its arrival hour while somebody stands out of
  doors elsewhere with nothing to keep — the guarantee `SceneFactory.EnsureArrivalCompany` makes. Each
  inhabited factory also prints `arrival … in N/M peopled locations`; what falls short of M is the
  one- and two-person households where everyone is the sole keeper of a hall or a store;
- stable keys that are missing, colliding, or **different across two builds of one location id**.
  That last one matters because procedural descriptions are seeded from the key, so a key that moves
  re-rolls how every door in the place looks between visits.

It finishes by printing real door prose from both sides and at night, and by driving a
`SyntheticObservationObject` directly to prove the viewing area and the period stamp actually reach
the description — a door that lost either would still read perfectly well, just always from outside
and always by day.

Run it after touching `BuildingFactory`, any scene factory, `NpcSchedule`, or the observation
description path.

