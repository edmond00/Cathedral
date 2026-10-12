# Tripling the narration-reachable modi mentis

Status: **implemented.** Derived from `--mm-grant-csv` and `--mm-reach-csv` (2026-08-19).

| | before | after |
|---|---|---|
| catalogue | 183 | **287** |
| **reachable through narration + dialogue** | **68** | **207** (3.04x) |
| reachable by the protagonist, any route | 122 | **261** |
| human-learnable and reachable by nobody | 37 | **2** |

The two left are deliberate: `childhood_reminescence` is phase-only and `gnawing` is a beast's
lesson. Every one of the 37 orphans is now reachable.

---

## What was wrong, and what fixed it

### 1. A verb taught one lesson to two anatomies

Ten verbs taught the protagonist **nothing at all**. Climbing, scaling, both stair verbs, crossing,
smelling, digging, tracking and catching all named beast organs — `smell` had 86 authored
declarations of `scenting`, which needs a *snout*. `ModusMentisGrantOutcome.For` refused every one
and wrote a log line nobody read, and the tests passed on the dice chain's practice chip.

**Fixed by** `Verb.GrantedModusMentisIds` — an ordered candidate list, walked by
`ResolveGrantedModusMentisId(target, actor)`, which takes the first lesson the acting body can hold.
Six human counterparts written: `scaling`, `balance`, `keen_nose`, `spadework`, `spoor_eye`, `snatch`.

**Order beast before human.** A beast lesson names a beast organ so a human falls through cleanly;
the reverse is not true — `spoor_eye` names eyes and anamnesis, which a beast also owns.

`--verb-audit` now asks this per anatomy and fails on it. That immediately found the same fault
running the other way: sixteen verbs teach a beast companion nothing, listed in
`VerbAudit.NoBeastCounterpart`. **Empty that list, do not extend it.**

### 2. The senses could not touch anything alive

`SensoryVerb` gated on `target is PointOfInterest`, so you could listen to a tree and not to the lark
in it. Every bird, insect, beast and person in the game accepted only the verbs that killed them.

**Fixed by** `NpcArchetype.Senses` + `NpcArchetype.VerbModiMentis`, `SceneNpc` as an
`IVerbModusMentisSource`, and the sensory gate accepting a living target. A person teaches
`physiognomy` and `hearkening`; every non-human teaches `creature_lore`, `musk_reading`,
`fellow_feeling`, and a bird adds `birdsong`. Keyed on **species, not class** — `NamedNpcArchetype`
carries every wolf as well as every baker.

### 3. One verb owned all the variety

`examine` carried 35 of the 37 per-object overrides in the game. Meanwhile **86 objects declared
`smell -> scenting` and 37 declared `listen -> keen_ear`** — the declaration sites existed and all
said the same word.

**Fixed by** rewriting those 125 sites contextually, keyed on the object's own examine lesson
(`cellarcraft` smells of `bouquet`, `firecraft` of `smoke_reading`, `husbandry` of `byre_sense`), and
by 40 new sense modi mentis.

### 4. Nothing could be learned from the circumstances

**New mechanism: `CircumstanceGrants`.** A verb's grant answers "what did I practise?"; this answers
"what did doing it under *these conditions* teach me?", and conditions vary far more than targets do.
Three layers — conditional rules (wounded, watched, illegal, private, night, difficulty 5), target
rules matched on the object's name, then a per-verb table — first match wins, always **additional**
to the verb's own lesson. This is what reaches the whole disposition set.

### 5. Every conversation taught the same two things

`AdditionalGrantedModusMentisIds` existed, was virtual, and had **one** implementer. Now thirteen:
begging a reeve teaches `humility` where begging a farmhand teaches `weeping`.

---

## The 104 new modi mentis

| group | count | examples |
|---|---|---|
| anatomy counterparts | 6 | `scaling`, `balance`, `keen_nose`, `spadework`, `spoor_eye`, `snatch` |
| smell | 10 | `taint_sense`, `bouquet`, `smoke_reading`, `petrichor`, `charnel_sense` |
| listen | 10 | `birdsong`, `night_ear`, `water_voice`, `forge_ear`, `crowd_murmur` |
| examine | 10 | `physiognomy`, `hallmark`, `coin_eye`, `mortuary_lore`, `provenance` |
| contemplate | 10 | `awe`, `reverence`, `vanitas`, `wanderlust`, `ruin_sense`, `superstition` |
| working verbs | 18 | `ore_lore`, `seamcraft`, `felling`, `coppicing`, `netcraft`, `wardcraft` |
| creatures | 8 | `creature_lore`, `musk_reading`, `fellow_feeling`, `stillness`, `mimicry` |
| dialogue | 14 | `deference`, `banter`, `parley`, `oathmaking`, `gossip`, `plain_dealing` |
| disposition | 20 | `curiosity`, `nerve`, `dread`, `resolve`, `thrift`, `spite`, `recklessness` |

Two overloaded lessons were split: `mine` teaches `ore_lore` rather than `stonework` (a *mason's*
lesson four jobs already pay in), and `topographia`'s three unrelated jobs became `trailcraft`
(follow_path), `parish_lore` (asking about a district) and `topographia` (a landscape).

---

## Balance, measured

| | before (68) | now (207) | target |
|---|---|---|---|
| morality Low / Med / High | 10 / 50 / 8 | 35 / 139 / 33 | ~20/60/20 |
| memory Proc / Sens / Sem | 33 / 19 / 16 | 71 / 70 / 66 | ~33/33/33 |
| anatomy sources with zero coverage | 4 | 0 | 0 |

The four sources that had *no* narration-reachable lesson — paunch, teeths, hippocampus, visage — are
all covered. Eyes and hands, which carried 42 of the old 68, no longer dominate.

`--mm-audit` hard rules stay green; R13 is the one to watch when writing a disposition, since an
Observation-only modus mentis must be Medium morality and so any moral flavour needs Thinking or
Action.

---

## Still open

- **Beast counterparts.** Sixteen verbs teach a beast companion nothing
  (`VerbAudit.NoBeastCounterpart`). Same fault as the one just fixed, other direction.
- **`--verb-probe` and the test location.** New objects and lessons are not represented in
  `TestSceneFactory`, so the sense variety is thin under `--location-type test`.
- **The manual.** Chapters covering what verbs teach need reconciling; run `/manual-sync`.
