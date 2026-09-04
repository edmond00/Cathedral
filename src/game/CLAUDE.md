# Working under `src/game/`

The rules, contracts and gotchas for the game systems themselves. The root `CLAUDE.md` carries the
project-wide rules; this file carries everything that only matters once you are editing game code.

## Saving

**One save, no slots, no manual save.** `%APPDATA%\Cathedral\save.json`, written automatically at the
start of every world-travel phase and at no other point. **New erases it. Death erases it** — all
three causes, in `TriggerDeath`, so a force-quit at the death screen cannot outlive the character.
The main menu's **Continue** does two jobs from one button: resume the session in memory if there is
one, otherwise load from disk.

**Every failure is the same silent refusal**: a missing, corrupt, wrong-version or wrong-seed save
reads as no save, and Continue simply greys out. There is deliberately no migration and no partial
read — `SaveGame.CurrentVersion` confines a save to the build that wrote it, which is what lets
`PartyState.Rebuild` treat an unknown content id as corruption and **fail closed**. A partly-restored
character is worse than a refused load when there is only one save.

### What is stored, and the rule for adding to it

Five things: the master seed, the clock, the avatar's vertex, `Dictionary<int, LocationInstanceState>`
verbatim, and the party. Nothing else feeds a scene build — the world is a pure function of the seed,
a scene of its location id, and the time of day is drawn fresh on every arrival.

**Locations and the party are persisted by opposite means, and that is deliberate.**
`LocationInstanceState` is plain data shared *by reference*: `AttachTo` hands its dictionaries to the
scene, gameplay writes land in the saved object, and there is no save step at all. The party cannot
work that way — it is polymorphic, reference-shared and full of get-only collections — so it is
*captured* into `PartyState`, which carries the same four-rule contract in its doc comment, with rule
three inverted (**captured, not shared**).

**Adding a field to a party type means three lines**: one in `PartyState.Capture`, one in
`PartyState.Rebuild`, and one in `SaveAudit`. The audit is what makes that mandatory —
`dotnet run -- --save-audit` reflects over the party types and fails naming any public member that is
neither persisted nor recorded as derived. It exists because this failure is otherwise *silent*: the
round-trip test compares a capture against itself, so a field captured nowhere matches perfectly.

Three hazards the rebuild is written around, all of which produce quiet wrongness rather than a crash:

- **`ModiMentis` and `LearnedModiMentis` are the same `List` object.** `Rebuild` re-aliases them; two
  lists means every read path silently diverges from every write path.
- **Memory slots hold the *same instances* as `ModiMentis`.** Serialised inline in both places you get
  two objects, and consolidate/archive/reject silently no-op. `save roundtrip` asserts the topology
  separately, because a value comparison cannot see it.
- **Restore order is load-bearing**: organ scores before `InitializeMemory()`, or the modules come out
  the wrong size and every saved slot index lands elsewhere.

And one that reaches past the party, because a field is stored **as-is** rather than captured:

- **An abstract or interface-typed field must declare `[JsonPolymorphic]` and a `[JsonDerivedType]`
  per subclass.** `System.Text.Json` writes such a field by its *declared* type, so the subclass's own
  fields go missing in silence, and it refuses to read one back at all — which
  `SaveFile.Read` catches as corruption, so **one such field makes the entire save unloadable**.
  `RoutineStep.Constraints` was exactly this: every saved routine lost its constraint data, and the
  first player to record a routine lost the save. Only `cli/system/routine_record_replay.cli` reaches
  a routine, so it is the script that carries the `save roundtrip` covering that branch —
  `save_roundtrip.cli` stacks breadth of party state by flags, and no flag records a routine.

### The seed is per run, not per process

The world derives from the master seed, and that seed locks before the window opens — so the save's
seed is **peeked in `Program.cs` before `GameRng.Initialize`**, and a continued run therefore builds
its world on the seed the process already holds. `--seed` still wins outright, which keeps every
scripted run reproducible and means a save from a different seed is simply unloadable.

**A launch that names no seed gets `Config.Rng.BootSeed`, a constant** — not the clock, as it was
while the world was built at startup. Nothing visible hangs off it any more: a run takes its seed
from its moon or from its save, so the boot value governs only the stretch before any run exists,
and there is no reason for it to differ between launches.

**Nothing is generated at startup.** A world belongs to a run, and until New or Continue there is no
run to own one — the launcher sets `GlyphSphereCore.WorldRenderEnabled = false` and the sphere stays
empty and undrawn, so the main menu and the world-selection screen open on nothing but stars. Both
entrances then build one: New through `StartNewRun(worldSeed)`, Continue through `GenerateWorld()` on
the seed already peeked out of the save.

`StartNewRun` is the whole of it — `GameRng.Reseed` (which also clears the streams),
`GlyphSphereCore.RebuildForNewSeed`, `MicroworldInterface.RegenerateWorld` — and it runs for *every*
New, including the first. It used to exempt the first New of a process, because that process had
booted with a world nobody owned yet; that exemption went with the boot-time generation that created
it, and with it a class of bug where the first run and every later one took different paths through
the same code. Note **three** things come off the seed, not one: terrain, per-vertex pathfinding
noise, and per-edge travel jitter. Regenerating only the terrain lays a new world over the old
world's travel costs.

### Which world: the moons

`GameMode.WorldSelection` is where a new run's seed comes from. The screen is the star sphere with
the world not drawn behind it, and the moons in it — the `'O'` glyphs, 383 of them — are clickable.
Each stands for a world: `SkyMoons` maps an ordinal to a name and to `WorldSeed(ordinal)`, and
CONFIRM feeds that seed to `StartNewRun`. CANCEL and Escape both go back to the menu, having spent
nothing — the save is not deleted until `StartNewRun` runs. **A press on empty sky releases the
choice** without leaving the screen: `MoonClicked` fires with -1 rather than not firing, which is the
whole of that rule.

**Hovered and chosen are two states, not one.** They are held at the same time and drawn
differently — chosen is larger and white, hovered smaller and yellow (`SetSelectedMoon` and
`SetHighlightedMoon`, ranked in `RewriteMoon` as blanked > chosen > lit) — so a player can weigh a
second moon against the one they hold without giving it up. The box reads them on separate lines for
the same reason: one line says what CONFIRM will take, the other what the cursor is over.

**The sky is not seeded from the master seed, and must not be.** It is drawn from
`Config.SkyCloud.SkySeed`, a constant, so that the third moon is the same moon with the same name
over the same world in every run on every machine. Seeding it from `GameRng` would mean the moon a
player just clicked no longer existed the moment they clicked it — clicking it is what changes the
master seed. This is one of the two deliberate `new Random(constant)` calls in the codebase.

The moon of the world you are standing in is blanked out of your sky — by `SetHiddenMoon`, which
shrinks its quad to nothing rather than painting it black (black is still a black square, and clear
depends on whatever blend state the sky pass inherits). Confirming a moon hides it; continuing a save
hides whichever moon matches the save's seed, if any does.

**`--seed` skips the screen.** It has already named the world, so New goes straight to
`ProtagonistCreation` — which is what keeps the whole CLI suite working, since every script passes
it. The distinction that makes this safe is `Config.Rng.SeedPinned`: `Program.cs` also writes a
*save's* seed into `Config.Rng.Seed`, and testing that field alone would skip the screen whenever a
save merely existed.

**Reloading re-rolls anything random that follows the save**, because RNG stream *positions* are not
stored. This is a known and accepted save-scum vector, not a bug. The fix, if it is ever wanted, is a
per-stream draw counter in `SaveGame` — cheap to add precisely because `GameRng` owns `_streams`
centrally.

### Testing it

**Saving is off by default under `--cli`.** Autosave fires on every entry to the world map, so
without that rule every scripted run would overwrite the player's real save, hundreds of times per
suite. A script that is actually testing saving passes `--save-path <file>`, which turns it back on
against a file of its own; `--no-save` disables it outright.

| | |
|---|---|
| `save roundtrip` | capture → serialise → rebuild → re-capture → compare, plus the reference-topology check. Fails naming the first field that did not survive. Append it to any script after doing something interesting. |
| `inspect save` / `inspect savefile` | the live run / what is on disk, for `expect-state`. **Not `expect`** — that scans the rendered terminal and never sees them. |
| `save dump` / `save read` / `save write` / `save erase` | print or force, for diagnosis |
| `pause` | opens the main menu. Escape is handled in the launcher's key hook, not `OnKeyDown`, so `key escape` never reaches it and the menu is otherwise unreachable from a script |
| `click end-run` | the death screen's END RUN button |
| `inspect menu` | which menu buttons are enabled — a disabled button differs only by colour, so this is unassertable from the screen |
| `--black-bile` | fills every humor queue with black bile, so the next journey starves. The only death a script cannot otherwise stage (old age uses `--advance-days`, wounds `--start-fight` plus `fight-end death`) |
| `click moon` / `click world` | drive the world-selection screen. A script that wants it must run **without** `--seed`, which skips it — `cli/system/world_selection.cli` does, and is reproducible anyway because the sky is a constant |

`cli/system/save_*.cli` and `cli/system/death_*.cli` cover the lifecycle in-process.
**`tests/save_reload.sh` is the two-launch check** — save, quit, relaunch, Continue — and is
deliberately *not* wired into `run_tests.sh`, which launches the game once per script and so cannot
express a test spanning two processes. Run it by hand before a release.

### Escape, and the phases it must answer

**`LocationTravelGameController.HandleEscape` is the whole rule, and every `GameMode` is listed in
it — including the ones that deliberately do nothing.** The main menu is the only screen carrying an
Exit button, so a phase Escape does not answer is a phase from which the game cannot be quit; and
the gap this replaced was a phase nobody had thought about rather than one somebody had decided
against. Silence is therefore not allowed to mean "no": a mode that should not reach the menu says
so, and says why.

| | Escape |
|---|---|
| Fighting | cancels the armed skill or move target; pauses when nothing is armed |
| LocationInteraction, ChildhoodReminescence, GetUp | closes the thinking popup; otherwise pauses **as an overlay**, leaving narration standing |
| Dialogue, Working, Trading, EncounterPrompt, WorldView | pauses. None of them is cancelled by it — walking out is the footer INTERRUPT / LEAVE / ENGAGE button's job |
| ProtagonistCreation | pauses. Not a running phase, but a screen with no other way off it |
| ProtagonistManagement, Settings | back to the main menu — the same press as their Back button |
| MainMenu | resumes `MenuReturnMode`, or nothing at all before a run has started |
| LLMLoading, Traveling, Death, DialogueDemo | **nothing, on purpose** — see the reasons on each case |

