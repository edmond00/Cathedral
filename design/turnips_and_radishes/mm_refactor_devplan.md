# Modus Mentis Content Refactor — Devplan

Large refactor of all Modus Mentis (MM) content: a new `Fighting` function, hard structural
rules validated at every launch (throw + exit on violation), soft statistical targets, a data
pass over every existing MM, and up to ~50 new MMs.

## 0. Decisions locked in

| Question | Decision |
|---|---|
| "809%" in draft | Typo for **80%** (2-organ MMs) vs 20% (1-region MMs) |
| Organ-medium rule scope | Counts only skills where the MM is the **main** (`RequiredModusMentisId`). Main-MM links may be reassigned where needed |
| Min-5 coverage scope | **Everything**: all 26 organs and all 8 body regions, beast anatomy included |
| Fighting function behavior | **Declarative only** (no gameplay change), with **both-way** validation: Fighting MM ⇒ linked to ≥1 skill, and any MM referenced by a skill (main or secondary) ⇒ has Fighting |
| New MM registration | Auto-registration via reflection is fine; "not integrated" only means not wired to NPCs/archetypes/story |

## 1. Current state

- ~112 concrete auto-registered MMs in `src/game/narrative/modimentis/` (runtime-constructed
  ones like `ChildhoodMemoryModusMentis` take ctor args and are skipped by the registry).
- `ModusMentisFunction` enum: Observation, Thinking, Action, Speaking ([ModusMentis.cs](../../src/game/narrative/ModusMentis.cs)).
- `ModusMentisRegistry` already fails fast at startup for one rule (`ValidateDialogueModiMentis`:
  Speaking ⇒ PersonaTone). The new validation follows the same pattern.
- `Organs` arrays freely mix 1–3 organ/region ids today (e.g. Brawling: arms, hands, legs) —
  most MMs will violate the new "1 region XOR 2 organs" rule and need editing.
- Fight side: 49 skills, each with a main MM + secondary MMs and organ/body-part/weapon
  mediums ([FightingSkill.cs](../../src/fight/FightingSkill.cs), [fighting.md](fighting.md)).
  22 MMs are referenced by fight skills (the list in fighting.md §MODUS MENTIS).

### Anatomy id inventory (validation targets)

- **Organs (26)**: cerebrum, cerebellum, hippocampus, anamnesis, pineal_gland, backbone, heart,
  viscera, hepar, spleen, paunch, pulmones, genitories, eyes, ears, nose, teeths, tongue, arms,
  hands, legs, feet + beast: snout, fangs, beast_legs, beast_claws.
- **Body regions (8)**: encephalon, visage, trunk, upper_limbs, lower_limbs + beast: muzzle,
  limbs, beast_trunk.
- Canonical id sets are obtained by instantiating all `Organ` / `BodyPart` subclasses via
  reflection (same pattern the registries already use), so the validator never goes stale.

## 2. Code changes

### 2.1 New `Fighting` function

Add `Fighting` to `ModusMentisFunction`. Purely declarative for now — no controller/UI/unlock
logic changes.

### 2.2 Hard-rule validator (throw + exit at every launch)

New file `src/game/narrative/ModusMentisRuleValidator.cs`, static `ValidateOrThrow()`.
It needs both `ModusMentisRegistry` and `FightingSkillRegistry`, so it is **not** called inside
either registry ctor (avoids singleton-init recursion). Instead it is invoked once at app
startup, before any game mode launches (locate the earliest common entry point in
`Program`/startup during implementation; touching `.Instance` on both registries there triggers
their construction first). Errors are collected and reported together in one
`InvalidOperationException` listing every violating MM id — same style as
`ValidateDialogueModiMentis`, which stays as is.

Rules:

| # | Rule |
|---|---|
| R1 | No MM has both Thinking and Action |
| R2 | Every MM has ≥1 of Observation / Thinking / Action |
| R3 | Every MM has ≤3 functions, no duplicates in the array |
| R4 | Semantic memory ⇒ has Thinking; Sensory ⇒ has Observation; Procedural ⇒ has Action |
| R5 | `Organs` is exactly 1 body-region id XOR exactly 2 distinct organ ids; every id resolves to a canonical organ/region |
| R6 | Every organ (26) and every region (8) appears in ≥5 MMs' `Organs` |
| R7 | For each MM, the organ mediums of all skills whose **main** MM it is ⊆ that MM's `Organs` |
| R8 | No fighting skill has more than 2 organ mediums |
| R9 | Both ways: MM has Fighting ⇒ referenced by ≥1 skill (main or secondary); MM referenced by any skill ⇒ has Fighting |