Three things that had to move with it, and each was its own bug:

- **The CLI's `pause` calls `HandleEscape`.** It used to carry a second, simplified copy of the rule
  ("anything that is not the menu opens the menu"), which is a test that cannot fail: `pause` opened
  the menu during childhood reminescence, so a script saw a working pause menu in the one phase
  where a player pressing Escape got nothing. Two copies of a mode list means the CLI is testing a
  paraphrase. `cli/system/pause_early_phases.cli` and `pause_later_phases.cli` walk the phases a
  script can reach.
- **`MenuReturnMode` covers the narrative phases separately.** Exploration, childhood and get-up all
  run on one `_narrativeController` with `_isInNarrativeMode` set, and are otherwise
  indistinguishable — so the flat `LocationInteraction` it used to return would have resumed
  childhood *into exploration's mode*. `_narrativePhase` records which of the three is live. Two
  more phases are derived from the object that draws them: `_protagonistCreationRenderer` and
  `_pendingEncounterNpc`.
- **`OnEnter…` must distinguish resuming from starting.** Every phase whose `OnEnter` builds a scene
  throws the previous one away on a second entry — so resuming childhood from the menu would have
  restarted it from its first reminescence. `ResumeLiveNarrativePhase` is that guard, and it is the
  same one `OnEnterLocationInteraction` has always carried inline.

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

### Opening a fight: the first blow

**`attack` used to be a doorway and nothing else.** It rolled, it printed nothing a player could
read, and the fight began with both sides untouched — so the one thing the verb was *for*, hitting
somebody, happened only after the fight screen had replaced the narration. `FirstBlowOutcome` is the
swing itself, and it is assembled in three steps that each answer a different question.

**What can this body strike with?** `FirstBlow.MediumsFor` gathers fighting mediums, and **an
implement replaces the body rather than adding to it**: a combined weapon is the whole list (a man
who draws a sword is not also kicking), and empty hands offer every organ medium the anatomy owns
plus the body-part mediums (`upper_limbs` — seize, chokehold), each needing a score above zero and no
High-handicap wound. Note it reads the **combined** item, never the equipped one — the weapon on your
belt is not the weapon in the blow unless the player put it there, and choosing to is what the tool
combination is for.

**Which blows does that allow?** Every medium's skill list, minus everything that is not
`FightingSkillEffect.Attack` (viscera is all buffs, legs all movement and guard) and everything this
body could never keep the lesson of. **An unlearned skill is a candidate** — requiring the modus
mentis would mean somebody who has never fought cannot throw a punch, which is backwards: the first
blow is where the punch is learned. What is required is that the lesson could be *held*
(`ModusMentisAnatomy.IsLearnableBy`), so a body is never taught something its anatomy caps at level 1
for ever.

**Then one is drawn uniformly, and it decides three things at once**: the blow that lands, the wound
it leaves (`FightResolver.PickWound` against a pre-rolled hit location, exactly as `SkillAction` does
it — but **armour is not consulted**, since armour buys defence *dice* and there is no roll here), and
**what the verb teaches**. That last one is why `AttackVerb.GrantedModusMentisId` returns null: a
punch teaches Brawling and a chop teaches the axe, so the lesson cannot be declared per verb the way
every other verb declares its. `--verb-audit` names attack in its `TeachesPerBlow` exemption, since a
verb teaching nothing is otherwise exactly the dead content it exists to catch.

Four rules the rest of it depends on:

- **The verb dice are the only dice.** A landed `attack` is an automatic hit; there is no second roll
  against the target's defence. The verb was hard enough to reach.
- **A first blow can never kill.** A target down to its last hit point turns it aside — the lesson is
  still learned and the fight still begins, but no wound is dealt. Without that a corpse would be left
  standing: `NpcEntity.IsAlive` reads the hit points, so a body killed here would be dead with no
  `RemoveNpcFromPlay`, no corpse, and a fight opening against somebody already gone. Killing outright
  is `slay`'s job, at a difficulty of its own.
- **A miss still starts the fight, and costs the initiative.** `FailureReports` returns a
  `FightTriggerOutcome` with `EnemyInitiative`. Attack was the only crime whose victim could not tell
  it had been attempted.
- **The fight waits for CONTINUE.** `_deferredFightOutcome` holds it until the press that closes the
  segment, because `_pendingFightOutcome` is read by the game controller on the very next tick — and
  the chips (what the blow was, what it broke, what it taught) are the whole account of the swing,
  painted over by the fight screen the frame it opens. `state` reports `fight-held=yes`, since on
  screen a held fight is indistinguishable from an ordinary resolved action and a script that presses
  nothing would sit out its timeout.

**Effects outlive the narration.** Twelve attack skills carry a `FightStatusEffect` — Trip knocks
down, Flesh Tear starts bleeding — and a `FightStatusEffect` is meaningless without the `Fighter` it
hangs on, which does not exist until the arena is built. They ride on `NpcEntity.CarriedFightEffects`
and are drained by `FightModeAdapter.ApplyCarriedEffects` *after* the `FightState` is constructed
(`OnApply` logs, and a pushback needs the arena) and cleared as they are applied. Carried on the
individual, so a blow struck at one person cannot land on whoever the fight brings in with them.

**Shallow wildlife takes the same first two steps and neither of the last two**: no anatomy to wound
and no fight to have, so the blow kills and narration carries on. The kill itself is the ordinary
`NpcSlaynOutcome` reported beside it, which is what spawns the carcass.

`cli/verb/attack/` holds all three arms — the landed blow, the miss, and the chicken — and
`cli/outcome/first_blow/` proves the wound is really on the man.

### Winning a fight: what the victory has to hand back

`FightState.CheckFightEnd` is faction arithmetic and nothing more — the last living `Enemy` falling
is `PartyWon`, however many `Party` fighters are standing. **Companions are `FighterFaction.Party`
and `IsPlayerControlled`**, added by `BuildFighters` straight from `Protagonist.CompanionParty`, so a
companion neither prolongs a fight nor is waited on. `cli/system/fight_victory_companion.cli` plays a
kill out blow by blow rather than calling `fight-end victory`, because the forced end sets the result
directly and never evaluates the condition at all.

**What the victory returns *to* is the part that is easy to get wrong, and there are three cases.**
`OnFightCompleted`'s travel branch answers all three, and answering only the middle one is what
produced a reported softlock — the travel progress box left standing over a black screen:

| | after victory |
|---|---|
| encounter **mid-journey** | resume travel — the path is still there to walk |
| encounter on the journey's **final step** | deliver the held arrival (`_pendingArrivalVertex`) and finish the journey |
| **no journey at all** (`--start-fight`) | back to the world view |

The middle case is the trap. `MicroworldInterface.UpdateMovement` raises `ProtagonistSteppedToVertex`
— which is where the encounter roll lives — and `ProtagonistArrivedAtLocation` from the **same call**,
with `_currentPath` nulled between the two. So the arrival is announced while the mode has already
become `EncounterPrompt`, `OnProtagonistArrived` refuses it (it only answers from `Traveling`), and
resuming travel afterwards resumes onto a path that no longer exists: `Traveling` for ever, with only
the pause menu to leave by. The arrival is therefore **held rather than dropped** and delivered once
the encounter settles; the runaway and death branches clear it, because both end the journey.

**There are two ways to die in a fight, and only one of them used to leave a mark.**
`Fighter.IsAlive` is `CurrentHp > 0 && !IsHumorDepleted`, while `NpcEntity.IsAlive` reads the hit
points alone — so a fighter bled out by `BleedingEffect` (which raises `IsHumorDepleted` when the
humor queues go fully critical) was dead for exactly as long as the fight lasted, and alive again at
full health the moment the `Fighter` wrapper was dropped. `CheckFightEnd` counted them dead and
awarded the victory; `OnFightCompleted` then asked `enemy.IsAlive`, was told yes, and skipped them.
No corpse, no `RemoveNpcFromPlay`, no `DepartedNpcs` entry — a fight won against somebody still
standing in the room, whom the next visit rebuilt from the seed regardless.

`FightModeAdapter.SettleEnemyDeaths` carries the fight's verdict back onto the NPCs, once, as the
fight ends, and for **every** result rather than only a victory: an enemy who bled out while the
party fled is no less dead. Marking is one-way, so nothing there can resurrect anybody.

`fight-deplete` is the CLI counterpart of `fight-end` for this, and exists for the same reason: the
real route is a bleed out-lasting a dozen turns of drain against full queues, and which special
effect a blow rolls is not something a script can ask for.
`cli/system/fight_victory_humor_depletion.cli` asserts both halves — a body left, and the man gone —
because spawning the corpse without the removal behind it would satisfy the first alone.

### Death, and where it is reckoned

`TriggerDeath` is the funnel for every cause, and it now **tears down a live narrative session**
before switching mode. That is not tidiness: `Update` enters its narration branch on
`_isInNarrativeMode` rather than on the mode, so a session left standing repaints itself over the
death screen every frame. `ExitNarrativeMode` and `TriggerDeath` share `TearDownNarrativeSession`,
which is everything except the mode change.

**A fight lost is a run lost, wherever it was fought.** The narration branch of `OnFightCompleted`
used to answer a `Death` result with `ExitNarrativeMode()` — so the travel encounter was the only
lethal fight in the game, and a fight picked inside a location could be lost at no cost at all, the
protagonist walking out at zero hit points and playing on.

**Wounds and spent humors are reckoned continuously; age is reckoned on the map.**
`SettleProtagonistDeath` runs on the same tick as the companion sweep and reads `CauseOfDeath`,
taking every arm except `OldAge`. Both of the causes it takes are dealt at arbitrary moments of a
visit and neither had a check that covered those moments:

- a **wound** arrives from a blow or from the penalty a failed act *samples* — no check existed at
  all, so a mortal one left the protagonist playing on at zero health;
- the **humors** are spent by bleeding and by every buff a fight is paid for with, and all three
  starvation checks were on the travel path — so a body whose queues went critical indoors stayed
  upright until the next journey drew heat it did not have. Worse, a protagonist who bled out in a
  fight a companion then *won* walked away alive and at full health, because depletion is recorded on
  the discarded `Fighter` wrapper and nothing outside the fight reads it.

**Age stays where it is** (`CheckOldAgeDeaths`, on the world-view entry, after the healing sweep).
Sweeping it continuously would end the run mid-visit the moment the clock crossed the term — and
since a beast outlives a person, it would also make a companion's death by age unreachable, the
protagonist always falling first. There is little for a standing check to catch anyway: only a work
stint moves the calendar inside a visit. Companions are swept on all three, because their death ends
nothing.

The travel-path starvation checks are left in place and still fire first while a route is walked:
the drain notices the queues empty in the same breath as it empties them.

**One consequence to know.** `--black-bile` sours the queues at the moment the protagonist is
accepted, so that state is now lethal on sight — the run ends on reaching the world map, with no
journey attempted. `death_starvation.cli` used to prove the death happened *between two cells* and
cannot any more; it asserts the new behaviour instead. The leg-by-leg drain therefore has no
dedicated test now, which would need a way to stage a *nearly* critical body.

| | |
|---|---|
| `wound [protagonist\|companions\|<name>] [n]` | the narration counterpart of `fight-wound`. A wound is sampled one slot in five, so a script cannot ask a failed act to injure anybody, let alone three times on one body |
| `starve [protagonist\|companions\|<name>]` | its humoral twin, and why it is a command and not a flag: `--black-bile` can only sour the queues before the world has been touched, so it stages a body that was *always* starving, never one that starves partway through — which is also what lets `death_narration_humors.cli` assert that a save existed and was erased |

`death_narration_wounds.cli`, `death_narration_humors.cli` and `death_narration_fight.cli` are the
three new routes to the death screen, kept alongside `death_wounds.cli` and `death_starvation.cli`
because they reach `TriggerDeath` down different paths — and the difference between the paths *was*
the bug.

### When a companion dies

**Companions could not really die.** `CompanionParty.Remove` was reached by the old-age check and by
the heart-ceiling overflow and by nothing else, so a companion cut down in a fight walked out of it
at zero hit points and stayed in the party for ever — counted against the heart ceiling, handed the
narration by Speak About, turning up in the next fight with nothing left to lose. One bled dry was
not even dead, for the reason above.

**One question, asked in one place.** `PartyMember.CauseOfDeath` returns `Wounds`, `Starvation` or
`OldAge` — or null — and all three are **derived**: hit points are `MaxHp` minus the wound count, a
fully-critical humor set is a state every consumption path already produces, and old age is the clock
against `GetLifetimeDays()`. So a member is dead the moment they are, whenever the question happens
to be asked, with nothing to keep in sync. `LocationTravelGameController.SettleCompanionDeaths` asks
it, and `CheckOldAgeDeaths` now delegates its companion half to the same funnel.

Three things about it that are load-bearing:

- **It is swept every tick, not called at each place a companion can be killed** — because those are
  not a closed set. A fight is the obvious one, but a failed action's wound penalty lands on whoever
  is *acting*, which after a Speak-About hand-off is the companion, and the humors drain on their
  own. Derived state makes the per-tick cost a few property reads, and makes it impossible for
  whatever kills somebody next to forget to call it.
- **Never while a fight is running.** The fight holds `Fighter` wrappers over these members; pulling
  one out of the party mid-swing leaves a fighter on the board belonging to nobody. `Update` gates
  the sweep on the mode, and it fires within a tick of the fight handing back. A dead companion
  therefore does *not* end the fight — `CheckFightEnd` is faction arithmetic and the protagonist is
  still standing.
- **The body is only left when there is a scene to leave it in.** A travel encounter and the old-age
  check both happen on the world map, where there is no area and no observation graph; there the
  notice is the whole of the news, which is what it always was.

`CorpseRegistry.CreateForCompanion` is a second entry point because **a companion is not an
`NpcEntity`** — recruiting moves the `EnemyCombatant` into the party and drops the wrapper, so there
is no `Archetype` to read the species off and no `Combatant` to reach the pack through. Both come
off the `PartyMember` instead, and `CorpsePointOfInterest.NpcEntity` is nullable for exactly this
body. Everything downstream is unchanged, because `cut` and `grab` gate on the PoI's **type**.

**`CompanionDeathBox` is now modal over every mode**, gated at the top of `Update` rather than under
`_currentMode == GameMode.WorldView`. Old age was raised at the world map and nowhere else; a
companion now falls in a narration fight, and narration redraws itself every frame — a box gated
below the mode dispatch would be painted over before it was read. Dismissing it calls
`RedrawCurrentMode`, **not** `OnEnterWorldView`: the box can come up mid-session, and repainting the
map over a scene the player has not left is the bug that swap prevents. `RedrawCurrentMode` shares
`SetMode`'s mode-entry switch (`RunModeEnter`) because `SetMode` early-returns on an unchanged mode,
which is right for a transition and useless for an overlay.

| | |
|---|---|
| `fight-wound [enemies\|companions\|<fighter>]` | wound one fighter to death. **The only way a script can kill a companion** — `fight-end` settles the whole fight and cannot single anybody out, and steering the enemy AI onto one party member for long enough is not something a script can ask for |
| `fight-deplete` | the same, by humors |
| `click companion-death` | the notice's CONTINUE. Modal over every mode, so a script that cannot press it cannot get past a companion dying; `state` reports `companion-death=shown` for the same reason |

`cli/system/companion_death_wounds.cli`, `_humors.cli` and `_age.cli` cover the three causes — one
each, because only the funnel is shared, not how each death is arrived at. The humors script asserts
the notice names the *humors*, since a companion at full hit points reading as unhurt is exactly the
failure. The age script pushes the clock from **inside** a location: the body needs an area to lie
in, and a cat outlives a person (Nighthunter's span runs past 48,000 days against a heart's 43,200
ceiling), so on the world map the protagonist's own old-age check would always fire first.

`--encounter-on-arrival` exists to stage precisely that step — a random roll lands on the last cell
of a path about once in a hundred journeys, which is a bug that reaches players and not tests.
`cli/system/fight_victory_travel_arrival.cli` and `fight_victory_travel_dropin.cli` are the two
travel cases; `fight_victory_narration.cli` is the fourth route out, which returns through
`NarrativeController.OnFightCompleted` instead and is asserted on the **corpse**, since a scene
re-entered with the enemy still in it would pass a mode check and be wrong.

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

**A wound belongs to an anatomy, and every path that puts one on a body must ask first.**
`WoundRegistry.CanBeSufferedBy` is that question — the wound counterpart of
`ModusMentisAnatomy.IsLearnableBy`, matched by **type**, never by `WoundId`, which collides across
anatomies by design. `WoundRegistry.All` is the *human* catalogue; each anatomy factory owns its own
through `GetWoundClassMap`, and a beast has no knee, no fingers and not even a `ScarWound`.

The reason it is a rule and not a nicety is that the failure is silent twice over. A human wound on
a beast **penalises nothing** — every `Affects*` query misses an organ part the anatomy lacks, so it
costs the one hit point any wound costs and reads, if anyone looks, as a lame leg on an animal with
no knees. It then goes into the save verbatim, and `PartyState.Rebuild` resolves wounds against the
body's **own** catalogue and fails closed: one such wound, taken at generation and never noticed,
makes the whole save unloadable a dozen sessions later.

That is not hypothetical. Global personality traits are dealt to every anatomy and eight of the
sixty carry a human wound, so roughly **one beast in four** was given one; taming it and saving cost
the run. `PersonalityTrait.ApplyGameplay` now refuses the wound and logs it, exactly as
`NpcSkillGrant` refuses an unlearnable skill — expected for a global trait, a content fault for an
archetype's own, which `--npc-audit` names. There is deliberately no translation to a beast
equivalent: nothing sensibly maps a missing finger onto a wolf.

### A wound takes a modus mentis away, not just its growth

**A wounded anatomy source contributes a negative amount to a modus mentis's max level.** That is the
whole rule, and it is the one place in the game a derived stat goes below its `WorstValue`:

| the source | contributes |
|---|---|
| a **High**-handicap wound (`GetEffectiveScore` → `int.MinValue`) | **−2** |
| **Medium** wounds that have driven the effective score below 0 | **−1** |
| score 0 — wounded to nothing, or never invested in | +0, then the ordinary linear curve |

Since `GetMaxLevelForModusMentis` is `1 + sum(contributions)` with no clamp, enough damage drags the
ceiling under the base of 1 that every modus mentis starts from.
`PartyMember.GetEffectiveModusMentisLevel` is `min(stored level, that ceiling)`, and
**`IsModusMentisBroken` is that result at or below 0** — nothing left to roll with, so the modus
mentis cannot be used at all.

**Derived, never stored**, for the same reason age and wound healing are: the stored `ModusMentis.Level`
is what the character *learned* and no wound writes to it, so the reach comes back on its own when the
wound closes, with nothing to keep in sync and nothing to migrate in the save.

Before this, a wound was a **growth** penalty and never a **performance** one — the cap gated
`AwardModusMentisXp` and the memory panel's `L4/7`, while `GetTotalModusMentisLevel` summed the stored
levels straight into the dice. A ruined arm cost nothing at the table.

**Offered and refused, not withheld — except for speech.** A broken modus mentis stays in the
observation, thinking and action pools and the refusal is narrated in its own voice, naming the
organs and the wounds behind them. Filtering it out silently would make a player's skills disappear
with no account of where they went. It costs a noetic point, like every other refusal.