R1+R2+R3 together generate exactly the 18 allowed combos from the draft (base O/T/A/O+T/O+A,
optionally +Speaking and/or +Fighting, capped at 3), so no explicit combo table is needed.

Notes:
- R7 counts **organ** mediums only. Body-part mediums (upper_limbs: seize, chokehold) and
  weapon mediums are exempt — the draft's rule is about organs, and an MM cannot hold both a
  region and organs anyway. Flagged here as a default; easy to extend later.
- Runtime-constructed MMs (ChildhoodMemory, SyntheticItem) are outside the registry and thus
  outside launch validation. A follow-up could validate them at construction; out of scope now.

### 2.3 Audit tool (drives the data work + reports soft rules)

New debug flag `--mm-audit` (following existing `DebugMode`/CLI-flag patterns) that prints:
- per-MM table: id, functions, organs, memory type, moral level, discrete, main/secondary skills;
- all hard-rule violations (same checks as the validator, but listed without throwing);
- per-organ / per-region MM counts vs the min-5 floor;
- soft-rule stats vs targets: % 2-organ vs 1-region, moral distribution, % discrete,
  memory-type distribution.

Soft-rule misses are informational only (no error, per draft). The tool is the feedback loop
for phases B–E and stays afterwards as a content-health check.

## 3. Feasibility math (why some numbers land where they do)

- Coverage floor: 26 organs × 5 = **130 organ slots** → ≥65 two-organ MMs (if pairings spread
  well); 8 regions × 5 = **40 region MMs**. Minimum ~105 MMs, all constraints interlocking.
- Projected pool: ~112 existing + up to 50 new ≈ **162 MMs**.
- **Known soft-rule tension**: 40 region MMs out of ~162 is ~25%, above the 20% target. The
  hard min-5 rule wins; expect to land around **75/25** rather than 80/20. (Exact 80/20 would
  need ~200 MMs, beyond the +50 budget.)
- Memory-type thirds are achievable: Procedural⇒Action and Semantic⇒Thinking are mutually
  exclusive per MM (R1), Sensory⇒Observation is free to combine — balance is set by choosing
  function combos on new MMs and adjusting existing ones.

## 4. Phase B — Fight-data fixes (skill ↔ MM links, Fighting function)

Audit of current main-MM organ footprints shows **one violation**: Ferocity is main for
flesh_tear (fangs+teeths), bite (teeths), scratch/lacerate/gut_ripper (claws) → 3 organs.

- Reassign **scratch, lacerate, gut_ripper** main MM → **Predator** (currently main for
  flesh_clamp, throat_grip — fangs only → becomes fangs+claws = 2 ✓). Ferocity keeps
  flesh_tear + bite → fangs+teeths = 2 ✓. Ferocity can stay secondary on the claws skills, and
  Predator secondary on Ferocity's, so unlock behaviour barely changes.
  (Alternative if Predator's persona fits fangs better: introduce a new claws-flavoured beast MM
  as main; decide during implementation. Draft allows near-duplicate MMs.)
- All other main MMs fit: Pugilatus {hands, legs}, Acrobatics {feet, legs}, Athletics
  {feet, legs}, viscera skills each mono-organ, weapon-only MMs (Swordsmanship, Tactics,
  Battlecraft, Marksman, Deadeye, Incisiveness, Brute Force, Iron Fist, Low Blow, Uppercut,
  Vigilance, Brawling) have ≤1 organ medium.
- R8 already holds (flesh_tear: 2 organ mediums max).
- Add **Fighting** to the function arrays of the 22 fight MMs (fighting.md list — this set
  exactly equals the set referenced by skills, satisfying R9 both ways). Their combos must stay
  legal, e.g. Brawling {Action} → {Action, Fighting}; an Action+Speaking MM becomes
  Action+Speaking+Fighting (3 ✓).
- Consequence of R7: main MMs of viscera skills (Survivalism, Cold Blood, Rage, Blood Lust,
  Iron Nerves) must each carry `viscera` as one of their 2 organs — this alone nearly satisfies
  viscera's min-5.
- Update [fighting.md](fighting.md) skill entries where main MMs changed.

## 5. Phase C — Existing MM data pass (~112 files)

Mechanical loop driven by `--mm-audit`, per MM:

1. **Functions** → nearest legal combo (fix Thinking+Action clashes, add Fighting per Phase B).
2. **MemoryType** → align with functions (R4).
3. **Organs** → reduce/rework to 1 region or 2 organs. Choose to serve global coverage (R6):
   prefer under-covered organs (pineal_gland, anamnesis, spleen, hepar, paunch, genitories,
   backbone, tongue, nose…) when thematically defensible.
4. **MoralLevel / ActsDiscretely** → touch only where it helps soft targets and fits the persona.
5. **Persona/string fields** → untouched unless a function change makes them incoherent
   (e.g. MenuDescription mentioning a dropped capability). Speaking MMs must keep PersonaTone.

Existing MMs are modified, never removed (draft rule).

## 6. Phase D — New MMs (≤50)

Created only to close R6 gaps and tune soft ratios, as plain classes in
`src/game/narrative/modimentis/` with full persona fields in the established style
(SkillMeans, MenuDescription ≤60 words continuous prose, PersonaTone, PersonaReminder,
PersonaReminder2, StyleInstruction, PersonaPrompt in 2nd-person voice — see
[BrawlingModusMentis.cs](../../src/game/narrative/modimentis/BrawlingModusMentis.cs) as template).
They auto-register; no NPC/archetype/story wiring yet.

Biggest gaps are beast anatomy (each needs 5): snout, fangs, beast_claws, beast_legs organs;
muzzle, limbs, beast_trunk regions. Candidate seeds (final list set by audit numbers):

| Gap | Candidate MM concepts (functions / memory) |
|---|---|
| snout (+nose) | Scenting (Obs/Sensory), Spoor-Reading (Obs+Thinking/Semantic), Carrion Sense (Obs/Sensory) |
| fangs (+teeths) | Maw Instinct (Action/Procedural), Worry-the-Bone (Action+Fighting if linked as secondary) |
| beast_claws (+beast_legs) | Raking Reflex (Action/Procedural), Burrowing (Action/Procedural) |
| muzzle region | Muzzle Language (Obs+Speaking/Sensory), Panting Read (Obs/Sensory) |
| beast_trunk region | Beast Posture (Obs/Sensory), Haunch Memory (Action/Procedural) |
| limbs region | Gait Reading (Obs+Thinking/Semantic), Four-Footed Balance (Action/Procedural) |
| pineal_gland, anamnesis, spleen, hepar, paunch, backbone, genitories, tongue, ears… | paired 2-organ MMs (e.g. Premonition: pineal_gland+eyes, Grudgekeeping: spleen+anamnesis, Appetite Lore: paunch+tongue…) |

Near-duplicates of existing MMs (stealth vs discretion style) are allowed when properties
differ. New MMs get Fighting **only** if simultaneously added as a secondary MM on a skill
(R9); default is no Fighting.

## 7. Phase E — Soft-rule tuning & verification

Targets (checked by `--mm-audit`, warnings only):

- ~80% 2-organ / ~20% 1-region → expect **~75/25** (see §3, accepted deviation).
- Morality 20% Low / 60% Medium / 20% High.
- ~10% `ActsDiscretely` (~16 MMs).
- Memory types ~33/33/33.

Verification gate:
1. `dotnet build` clean.
2. `dotnet run -- --mm-audit`: zero hard-rule violations; soft stats within reasonable range.
3. Plain `dotnet run` launch: validator passes silently, game reaches menu; spot-check memory
   menu and a fight (skill unlocks unchanged except the Ferocity→Predator main reassignments).
4. Temporarily corrupt one MM to confirm the launch error + exit path actually fires.

## 8. Execution order & risk notes

A (code: enum, validator, audit tool) → B (fight links + Fighting flags) → C (existing MM pass)
→ D (new MMs until R6 green) → E (soft tuning + verification). A ships first since the audit
tool drives everything after it; hard-rule enforcement can stay disabled (audit-only) until C/D
complete, then flipped on as the last step of D so intermediate commits still launch.

Risks:
- **Validator init order**: cross-registry validation must run after both singletons exist —
  handled by calling from startup, not registry ctors.
- **Menu length**: +50 MMs lengthens FillMemoryMode / memory menus; accepted (no pagination
  work in this refactor).
- **Ferocity reassignment** slightly shifts which MM is "learned in fight" for claws skills;
  gameplay-visible but intended.
- **Body-part mediums exempt from R7** is an interpretation default; revisit if seize/chokehold
  should constrain Brawling's organs.