| where | what happens |
|---|---|
| **action** | `BrokenModusMentisRule`, **first** in `ActionRulesChecker` — ahead of every circumstantial rule, because a player told a witness is watching will walk to another room and one told their arm is ruined will not |
| **thinking / focus observation / Speak About** | `ApplyModusMentisSelection` is the one place all three player-chosen faculties pass through, so the guard is written once; `RefuseBrokenModusMentisAsync` narrates it |
| **the phase-opening observation** | the persona passes over broken ones *while any usable one remains* — nobody chose it, and there is no popup to be told anything through, so opening on a refusal would spend a phase on a choice the player never made. With **every** observation modus mentis broken the phase opens on the refusal and carries no keyword |
| **dialogue replies** | **dropped** (`GetUsableSpeakingModiMentis`). A reply is written straight into the option list with no narration frame to carry a refusal — it would have to be worded as something the character says, which is the thing they cannot do. Emptying the list falls through to `ZeroRepliesDialogueRule`, which now answers *two* questions: no voice (fluency 0) and no means (every speaking modus mentis broken), with a different sentence for each |
| **fighting** | `FightingSkill.IsBrokenFor` is the modus-mentis sum **below zero**, tested inside `IsUnlocked` so the fight AI is gated by construction. Broken skills leave `GetUnlockedSkills` and appear greyed through `GetUnusableKnownSkills` |
| **emotions** | untouched. A disposition needs no organ to be moved by something, so a broken one still fires |

**Below zero and not at it, for fighting only, and the asymmetry is deliberate.** `GetTotalMmLevel`
returns 0 for a skill the fighter never learned, and that state must stay usable — `FirstBlow` draws
from unlearned skills on purpose, since the first punch is where the punch is learned. Inside a fight
`IsUnlocked` has already required the modus mentis to be known, so a 0 there means "known and worn
down to nothing" and *does* slip through: such a skill rolls `BaseDice` plus its medium and nothing
else. Narration has no such ambiguity and breaks at 0.

Three things that had to move with it:

- **`DerivedStat.GetValue` is now virtual, and only the two max-level contribution stats override
  it.** Every other stat degrades *towards* `WorstValue` and stops, which is right for a quantity a
  body either has or lacks. Do not widen the negative reading — that method is the read path for
  carry weight, beauty, health and noetic points.
- **Absent is not wounded.** `DerivedStat.DiscoverAll()` gives every anatomy every stat, so a human
  really does carry `FangsMaxLevelStat`. It reads +0 because `GetSourceScore` answers 0 (not
  `int.MinValue`) for a missing organ and the linear curve then divides into a `MaxScore` of 0 —
  a *different path* from the wounded one. Route absence through the negative branch and every beast
  organ named by a human's modus mentis starts costing −2.
- **`NpcAudit` needs the same floor of 1 that `NpcContentGenerator.SettleSkillLevels` applies.** An
  NPC generated with a trait scar keeps the skill at level 1 and simply has it broken; without the
  floor, 37 scarred archetypes read as "over its cap of −3".

| | |
|---|---|
| `cripple <mm-id> [who]` | High-wound every source a named modus mentis draws on until it is broken. **The only deterministic way in** — `wound` draws from the catalogue at random, so landing a High wound on one named organ is a lottery. Note it reaches further than it is aimed: a visage wound disables the eyes *and* the nose, so one call breaks every modus mentis built on either |
| `inspect skills` | now reports `effective=`, `max=` and `broken=` beside `level=`. The stored level alone cannot say whether the thing can still be used, and deriving `broken` in a script from two numbers would be a paraphrase of the rule rather than a reading of it |
| `cli/system/broken_mm.cli` | the level falls, the refusal is narrated naming the organs, a noetic point is spent, and `save roundtrip` still passes — the clamp is derived, so nothing about it reaches the file |

### Verbs, actions and outcomes — the three types, and what each is for

The narration loop has exactly three nouns. They were once eight, several of them named "outcome",
which is why the word alone never told you which end of the pipeline you were at.

| type | what it is | when |
|---|---|---|
| `Verb` | the abstract definition — GRAB, MOVE, ATTACK. Hand-registered in `VerbRegistry`, queried before anything exists (`IsPossible`, `DifficultyFor`, `IsIllegal`, `ExpandViews`) | — |
| `VerbAction` | a verb **in context**: verb + verbatim + target + variant. What the thinking phase picks as a goal (`PreselectedOutcome`) and what a click executes | before |
| `Outcome` | a **consequence**: `Apply(OutcomeContext)` changes the world, `Text` is the chip, `Verbatim` is the phrase the narrator gets | after |

Two supporting types, both deliberately thin:

- **`INarratable`** — `DisplayName` + `ToNaturalLanguageString()`. All the outcome narrator needs.
  An interface, not a base class, because three unrelated families implement it and inheriting from a
  shared *base* implied kinship they do not have — a fight's entry payload derived from something
  called `OutcomeBase` purely to be passable to `NarrateOutcomeAsync`, and so read as a consequence
  for years.
- **`NarrativeAnchor`** — a place a click can land, with exactly two subclasses: `ObservationObject`
  (look at it) and `VerbAction` (do it). Both live in the same `SubOutcomes` / `PossibleOutcomes`
  lists because the thinking phase draws from them together: "look closer at that" and "grab the
  flower" are both answers to "what do you want to do?".

**Do not reintroduce a second class for one event.** Every consequence used to exist twice — once as
the chip the player sees, once as a narratable the LLM is told about — written separately and free to
disagree. `WoundOutcome`/`WoundInflictionOutcome`, `HumorOutcome`/`HumorChangeOutcome`,
`FightOutcome`/`FightTriggerOutcome`, `DialogueOutcome`/`DialogueTriggerOutcome`. `Outcome` implements
`INarratable` (`DisplayName => Text`, `ToNaturalLanguageString() => Verbatim`), so one object serves
both roles, and the mode-entry payloads are now the trigger outcomes themselves.

**A conversation's effects are ordinary outcomes too.** `IDialogueOutcome` is gone: its ten
implementers are `Outcome` subclasses whose `Apply` reads `ctx.Npc` and `ctx.PartyMemberId`. The only
thing that had kept them separate was an incompatible `Apply` signature, which `OutcomeContext` now
carries for both. Because such an outcome settles its own wording at apply time (an affinity move has
to phrase "Stranger → Distant Acq.", and only the code making the change sees both sides), **a tree
hands out a fresh set per access** — `SuccessOutcomes => new[] {…}`, expression-bodied, never
`{ get; } = …`. Trees are singletons in `DialogueTreeRegistry`, so an initialised auto-property would
share one mutable outcome object across every conversation in the process.

`Report(text)` settles that wording and sets `Reported`; an outcome that never reports changed nothing
observable, and `ShowInUI` follows. That replaced returning `null` from `Apply`.

### The economy of a narration phase: noetic points and the two ledgers

A phase's budget is **noetic points**, one spent per observation, thinking or item combination, and
the pool refills at the phase boundary (`CloseNarrationSegment`). Three rules decide how far that
budget reaches, and they were designed together — changing one alone re-breaks what the others fix.

- **The pool is the encephalon score divided by three** (`NoeticPointsStat`), **rounded up**. At one
  point per level a segment was never really spent: a player could work through every object in a
  room and still have attempts left, so nothing had to be chosen over anything else. Rounded up
  because flooring lands a starting character on exactly 1, which buys the one thought and nothing
  else — no focus observation, no Speak-About hand-off (that spends the speaker's own point).
- **An accepted tool costs nothing; a refused one costs a point.** Combining is part of the action
  already on screen, and only one item may ever be combined with it. Charging for the *accepted*
  case made the tool-gated verbs (`dig`, `mine`, `fish`, `cut_wood`, `break`) *impossible* once the
  pool shrank: the thinking phase that produces the action spends the point "Use Tool" then wants,
  so the option was offered and permanently greyed out. Charging for the refusal is what pays for
  never greying the row by verb — see "Tools: the three categories" below.
- **A failed action does not refill it.** `CloseNarrationSegment(refillNoetic:)` is false on exactly
  one path — the CONTINUE that closes a segment after a failed action (`_pendingSegmentSucceeded`).
  Refilling there meant a miss cost nothing, since the very next press handed the whole budget back.
  Every other boundary (an area move, a fight, a conversation, a hand-off) refills as before.
- **Nothing is offered twice in one phase.** Two ledgers, same lifetime, cleared together in
  `CloseNarrationSegment`:
  - `ObservationLedger` — what has been *looked at*. Keyed by **instance**, so two same-named things
    stay two things (which is what makes a second corpse reachable). Note `SceneFactory` merges
    same-named PoIs within an area at build, so ordinary scenery never has twins.
  - `ActionProposalLedger` — what has been offered a **button**. Keyed by **verb + target +
    variant**, *not* by instance: `RefreshSceneVerbs` re-expands every scene verb list immediately
    before each thinking request, so the `VerbAction` recorded and the one filtered are never the
    same object. Recorded when the button reaches the screen, so a goal the *action* modus mentis
    refused stays available — that refusal was one mind's, and asking again with another is the move
    that should work.

The compensations for a smaller pool are that last rule plus a **visible decline**: the goal choice
offers "do something else entirely" to the persona itself, not only to the match critic as a hidden
catch-all. With the goal list shrinking request by request, a mind left holding two leftovers it
cares nothing for needs a way to say so instead of committing to one. When the list empties
completely the ignore branch fires with no question asked at all — the same "nothing here worth
doing" line in the thinking modus mentis's voice.

**A player who spends the pool without succeeding can only leave.** Keyword clicks are gated on
`ThinkingAttemptsRemaining > 0`, so at zero the footer LEAVE button is the way out. That is the
intended shape, not an oversight — but it is why LEAVE must stay ungated by anything except a Visual
threat.

### Tools: the three categories, and the four gates

`Verb.ToolUse` is `Excluded` / `Optional` / `Required`, and the test that assigns it is **mechanical,
not aesthetic**: a combined item joins the die chain as its leaf, so the question is "can holding a
thing change how well this goes?". Speech, the senses, thought, waiting and walking on the flat all
answer no structurally — 31 verbs are `Excluded`, 14 `Optional`, 8 `Required`.

**`Required` implies `Handcraft`.** `Verb.EffectiveCapabilities` ORs it in, and `IsPossible` and the
audit's anatomy table both read *that* rather than `RequiredCapabilities`. Without it a beast is
offered `slay`, refused by `RequiredToolRule`, and charged a noetic point for a refusal it could
never have avoided — a withholding dressed as a refusal, which is the whole `ChoiceRulesChecker` /
`ActionRulesChecker` distinction. Read the declared half anywhere and the beast comes out able to
slay; that was the bug the audit fix caught.

**The "Use Tool" row is greyed only when nothing combinable is carried** — never by category. An
excluded verb still accepts the attempt and still fails it, at the cost of a point. That is the
bargain: the player is trusted to choose, and pays for choosing badly. Do not add a category test at
the `hasItems` gate in `HandleClick`.

`ToolCombinationRules.Resolve` is the whole pre-LLM ladder, and **the order is load-bearing**:

| gate | when | costs |
|---|---|---|
| `NoProficiency` | hands band is None | a point, no request made |
| `MadeForIt` | `Verb.UsesFightingMedium` and the item is an `IWeaponItem` | nothing, no request made |
| `NotAWeapon` | `Verb.UsesFightingMedium` and it is not | a point, no request made |
| `MadeForIt` | item is the verb's reference tool, **or** names it in `Item.MadeForVerbIds` | nothing, no request made |
| `NotItsPurpose` | item declares `MadeForVerbIds` and this verb is not among them | a point, no request made |
| `ExcludedVerb` | `ToolUse == Excluded` and the item is not made for it | a point, no request made |
| `AskTheCritic` | everything else | a point **iff** refused |

Proficiency first because a body that can use nothing can also not use the thing it was handed — and
**that one line is the whole of the rule that hands with no craft in them may use no implement at
all, for any verb**: `ToolUsageProficiencyStat` reads `DerivedStat.GetValue`, which returns the worst
value both for a score of 0 and for an organ disabled by a High-handicap wound, and the worst value
is `None`.

A verb struck with a **fighting medium** next, because for `attack` the question is not what the
implement was made for but whether it is a weapon — and the set of things one fights with is closed,
so both directions are settled with no request made. A sword is what attacking is done with whatever
its `MadeForVerbIds` say, and no argument about a lantern's heft makes it a weapon.

The item's own purpose next, and it settles the matter **both ways** before the verb's category is
consulted at all — `MadeForIt` and `NotItsPurpose` are one declaration read in its two directions.

**`ToolUsageProficiencyStat` replaced `ToolUsageCapStat`, and the replacement is the opposite end of
the same organ.** The cap clamped the dice a tool lent; the band decides whether the combination
happens at all. Keeping both taxed hands twice for one act, so **the item now lends its whole
`UsageLevel`**. Bands: 0 → None, 1–2 → Low, 3–4 → Medium, 5–6 → High. What each band clears is
`ToolCombinationRules.Threshold` — `is_the_tool`/`clearly_helps` at Low, `serves_well`/
`plausibly_helps` at Medium, `serves_poorly`/`detoured_use` at High, and anything absent from the
table never clears. **A re-authored critic tree that introduces a choice id fails closed**, which is
why the table is a lookup rather than a switch with a default.

**Five refusals, five neutral sentences** (`ToolFailureKind`). Told only "it did not work", the
acting modus mentis rewrites it as whichever near-miss flatters the character — so the wording
distinguishes the wrong implement, the act that admits none, the implement made for other work, the
hand with no craft in it, and the sound idea beyond an unpractised hand. Only `WrongTool` carries
the critic's own reason, because it is the only one an LLM was asked about.

**`Item.MadeForVerbIds` forbids as much as it permits, and that is the point.** A declaration means
"accepted for these acts without argument, refused for every other, also without argument" — a glass
ground to magnify cannot break ore out of a seam, and asking a critic whether it might is a request
spent to be told what the declaration already said. So it is for **single-purpose** implements only:
`Rope` deliberately declares nothing, because naming the four climbs would forbid the dozen other
things a rope does. When in doubt, leave it empty and let the critic judge.

A verb's `ReferenceToolIds` is the *other* side of the same idea and carries **no** exclusivity — a
knife is what `cut` is done with and is still an ordinary candidate for everything else. Only the
item-side list bars.

It is deliberately **not rendered anywhere**: an implement announcing what it was good for would
turn the phase into a list to be read off. It is also the only road into an excluded verb, which is
why `GetCombinableItems` admits **any** item declaring one regardless of category — reading lenses
are a garment, and are the case the mechanism exists for. `--item-audit` resolves every id, prints
how many verbs each declaration bars, and flags a pairing already implied by `ReferenceToolIds`
(redundant on the accepting side, and *not* harmless on the other).

**A combined implement is never consumed, and there is no longer a question about it.** An LLM
critic used to be asked "was this item used up?" after every successful combination. Since only
tools, weapons and special-use items are combinable and none of those is spent by being used, it
spent a request to answer "no" nearly every time — and the times it said yes were simply wrong, as
when it destroyed the knife a carcass had just been opened with. Under `--playground`, where every
critic choice is drawn at random, it also cost a script combining twice its tool about half the
time. If a consumable ever becomes combinable this comes back as a **property of the item**, not as
a question.

That removal left `ItemConstraint` with no producer, so it changed meaning rather than being
deleted: it is now recorded for **every** combined implement and **requires without spending** it.
That is what it should always have meant — replaying "work the seam for ore" without a pick is not
a routine that can be walked, whether or not the first pick survived. `Consume` is deliberately a
no-op rather than removed (the base class calls it for every constraint), and it leaves the virtual
ledger alone so two steps of one routine can both call for the same knife.

### Emotions: what an outcome does to the person who caused it

An action resolves, its consequences are applied — and then **one disposition the acting body holds
answers them**, renders 1d6 humors into the spleen, and says so in its own voice. That is the whole
system. The three nouns it adds are `ModusMentisFunction.Emotion`, `EmotionTrigger` and
`EmotionOutcome`; everything else is existing machinery reused.

**The match is on the outcome's TYPE, never on its payload**, and that discipline is the design
rather than a shortcut. Asking "was the item *fine*?" or "was the target *weaker than me*?" puts an
open-ended question in front of every consequence in the game, and the answer has to come from an
LLM — one more request per action, for a decision the player never sees. A type is a fact the
compiler already knows, so the whole resolver is type matching and two draws, with no request made.
The cost is real and worth naming: an `ItemAcquisitionOutcome` cannot tell gluttony the item was
bread, so **gluttony is not an emotion modus mentis**. That is the correct answer, not a limitation
to route around.

`EmotionTrigger.WhenSeverity` is the one concession and it reads no payload either —
`Outcome.Severity` is on the base class. It exists for `AffinityIncrementOutcome`, which is one type
carrying two opposite pieces of news (its severity is set from the delta's sign in its own
constructor), so a type-only match would make pride feel affronted by being liked.

Four rules the rest of it assumes:

- **Every modus mentis the body holds is asked, not the one that acted.** Avarice rejoices at coin
  whether or not avarice was the modus mentis that earned it. Keyed to the acting one, the emotion
  would fire in the rare case and stay silent in the common one, which is backwards — so
  `EmotionResolver.Resolve` takes a `PartyMember`, not the action's chain.
- **Exactly one emotion per action**, sampled uniformly from the matches. A wolf slain in a private
  room after a forced door matches five dispositions at once, and five blocks plus five chips for one
  press would bury the action that caused them. Uniformly rather than by level, because a level is
  how well a thing is *done* and nobody feels their strongest feeling most often.
- **The narration is a second request in a different slot**, and that is the point of the block: the
  outcome was written by the ACTION modus mentis and this is written by the EMOTION one. A longer
  first request would be one persona speaking twice.
- **The feeling is named, not implied.** `BodyHumor.FeelsLike` carries one plain first-person
  sentence per mind state, and `NeutralNarration.Emotion` joins it to the outcomes' own verbatims.
  Without it the persona is handed the word "Laetitia" and invents whichever emotion flatters its
  disposition — so the humor that reached the queue and the feeling the player read would disagree.
  `NarrationKind.Emotion`'s instruction insists the stated feeling *survives* the rewrite for the
  same reason.

**`EmotionOutcome` is an `Outcome`, and the word in this codebase has never meant "something a verb
did"** — `StateCaptureOutcome` and `GetUpTransitionOutcome` are pure bookkeeping and are Outcomes
too. What the base class means is "a thing that applies its own change and can show a chip", which is
exactly this. Being one buys three things that would otherwise each need building: the chip renderer
already draws `block.OutcomeReports` and already colours by `Severity` (taken here from the humor's
`VitalHeat` sign, so a humor added later is coloured right without anyone saying so), `ApplyTo` is
already the one door every state change goes through, and `expect-outcome emotion` worked the day it
was written.

**Own block, own CONTINUE, for free.** The coda is one more `LlmPreviewSession` part after the
outcome's, and the preview box already gates one part per press — nothing schedules it. It is
generated ahead while the player reads the outcome, and it is **silent by default**: most actions
move nothing a disposition answers, `Resolve` returns null, no part is begun and no request is made.
Both wirings swallow their own exception, and that is the one narration path in the controller that
does not call `ReportPhaseFailure`: a garnish on an already-resolved action must not become a third
way for a visit to end.

**Two paths, because five outcome types live in only one of them.** `NarrativeController` resolves
from the reports gathered before the dice; `DialogueTreeController` resolves from the tree's outcome
set plus its lessons. Alms, an introduction granted, an enmity cleared, a trade or job opened, and a
conversation that came to nothing are reachable **only** through a tree, so wiring narration alone
would leave pride unable to feel a refused alm. Note what that cost: the dialogue controller now
builds its outcome set, its lessons and its no-consequence fallback **before** the commit closure
rather than inside it — a tree hands out a *fresh* outcome set on every access, so reading it twice
would resolve the emotion against instances the commit never applies.

**R14 and R15** are in `ModusMentisRuleValidator`, fatal like the rest. R14 forbids Action + Emotion,
for the same reason R1 separates Thinking from Action: wanting, doing and being moved are three
offices a mind holds toward one event, and one modus mentis is asked to be exactly one of them.
R15 makes the function and the trigger list imply each other (as R9 does for Fighting and a skill
reference) and rejects a trigger on a humor with no `FeelsLike` — which would narrate an empty
clause.

**R14 cost nothing to satisfy, and that was not the expected answer.** All 22 Action modi mentis that
looked like emotion candidates — rage, blood_lust, sneak_art, clenched_grit, endurance and the rest —
turned out to name a *technique* ("the going on after the reasonable moment to stop") rather than a
feeling, and all 22 were additionally Procedural, so stripping Action would have broken R2 and R4 as
well. **Twinning is the answer here, not conversion**: `exultation` beside ferocity, `obduracy`
beside resolve, `sangfroid` beside cold_blood, `temerity` beside recklessness, `ruination` beside
brute_force, `impatience` against patience. The action pool is unchanged at 127.

**`--mm-audit` prints the coverage table**, and the failure it exists to catch is an absence. An
outcome type no trigger names is fine when nothing feels about it and a fault when a player sees it:
`meet_stranger` — the introduction, which gates every other conversation and is therefore the most
played tree in the game — resolves to a single `AffinityTransitionOutcome`, and nothing named that
type. The most common conversation in the game could stir nobody, the wiring was correct, every test
passed, and the only symptom was silence. That one was found by hand and would not have been found
twice.

**`AffinityTransitionOutcome` is also the clearest illustration of the type-only rule biting.** It
*sets* a level rather than stepping one, and at construction it does not know the level it replaces —
so no severity can be derived and only a **direction-agnostic** disposition may answer it. That is
why gregariousness and misanthropy carry it and pride does not.

| | |
|---|---|
| `inspect humors` | all four queues by composition, newest first. The only assertable proof an emotion landed — the chip says what the player was told, the queue is what moved |
| `--grant-mm <id>` | the starting kit is random, so whether a body *holds* a disposition is otherwise a coin flip. Every emotion test grants one |
| `cli/outcome/emotion/success.cli` | the narration half — pickpocket, avarice, Voluptas into the spleen |
| `cli/system/emotion_dialogue.cli` | the conversation half, on `meet_stranger` for the reason above |

### Corpses

A kill spawns a `CorpsePointOfInterest` in the area it happened in, through
`Scene.AddPointOfInterestToArea` — the one door both routes come through, the `slay`/`attack` verb
applying `NpcSlaynOutcome` and a won fight spawning one body per dead enemy. A human additionally
leaves a plain belongings PoI holding what they carried. Tiny creatures leave nothing.

**A corpse is an ordinary area PoI, not a place you enter.** Its `Items` are the harvestable parts,
and `SyntheticObservationObject` folds a PoI's items into its own sub-outcomes — so one keyword on
the body offers "cut the pelt" and "cut the liver" together, in a single phase. Identical parts
collapse to one goal (`PersonaChoiceSelector` de-dupes by label): a pig with three `Meat` offers one
"cut the meat", and each cut removes one instance while the goal remains.

**A body yields four to eight parts; a tiny creature's `catch` yields two or three.** Below four a
carcass is one cut and a shrug, which does not pay for the kill, the knife and a noetic point per
attempt; above eight the goals run past what a phase can offer and past what the pack can hold, so
the surplus reads as litter. Duplicates count toward the eight and not toward the goals. **A human is
butchered like anything else** — meat, offal, bone, skull, hair — and their belongings are the
separate PoI beside the body, because that is grabbed and this is cut.

**The parts are one general vocabulary, and nothing in it names a species** (`BodyPartItem`, in
`src/game/narrative/items/corpse/BodyPartItems.cs`). A wolf, a cow and a man all give up `Meat` and
`Bone`. What makes a bear a bear is *which* parts and *how many* — a skull and two claws — never a
"Bear Meat" beside the `Meat` everything else gives. That middle case is what this replaced: there
was an `AnimalHide` and a `DeerHide` and a `GoatHide`, a `Feather` and a `ChickenFeather` and an
`EagleFeather`, so which item a carcass gave depended on which convention its archetype was written
under. Where a real distinction exists it is a **word**, not a prefix: `Hide`/`Pelt`/`Skin` are three
grades of one material, and so are `Fang`/`Tooth`, `Horn`/`Antler`, `Feather`/`Plume`. When adding an
animal, compose from what is there; add a part only when it is a thing no existing word covers.

The verb split needs no per-item reasoning: everything in a `CorpsePointOfInterest` is flesh, so
`cut` takes it and `grab`/`steal` refuse it (`ItemPickup.FindHoldingPoI` filters the type, and
`StealVerb` repeats the filter); everything in the belongings PoI beside it is cloth and steel, so
the pickup verbs take it and `cut` — which requires a `CorpsePointOfInterest` — does not.

Two rules the rest of it depends on:

- **A spawned PoI is not yet observable.** The narration graph is built once, from the areas as the
  factory left them, so anything the game spawns during play has no observation object — and an
  object with no observation object cannot be looked at or acted on, however correct it is in
  `area.PointsOfInterest`. `NarrativeController.SyncSpawnedObservations` reconciles the two before
  every observation phase and every thinking request (it runs from `RefreshSceneVerbs`). That sync is
  why a corpse is reachable at all; without it the body existed and the player was never told.
- **A corpse opens the next narration phase, alone.** `PendingCorpseObservations` collects what fell,
  `GenerateObservationsAsync` drains it, and `GenerateCorpseObservationAsync` observes every body in
  the order it fell — first plainly, each later one through a transition — with no persona choice and
  no length gate, the way the post-dialogue opener imposes the person just spoken to. It runs *ahead*
  of the threat opener: a corpse is a one-shot event consumed on the spot, while an enemy still
  standing leads the phase after this one anyway. The list is drained whether or not it is used, so a
  body left in an area the player has since walked out of cannot open a phase two moves later.

Two bodies in one room are two same-named PoIs, deliberately unmerged. They behave like any other
pair of identical objects — the observation choice list keeps one representative per phase and the
ledger retires instances, so the second becomes observable once the first has been seen. This is the
one place `Scene` does *not* follow `SceneFactory`, which merges same-named PoIs at build time.

Historical note: a corpse used to be an enterable `Spot` holding one PoI per body part, and that was
the only content the `Spot`/`PoV.InSpot` axis ever had — no factory built one. The axis is gone.
Nothing in `design/` ever planned other spot content; the `# spots` sections in the location design
docs use the word in its pre-`c3fef5a` sense, meaning what is now a `PointOfInterest`.

### Crime: what makes an act illegal, and what it costs

**Legality is contextual, and `Verb.IsIllegal(scene, pov, target, actor)` is the only place it is
decided.** Sealed, like `IsPossible` and for the same reason — the setting test (standing in a private
area makes *anything* trespass) applies to every verb at once, and three call sites could each forget
it. The verb's own half is `protected IsIllegalFor`. What that buys:

- `attack` / `slay` against somebody who **already counts you an enemy** is not a crime — the quarrel
  was declared before the blow. `murder` (on a sleeper) stays unconditional: a sleeper has declared
  nothing;
- `unlock_door`, `slip_into` and `break` ask `PrivacyModel.ReachesPrivateArea(scene, target)`, **not
  `pov.Where`**. A house door is listed in the street's PoIs *and* the room's, so reading the actor's
  own area would call a burglary from the street lawful. A public storehouse's lock is nobody's
  privacy.

`PrivacyModel` answers "does this object reach into somebody's private space?" — from a connector's
own two endpoints where it has them, else from whichever areas list the PoI.

**Two rule families, and the split matters.** `ActionRulesChecker` (`IActionRule`) judges an action
already chosen: it *rejects*, the player is told why in the modus mentis's voice, and it costs a
noetic point. `ChoiceRulesChecker` (`IGoalChoiceRule`, `IWillingnessRule`) narrows what is *offered*:
it *withholds*, silently, and nothing is spent because nothing was refused. Character belongs in the
choice rules ("that never occurred to me"); circumstance belongs in the action rules ("that man is
watching"). Both are ordered lists of tiny classes — a new rule is one file and one line.

Morality now has four distinct behaviours, two per side of the chain:

| | thinking MM | action MM |
|---|---|---|
| **High** | crimes are dropped from its goal list (`HighMoralityAvoidsCrimeRule`) — filtering everything away is a legitimate result and reads as ignore | refuses outright (`IllegalActionHighMoralityRule`) |
| **Low** | if any goal on offer is a crime, **only** the crimes are offered (`LowMoralityPrefersCrimeRule`) | cannot refuse — the decline option is withheld, leaving eager/willing/reluctant (`LowMoralityNeverRefusesCrimeRule`) |

Both sides are needed because the goal and the means are two separate LLM decisions: a scrupulous
skill can still be picked to carry out a goal some other modus mentis chose. Note `MoralLevel` is read
**only** off these two — R13 makes a non-Medium value on anything else fatal.

**Getting caught is deterministic, and it is two separate questions.** Keeping them apart is what
makes discreteness mean something:

1. **May I act at all?** Asked by the coded rules against **raw** proximity. Somebody in the room
   blocks a non-discrete MM outright. A **discrete** MM may always attempt — that permission is the
   whole of what `ActsDiscretely` buys at close range (and `CanBeUsedUnderThreat` verbs are likewise
   never blocked).
2. **What does failing cost?** Asked against **effective** proximity — `ProximityModel`, where
   discreteness applies. It silences you *through a wall*, never *in the same room*: `Audio → None`,
   `Visual → Visual`.

Three rungs of one ladder, identical for witnesses and enemies except in what the top rung is:

| effective | witness | enemy |
|---|---|---|
| **Visual** | caught red-handed → the tree | they attack, on their initiative |
| **Audio** | **they come to look** — `Scene.DrawNpcTo` moves them into your area | same |
| **None** | nothing | nothing |

**The Audio rung is an approach, not a verdict.** Nothing is confronted yet; what it costs is that
they are now standing in the room, which closes the free exit and makes the *next* slip a caught one.
That needs real state, because position is resolved from `NpcSchedules` and
`SceneNpcPlacement.PlaceForPeriod` re-derives every NPC observation object from it on **every**
segment — so somebody moved any other way is put straight back one phase later, exactly as a tamed
beast's stale observation object is. `Scene.DisplacedNpcs` is consulted inside `GetNpcsAt` and
`GetAreaOf` instead: one override in one lookup, and the placement, the verb gates, both selectors and
the exit button follow without knowing it exists. A displaced NPC also reads as going nowhere all day,
so `stalk` correctly declines to follow them. One visit's business — what outlasts it is whatever the
confrontation turned into.

An arrival opens the next observation phase, **ahead of the corpse opener** (both are one-shot events,
but an arrival is the newest and has just changed what is safe to do), and only if they are still in
the player's area — announcing somebody who came to a room nobody is in reads as materialising.

A catch opens `CaughtRedHandedTreeFactory`'s tree, and **its failure outcome is always a fight** — the
witness's nerve used to be a per-archetype `IsBrave` flag whose timid branch wrote the same affinity
level the success branch did, so against a third of the game losing that conversation cost exactly
nothing. `IsBrave` is gone; `AuthorityLevel > 0` now carries the two things it also did (who joins a
fight as an ally, and the fight AI's aggression).

The consequence **lasts**, because `NpcEnemies` persists (see "What survives a visit"). Walking out
of the fight ends the fight, not the enmity: come back and they are still a Visual threat, the footer
button is RUNAWAY, and any failed action under threat starts it again.

**Only a Visual presence gates LEAVE.** `ComputeExitContext` tests `== Visual` on both branches, so
neither the Audio tier nor discreteness touches the footer button — the exit is a property of who is
in the room with you. The witness branch additionally needs a *reason*: either the area is private, or
**they came in here after you** (`DisplacedNpcs`). Without that second condition the three crimes that
happen in the open — `pickpocket`, `stalk`, `attack` — would draw a witness across a public square who
then watched you stroll off.

There is deliberately **no criminal record**. One existed — written, escalated and cleared, with not a
single reader anywhere — and `CriminalAffinityType` survives only to word the confrontation.

**Earshot is not the area graph.** `SceneAdjacency.WithinEarshot` is the shared definition used by
both `WitnessSelector` and `ThreatSelector` (which each had a private copy of it). It walks graph
edges in both directions *and* the far side of every connector — because a gate connector carries no
`AreaGraph` edge by design, and reading the graph alone made two rooms joined by a door non-adjacent.
That erased the whole Audio tier indoors: of 3618 sampled private-area situations, **every** witness
was Visual (blocked outright) and **not one** was Audio, so the confrontation could not happen inside
a building at all. A door is a gate, not a wall. `--crime-audit` prints the tier distribution and
fails if Audio is ever empty again.

### Landscapes: the one way to move without walking

A `LandscapePointOfInterest` is **a road you can see but cannot yet be on**. It hangs in an area you
had to climb to, points at another area of the same location, and `voyage_toward` walks it. It is a
`ConnectorPointOfInterest` like a door or a stair, with two differences:

- **listed at the viewpoint only** (`AttachToViewpoint`, not `AttachTo`), because a road visible from
  a rooftop is not visible from the street. Travel is therefore **one-way**: you come back by the
  ordinary graph;
- **`AllowsGraphEdge` is true**, unlike every other connector. Those are *gates* — crossing them is
  meant to cost a roll, so a graph edge beside one silently makes them decorative, which is what the
  building audit checks for. A landscape gates nothing; it is a shortcut earned by the climb, and the
  destination is expected to be walkable by road as well.

Observing one offers the journey. There is **no looking step**: this replaced a pair of verbs where
`observe_horizon` searched a horizon to record what could be seen and `go_toward` walked to it. What
that extra roll bought was a set of area ids written onto the `PoV` — and `RefreshSceneVerbs` builds a
*throwaway* `PoV` to re-gate verbs against, so the reveal worked, the knowledge was written, and the
verb that depended on it read an empty set every time. `go_toward` was unreachable in the shipped
game. Searching a scene for what is worth looking at is the narration system's job and it already
does it; a landscape is an object in an area, refreshed like any other, with no second copy to
disagree with.

**Placement is manual, per factory** — there is no automatic pass. A view over a location is a claim
about that location, and the one thing automatic placement reliably produced was a cave entrance
reporting that "the country opens out below". Today: a mountain's and a peak's topmost area, a
forest's giant-tree crowns, and **every building's roof** (reached by scaling its outside wall, seeing
every other outside area — roofs do not see other roofs). Caves have none by design. Climbable areas
that had nothing to offer once climbed were deleted rather than given a view.

Examining a landscape teaches `topographia`; contemplating one teaches `aesthetic`; the voyage itself
teaches `voyage`.

### Recruiting: taming a beast, and the two-step it belongs to

Both routes into the party — `tame` for a beast, the `propose_to_join` dialogue for a person — end in
the same three writes: the NPC's `EnemyCombatant` is added to `Protagonist.CompanionParty` (nothing
is copied: an `NpcEntity` wraps the combatant, which *is* a `PartyMember`), `NpcEntity.IsAlive` is set
false so `Scene.GetNpcsAt` stops returning them, and the `SceneNpc` is dropped from `scene.Npcs` and
`scene.NpcSchedules`. `RecruitedOutcome` does it for the verb, `RecruitFromDialogue` for the tree; the
ceiling (`max_companions`, heart-derived) is re-checked in both, because a companion can be picked up
between the offer and the roll and quietly exceeding the cap is worse than declining.

Two things make the removal actually show:

- **`tame` requires an appeased beast, not merely a non-hostile one.** `AffinityTable.IsAppeased` is
  "not an enemy *and* at Suspicious" — which only `appease` and the reconcile tree produce. So a wild
  beast takes two rolls at two separate phases, and `tame` is not even offered until the first lands.
  A beast that was never hostile is not tameable at all: it sits at Stranger, which `appease` (enemy
  or AnnoyingAcquaintance only) will not touch either.
- **The observation object goes with the NPC at the next segment boundary, not at the outcome.**
  `SceneNpcPlacement` owns one `SyntheticNpcObservationObject` per scene NPC and re-places them all
  in `PlaceForPeriod`, which runs from `ApplyTimePeriod` — and `StartObservationPhaseWithHistory`
  re-applies the current period precisely so every new segment re-places NPCs. Within the segment
  that tamed it the stale object is still in the node; it is harmless (every verb gate reads
  `GetNpcsAt`, so only IGNORE survives the refresh, and the segment is over anyway), and the phase
  after it is gone. `cli/tame_beast.cli` is the regression script — `state` drops one observable
  across the tame, and `--observe-only` on the beast reports nothing matching afterwards.

### What survives a visit

A scene is **rebuilt from its factory on every arrival** and thrown away on leaving, and the build is
a pure function of the location id (`CreateSeededRandom(locationId)`) — so the same rooms, the same
people and the same animals come back *identical*, down to names, stats and belongings. Anything the
player changed therefore has to be recorded in `LocationInstanceState` or it did not happen.

That class is the whole of a location's permanent memory, and a save file will read and write exactly
its fields. Four rules govern anything added to it:

- **plain data only** — dictionaries, sets and lists of primitives or enums, so it round-trips
  through `ToJson` with no custom converter. A `Guid` is re-minted by the next build and would point
  at nothing;
- **keyed by a stable id** — `INpcEntity.PersistentId` for a person or an animal (the name-derived
  `{archetypeId}_{name}`, *not* `NpcId`, which carries a random number for anyone non-persistent),
  `ItemElement.DepletionKey` for a picked slot;
- **mutable in place, shared by reference** — `LocationInstanceState.AttachTo(scene)` hands the live
  collections to the scene, so gameplay writes land in the saved state with no save step. This is why
  it is a class and not a record: a `with`-copy would silently detach a store the scene still writes;
- **wired in one place** — a new fact is one property plus one line in `AttachTo`.

Four facts live there today: `NpcAffinity`, `NpcEnemies`, `ItemDepletions`, and `DepartedNpcs` —
everyone this location has lost for good. Corpses, wounds dealt and forced doors are deliberately
*not* there: they are one visit's business.

`NpcEnemies` is separate from `NpcAffinity` because enmity is not a point on the affinity ladder —
an enemy has an affinity level as well, and the two move independently (the reconcile tree clears the
flag and leaves you `Suspicious`). Both are handed to the same `AffinityTable` by `AffinityFor`, which
shares *both* backing stores. Sharing only one is what made running away the dominant answer to every
fight: the scene was rebuilt on arrival, the grudge was not rebuilt with it, and the man who drew
steel on you last visit greeted you as a mild acquaintance.

**Every departure goes through `Scene.RemoveNpcFromPlay`** — the slay/murder verbs, a won fight,
`TameVerb`'s `RecruitedOutcome` and the `propose_to_join` tree. It does the three writes that make
someone gone from this visit (not alive, out of `Npcs`, out of `NpcSchedules`) and the fourth that
makes them gone from every later one (the id into `DepartedNpcs`). `SceneFactory.Build` drops those
ids straight after `BuildNpcs` and before `PlaceBeastSign`, so a departed beast leaves no tracks to
follow to nothing. Departure is **permanent**: a replacement drawn from the same seed would be the
very individual that left, so the spot in the world stays empty.

**A scene factory is built per scene, never reused.** Each one keeps working state while it builds —
the area list it wires paths through, a village's workshops and houses — and none of it is meant to
outlive the scene. `_sceneFactories` therefore holds `Func<SceneFactory>`, not instances. Sharing one
instance let that state pile up: the second build of a plain ran its path wiring and its beast
placement over eight areas, four belonging to a scene that no longer existed, and since the two
builds produce identically-named areas the corruption was invisible in the log. The audits never saw
it because they always built one factory per location — which is now what the game does too, so a
green audit finally covers the path the game takes.

### What a body can do

A companion may be a beast, and after a Speak-About hand-off a beast **narrates**: it observes,
thinks, chooses goals and acts. Two things therefore decide what a party member may learn and do, and
they are decided separately:

- **Organs — free, no authoring.** Every id in a modus mentis's `Organs` must resolve on the body.
  A human cannot learn a `fangs` skill; a beast cannot learn a `hands` one. Note the ids are shared
  where the anatomy is: `LegsOrgan` and `BeastLegsOrgan` both answer `"legs"`, `BeastClawsOrgan`
  answers `"claws"`, and both trunks answer `"trunk"` — so anything reasoning about this by class
  name is wrong. `ModusMentisAnatomy.SourcesOf(anatomy)` is the list, built from the anatomy factory.
- **Capabilities — authored.** A wolf owns a tongue and a cerebrum, so nothing structural keeps
  rhetoric or lockpicking away from it. `AnatomyCapability` is three flags — **Speech** (conversation,
  not voice), **Handcraft** (tools, locks, carrying, fine work), **Abstraction** (letters, number,
  institutions) — declared **per anatomy** on `IAnatomyFactory` (Human has all three; Beast has none)
  and **required** per piece of content (`ModusMentis.RequiredCapabilities`,
  `Verb.RequiredCapabilities`, both defaulting to `None`).

The direction matters: content declares what it *needs*, anatomies declare what they *have*. Adding
an anatomy is one line on its factory — no revisiting 180 modi mentis and 54 verbs.

Three consequences worth knowing:

- **`Verb.IsPossible` is sealed**; the per-verb condition moved to `protected IsPossibleFor`. The
  capability test lives in the sealed half so the routine replay engine, the verb audit and the debug
  window cannot each forget it. The whole `DialogueVerb` family declares `Speech` once, `ExtractionVerb`
  declares `Handcraft` once.
- **The gate reads the acting member, not the protagonist.** `RefreshSceneVerbs` passes
  `_activePartyMember`, and the actor parameter widened from `Protagonist?` to `PartyMember?`
  throughout (`Scene.View`, `VerbRegistry.GetApplicable`, `IVerbRefreshable.RefreshVerbs`). The two
  places that genuinely need the protagonist — the `max_companions` ceiling in `TameVerb` and
  `propose_to_join` — do `actor as Protagonist`. `RoutineReplayEngine` used to pass
  `ActingMember as Protagonist`, i.e. **null** for every companion, skipping every actor-dependent gate.
- **Learning is refused, not capped.** An MM naming an absent organ contributes +0 to the level cap,
  so it used to be grantable and stuck at level 1 — held, useless, unexplained. Every grant path now
  asks `ModusMentisAnatomy.IsLearnableBy` first: `NpcContentGenerator` filters the three pools *before*
  sampling (so an archetype still gets the count it asked for), `NpcSkillGrant` refuses, and
  `ModusMentisGrantOutcome` teaches nothing when the acting body cannot hold the lesson. A global
  personality trait offering a skill the drawn anatomy lacks is normal and logged; an *archetype's own*
  trait doing it is a content fault, and `--npc-audit` names it.

`cli/beast_verbs.cli` is the regression: the same flower patch, looked at by the protagonist (offered
"gather a daisy") and then, after the hand-off, by the cat (`goal gather` finds no such goal, and the
phase runs on "examine the flower patch closely" instead). `--verb-audit` prints what each anatomy is
barred from — 25 of 54 verbs for a beast.

### A verb teaches per body, not per verb

**A verb's lesson is a candidate list, not one id.** `Verb.GrantedModusMentisIds(target)` returns them
**most restrictive first**, and `ResolveGrantedModusMentisId(target, actor)` walks it — the target's
own override first, then the candidates — and takes the first one *this body can hold*. A null actor
(audits, tooling) takes the first without asking.

This exists because ten verbs taught the protagonist **nothing at all**. Climbing, scaling, stairs,
crossing, smelling, digging, tracking and catching are done by both anatomies and are not the same
lesson for both: a beast clambers with claws where a person scales with arms, and neither modus mentis
names an organ the other owns. With one id per verb, `ModusMentisGrantOutcome.For` refused the lesson
and wrote a log line — so `smell` had 86 authored declarations of `scenting`, a **snout** skill, and
every one of them taught a human nothing.

**Order beast before human.** A beast's lesson names a beast organ, so a human cannot hold it and
falls through cleanly. The reverse is not true — `spoor_eye` names eyes and anamnesis, which a beast
also owns — so putting the human lesson first silently takes the beast's own lesson away from it.

The pairs today: clambering/`scaling`, surefoot/`balance`, scenting/`keen_nose`, digging/`spadework`,
spoor_reading/`spoor_eye`, soft_mouth/`snatch`. `cross` puts the crossing's own lesson ahead of both.

**`--verb-audit` now asks this per anatomy** and fails naming any verb whose whole candidate list is
unlearnable by a body that could be offered it. The beast side of that fault is real and not yet
fixed: `VerbAudit.NoBeastCounterpart` lists the sixteen verbs a beast companion can be offered and
learn nothing from, with the reason. **Empty that list, do not extend it.**

### The senses reach living things

`SensoryVerb` used to gate on `target is PointOfInterest`, so the four senses could not touch anything
alive: you could listen to a tree and not to the lark in it, and the only verbs a bird accepted were
the ones that killed it. Three parts now:

- **`NpcArchetype.Senses`** — the same `SensoryProfile` a PoI carries. Default `Examinable`; a person
  (`NamedNpcArchetype`) rewards all four, birds rewards all four, tiny creatures examine and
  contemplate, and the two that sing (cricket, bee) add listening;
- **`NpcArchetype.VerbModiMentis`** plus `SceneNpc : IVerbModusMentisSource` — the NPC half of the
  override mechanism `PointOfInterest` has always had, declared per *kind* rather than per individual.
  A person teaches `physiognomy` and `hearkening`; every non-human teaches the naturalist's set
  (`creature_lore`, `musk_reading`, `fellow_feeling`) and a bird adds `birdsong`. Note this is keyed
  on **species, not class** — `NamedNpcArchetype` carries every wolf and bear as well as every baker;
- **a sleeper is excluded** from the NPC branch. `SceneNpcPlacement` swaps them and their bed for one
  merged `SleepingNpcPointOfInterest`, which has its own senses and its own lessons; offering both
  would be two routes to one act, differing only in which object the phase opened on.

### The affinity ladder, and what gates a conversation

`AffinityLevel` is an ordinary 0–5 ladder (Stranger → Close Friend) whose value is the bonus dice a
conversation gets — **plus `Suspicious` at 6, which sits off the ladder and grants 0**. Three
consequences the arithmetic will get wrong if you let it:

- **`Adjust(delta)` is a named step, never `level ± 1`.** A step down from Stranger clamps to the
  `min` (Annoying Acquaintance), and a step either way from Suspicious would land on nonsense — so
  Suspicious names both its neighbours explicitly (up = Distant Acquaintance, down = Annoying
  Acquaintance) and **`delta == 0` returns without touching the table at all**. That last one was a
  real bug with a long tail: `AffinityIncrementOutcome(0)` means "this conversation left things where
  they were", read as a step *down*, and filed the player as an irritant on first meeting — which
  then offered `reconcile` against somebody there had never been a quarrel with.
- **Suspicious must be escapable.** Refusing to move it made it a permanent dead end: an NPC talked
  down out of hostility could never be improved again, so every later conversation with them changed
  nothing.
- **A won conversation must not leave the player worse off.** `reconcile`'s success drops an *enemy*
  to Suspicious, which is right; imposing it on somebody who merely found you annoying is a
  downgrade in dice dressed as a win, so `SuspiciousAffinityOutcome(onlyWhenHostile: true)` steps a
  non-enemy up instead. It runs **before** `ClearEnemyOutcome` in the tree's outcome list — the flag
  it reads is the one that outcome removes.

**The introduction is the gate for conversation.** `meet_stranger` is what turns a stranger into a
distant acquaintance, and everything else sits behind it: trade and work through `TradeGate`,
`strengthen_relationship` and `gather_knowledge` through their own stranger check,
`introduce_me` because asking somebody who does not know you to vouch for you cannot work in that
order. `SocialDialogueVerb.RequiresAcquaintance` is the switch; **`beg_for_coin` and `provoke` opt
out**, because each is *about* addressing somebody who does not know you — behind an introduction,
begging would leave a destitute player with nothing to say to anyone, and provoking would be
unreachable against exactly the people worth using it on.

A verb that joins this set must also be named in `VerbAudit`'s `unreachable` table and
`VerbProbe.WhyUnreached`: the sweeps build scenes with no instance state, so their stand-in actor is
a stranger to everyone and an affinity-gated verb is *correctly* never offered.

### A lesson from the circumstances, not only from the act

`CircumstanceGrants` answers a different question from the verb's own grant: not "what did I just
practise?" but **"what did doing it under these conditions teach me?"** — and the conditions vary far
more than the targets do. Forcing a door is `forcing` whoever does it; forcing a door with an enemy
watching, at a difficulty well past your dice, while already wounded, is three different lessons and
none of them is about doors.

Three rules make it safe to have at all:

- **It is additional, never a replacement.** The practical lesson still lands and this is a second
  chip. Displacing the verb's grant would cost the player the craft knowledge they were going for,
  which would make every disposition read as a punishment.
- **At most one per action**, first match wins, so the ordering *is* the design: the body's condition
  outranks being watched, being watched outranks the difficulty, and the plain per-verb table at the
  bottom is what fires on an ordinary day.
- **Anatomy is not consulted here.** `ModusMentisGrantOutcome.For` refuses a lesson the body cannot
  hold and logs it, exactly as it does for a verb's own grant.

Three layers, in order: **conditional rules** (wounded, watched, illegal, private, night, at
difficulty 5), then **target rules** matched on the object's *name* — because the distinctions that
matter are content ones and not code ones, and one `DiggableGroundPointOfInterest` covers peat, a
garden bed and a grave — then the **per-verb table**, which is simply the other half of what each act
is. A miss at any layer falls through, so an unnamed object is never worse off.

The rules are `record Rule(string Id, Func<Circumstance,bool> When)` rather than lambdas returning
ids, so **the ids are data**: `AllGrantedIds` enumerates what the class can teach without executing
anything, which is what lets `--mm-reach-csv` count this as a route instead of guessing at it.

### A conversation's lesson depends on the branch and the person

`DialogueTree.AdditionalGrantedModusMentisIds(npc, resolution)` had one implementer and now has
thirteen. Begging a reeve teaches `humility` where begging a farmhand teaches `weeping`; a provocation
aimed at authority teaches `insolence` where one aimed at a peer teaches `boasting`.

**Mind the difficulty ladder when gating on it.** `BranchDifficulty.Hard` only reaches 3 at depth 4,
so a `Difficulty >= 3` gate inside a tree whose branches are two deep is a branch that never happens
— which is exactly how `insolence`, `dramaturgy` and `open_mindedness` were written, wired and
unreachable. `--dialogue-audit` prints each tree's branch lengths; read them before choosing a gate,
or key on the person instead, which every tree can answer.

**The caught-red-handed trees are built by a factory and never registered**, so anything reading
`DialogueTreeRegistry.All` misses them — and they are the trees a thief meets most. `MmReachAudit.AllTrees`
adds them back explicitly.

