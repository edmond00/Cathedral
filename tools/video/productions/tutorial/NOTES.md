# The tutorial run: notes

`tutorial.cli` plays the whole tutorial scenario (BRIEF.md) by itself under `--record`, from the main
menu to a calm closing frame. This file is what it depends on: the flags, what each section relies
on, the code changes made on this branch for it, and every fragility known.

## Launch

```bash
tools/video/.venv/Scripts/python tools/video/make_video.py record \
    tools/video/productions/tutorial/tutorial.cli build/rec_tutorial --timeout 14400 --flags \
    "--childhood-path \"sobs,waves,sharp,golden arch,shelter\" --force-encounter wolf,wolf --no-encounters --npc-static --start-area \"Longhouse Hall,Cobbler's Shop\" --observe-only farmer,cobbler --npc-affinity DistantAcquaintance --grant-mm diligence,provisioning,swordsmanship:2 --job-only farmhand --humors all=voluptas"
```

No `--seed` (it would skip the world-selection screen: the world is chosen by clicking its moon,
which reseeds the run), no `--skip-childhood`, no `--playground`, no `--organs` (see below).
`--timeout` must be long: on a CPU the run takes TODO.

| flag | why |
|---|---|
| `--childhood-path "sobs,waves,sharp,golden arch,shelter"` | **new.** Each childhood reminescence observes only the fragment one of these names matches, so the kit is fixed (see "Childhood"). |
| `--force-encounter wolf,wolf` | **new.** Exactly one encounter per journey, at its middle step: a wolf on the road to the farm, another on the road to the city. Also seeds the encounter's arena from the run (it was the wall clock). |
| `--no-encounters` | no random encounter besides those two. |
| `--start-area "Longhouse Hall,Cobbler's Shop"` | the farm visit opens in the farmer's hall, the city visit in the cobbler's shop (a list is **new**: the first name the location has wins). |
| `--npc-static` | everyone stays in the area they spend most of the day in, so the farmer is in her hall and the cobbler in his shop at whatever hour the visit opens. |
| `--observe-only farmer,cobbler` | the opening observation of each visit looks at that one person (a list is **new**), so the keyword that names them is always on screen. |
| `--npc-affinity DistantAcquaintance` | asking for work requires an acquaintance (`TradeGate.IsAcquainted`). Without it the farmer must first be met (`meet_stranger`), which is a choice the model makes among many goals. |
| `--grant-mm diligence,provisioning,swordsmanship:2` | diligence is a will whose one want is `request_job`, provisioning one whose want is `propose_to_buy`: thinking with either finds exactly one way and carries it out itself, so the asking and the buying are one thought each, decided by the path planner and not by the model. Swordsmanship at 2 gives the short sword's Snap Thrust without learning it mid-fight. |
| `--job-only farmhand` | **new.** The farmer offers that post alone (she offers three, at three wages, and which one the conversation lands on is the model's pick). A farmhand earns one copper per ten days. |
| `--humors all=voluptas` | every humor queue full of voluptas (any die → 6), so any roll a script cannot foresee can be turned with humors the player really holds. |

The script also declares `allow-flag-miss --start-area` and `allow-flag-miss --observe-only`: the
childhood, the get-up phase and the world map have no such room or person, which is expected.

## The organs, grouped by the system they feed

The creation screen tour follows this grouping. Each organ's score reaches the game through a
derived stat (`src/game/narrative/body/stats/`), read by name where the system needs it. Every organ
also raises the level ceiling of the modi mentis that name it (`MaxLevelContributionStats.cs`,
`PartyMember.GetMaxLevelForModusMentis`), which is not repeated below.

| group (script `clip`) | organ (`organ:<id>`) | what reads it, in the code |
|---|---|---|
| **Thinking and memory** (`organs_thinking`) | the encephalon as a whole | `NoeticPointsStat` (encephalon ÷ 2 + 1: thoughts per phase, read by `NarrativeController`); `WorkingMemoryCapacityStat` |
| | `anamnesis` | `ResidualMemoryCapacityStat` (memory slots); `MentalMapsStat` (anamnesis × 4 remembered places, `Protagonist.MentalMaps`) |
| | `cerebrum` | `SemanticMemoryCapacityStat` |
| | `cerebellum` | `ProceduralMemoryCapacityStat`; `fight_learning` dice for learning a fighting skill mid-fight (`Fighter`, `LearnSkillAction`) |
| | `hippocampus` | `SensoryMemoryCapacityStat` |
| | `pineal_gland` | `ThinkingIterationStats` (retries of a thinking, and doubt at 2+, `ThinkingExecutor`); `ModusMentisXpThresholdStat` |
| **The senses** (`organs_senses`) | `left_eye`, `right_eye` | modus mentis ceilings only (the most named organ: observation, thinking, action) |
| | `left_ear`, `right_ear` | fight initiative (`Fighter`, `FightModeAdapter`) |
| | `nose` | `EncounterAvoidanceStat` (the travel plan's encounter risk, `TravelPlanner`); `NoseRecognitionStat` (a food's composition shown in the inventory) |
| **Speech** (`organs_dialogue`) | `tongue` | `SpeechFluencyStat`: replies offered per turn of a conversation (`DialogueOptionGenerator`) |
| | `teeths` | `TeethHardFoodStat`: whether hard food can be eaten (`ConsumableItem`) |
| | the visage as a whole | `BeautyStat`: bonus dice in conversation (`DialogueTreeController`) |
| **The humors** (`organs_humors`) | `paunch`, `hepar`, `spleen`, `pulmones` | each fills its own humor queue (`HumorQueueSet`); food enters the paunch, drink the hepar |
| | `viscera` | `HumorModifierLimitStat`: humors spendable on one roll — narration, conversation and fight alike (`NarrativeController`, `DialogueTreeController`, `FightModeAdapter.EnableHumorIfPlayer`); `WoundHealingStat` |
| **Life** (`organs_life`) | `heart` | `MaxCompanionsStat`, `LifetimeStat`, `YouthfulnessStat` |
| | the trunk as a whole | `HealthPointStat`: hit points (`PartyMember`, `Fighter`) |
| | `genitories` | `GenderStat` (sex, and so the generated name); natural attack dice on every blow (`Fighter`, `SkillAction`) |
| **The body that fights** (`organs_fighting`) | `backbone` | `CombatStats` cinetic points (backbone × 2, a fight's action budget); `MaxWeightStat` (carrying, which gates travel) |
| | `left_hand`, `right_hand` | `ToolUsageProficiencyStat` (whether an implement can be used at all); punches |
| | `left_arm`, `right_arm` | push, elbow strike |
| | `left_leg`, `right_leg` | move speed in a fight, sprint, dodge; the lower limbs as a whole give `CombatStats.natural_defense` (defence dice) and `MaxTravelDistanceStat` |
| | `left_foot`, `right_foot` | runaway dice, footing, kicks |

The tour spends the creation screen's 25 points on the way, one `click element organ:<id>` per
point, so the body on screen is the body that plays: anamnesis +1, cerebrum +2, cerebellum +2, each
ear +1, tongue +2, viscera +4 (to 5: five humors on any roll), heart +4, genitories +1, backbone +3
(to 4: eight cinetic points), right hand +2, each leg +1. No `--organs`: that flag is applied when
the protagonist is accepted, after the screen, so the screen would show one body and the run play
another. `point element organ:<id>` hovers without spending (**new** command): the description and
the stats an organ drives show on the right as the cursor rests on it.

## What each section relies on

**World selection** (`world_selection`). The sky is drawn from a constant (`Config.SkyCloud.SkySeed`),
so moon 5 is **Fenumoth** on every machine, in the same place in the opening view. Under `--record` a
moon must be on screen to be clicked (the plain CLI turns the sky for you; the recorder refuses), and
only seven moons are in view without turning the sky: Fenumoth, Halarriel, Isholmir, Dunolwyn,
Garaesha, Torumdur, Arumgorn. Fenumoth is the one of those whose world has what the scenario needs: a
sylvan world (seed -259654943), the spawn at vertex 6992 on the coast, the **farm at vertex 7011**
(50 days) and the **city at vertex 27788** (41 days from the farm), whose cobbler sells leather boots.
(Belavel, moon 1, was the first choice and has everything too, but is out of view.)

**Childhood** (`childhood_1`…`childhood_5`). What the childhood grants is catalogue data
(`ReminescenceCatalog.cs`); the only thing the model decides is which fragment a reminescence's
observation looks at, and so which keyword is there to click. `--childhood-path` pins that, and the
script clicks each fragment's keyword by the fragment (`click keyword @sobs`, **new**: a keyword by
what it is linked to, not by the word the model wrote):

| reminescence | fragment | grants |
|---|---|---|
| sound_in_the_dark | sobs | → the port city; no clothes beyond townsman's cap, cloak, tunic, breeches — **no footwear** |
| port_city_childhood | the sound of waves and smell of sweat | Hard Labor; → work |
| work | something sharp | **Short Sword** (the WEAPON), Bargaining; → dream |
| dream | dream of a golden arch | the **traveller's backpack** (bread, cheese, sausage, a canteen of water: the CONSUMABLE), Archeology; → travel |
| travel | shelter | Exploration, Endurance; end |

Footwear comes only from the first reminescence's laugh (stable), scream (farm) and lullaby (castle),
so "sobs" is what keeps the feet bare. A remembered fragment has no dice; the get-up has one roll
(one six), settled with humors if it fails.

**Run start** (`run_start`, `anatomy`, `memory`, `inventory`, `humors`). The protagonist screen is
opened the player's way (`pause`, then `Protagonist`); `manage` is refused under `--record`. The
inventory has named controls now (**new**): `item:<name>`, `content:<name>`, `consume`, …; the script
opens the backpack and eats the bread. The humors tab then points at the newest humor in the paunch
(`humor:paunch:0`, **new**), which is the bread's.

**The two fights** (`fight_1`, `fight_2`). Fully seeded: the wolf comes from the travel generator
(`GameRng.For("travel")`), the arena from the run (`forced-encounter-arena`, **new**; it was
`Environment.TickCount`), every roll and every move of the wolf's from the fight's own stream. Nothing
the model writes reaches them. On Fenumoth the first wolf (Darkripper) starts far off and takes two
turns to close; we end our turns to let it come, then five Snap Thrusts kill it. The second
(Tawnyhackle) starts adjacent: five Snap Thrusts, our turns ended between. Neither lands a blow on us.
Each turn opens the Short Sword group first (the menu's groups close again after every action) —
`click fighter enemy` (**new**) names the wolf without its generated name — and spends humors only if
the attack roll is failing (`click humor <queue> needed` / `click die needed`, **new**). Skills that ask
the player to choose a body part (Needle Thrust, Deep Pierce) are avoided: their picker has no named
controls.

**The farm** (`farm_arrival`, `find_job`, `job_menu`). The visit opens on the farmer (Zedayne
Hartmead) in the Longhouse Hall. `@farmer` → Think → diligence: one way (ask for the farmhand's post),
which diligence carries out. The roll and the conversation's roll are settled with humors if failing.
The conversation's replies are the model's pick among the tree's options (each mind voices the option
the choice judge picks for it) and the branches end at depth two or three, so the script replies
`click option 0 needed` up to four times (**new**: replies only while a reply is on offer). The job
menu starts at 30 days; seventeen presses of ► make 200 days, which pay 20 copper.

**The city** (`city_arrival`, `buy_boots`, `trade`, `wear_boots`). The visit opens on the cobbler
(Publiosh Tommaezoth) in the Cobbler's Shop. `@cobbler` → Think → provisioning: one way (propose to
buy), carried out by provisioning; then the same humor and reply pattern. His shelf is seeded by his
id (`NpcTradeCatalog`): Leather Boots first, at 20 copper — the whole purse. A bought garment goes
straight to its place when that place is free, so the boots are on the feet when the inventory is
opened; nothing has to be dragged.

**Routines** (`chorography`, `routine_box`). The farm's mental map holds a Work routine (learned when
the job was agreed) and a Meet routine; the city's a Buy and a Meet. The Chorography tab is opened on
the city, the farm is clicked in the list (`place:farm`, **new**) and the stepper moved to the Work
routine (`routine-next`, **new**). Back on the world map, the road to the farm is planned and its
ROUTINES box opened (`routines-next`, `routines-return`, **new**).

## Code changes (all on `video-tutorial`, none merged)

Every flag below is inert at its default; every command is new vocabulary that changes nothing a
player does. None of them writes game state the scene did not produce.

**Flags**

| change | where | why |
|---|---|---|
| `--childhood-path <name>[,…]` | `Config.Debug.ChildhoodPath`, `ObservationPhaseController.ChildhoodPathFilter`, `Program.cs` | pins which fragment each reminescence observes (the first name that matches one of its fragments); a reminescence none matches chooses freely |
| `--force-encounter <c>[,…]` | `Config.Debug.ForcedEncounters`, `LocationTravelGameController.OnProtagonistSteppedToVertex` | one encounter per journey at its middle step, creatures in order, ahead of `--no-encounters`; while set, the encounter arena is drawn from `GameRng.Stream("forced-encounter-arena")` instead of `Environment.TickCount` (the ENGAGE handler) |
| `--job-only <job-id>` | `Config.Debug.JobOnly`, `JobRegistry.SampleJobs` | an employer whose pool holds that job offers it alone |
| `--llm-temperature <t>` | `Config.LLM.Temperature` (was a `const`), `Program.cs` | so the replays can be made at different temperatures |
| `--start-area` and `--observe-only` take a comma list | `SceneSyntheticGraphFactory.ResolveEntryArea`, `ObservationPhaseController.ObserveOnlyFilter` | one room / one person per kind of place the run visits; the first that matches wins; a miss is recorded only when none does |

**Commands** (`--cli` and `--record` alike unless noted)

| command | where | why |
|---|---|---|
| `click keyword @<name>` | `NarrativeController.CliFindKeyword` | the keyword linked to an object whose name (display, neutral, id or alias) contains `<name>` — the object is the scene's, the word is the model's |
| `point element <id>` | `RecordGate`, `CliDriver.CmdPoint` | hover a named control without pressing it (an organ on the creation screen, whose click would spend a point) |
| `elements`, `click element <id>` under plain `--cli` | `CliDriver` | they existed under `--record` only; `LocationTravelGameController.CliResolveElement` now resolves the name for both |
| `choose <mm-id>` under `--record` | `RecordGate` | the plain CLI had it; the recorder took indices only |
| `click humor <queue> needed`, `click die needed` | `RecordGate`, `DiceRollComponent.CliHumorNeeded/CliNeededDie` | press only while the roll is failing (and the humor budget lasts); the die is the first non-six the selected humor can change. A script spends "what it takes" without knowing the faces |
| `click option <n> needed` | `RecordGate` | reply only while the conversation offers a reply |
| `click fighter enemy` | `FightModeAdapter.CliFindFighter` | the nearest living enemy, whose name is generated |
| `click approach <fighter\|enemy>`, `click move` | `FightModeAdapter.CliApproachCell/CliApproach/CliMoveButtonCell`, `RecordGate`, `CliDriver` | walk to the reachable cell nearest a fighter. Written for the fights and not used in the end (the wolves come to us), kept because it is the only named way to move in a fight |
| `inspect npcs` prints `sells=item:price,…` | `NarrativeController` | which shelf holds boots was otherwise visible only by trading |

**Named controls** (for `click element` / `point element`)

| screen | ids | where |
|---|---|---|
| inventory | `item:<name>`, `content:<name>`, `content-back`, `consume`, `throw-away`, `move-to`, `move:<destination>` | `InventoryMenuRenderer.CliControlIdAt` |
| humors | `humor:<queue>:<index>` (0 = newest) | `HumorMenuRenderer.CliControlIdAt` |
| Chorography | `place:<name>`, `lock:<name>`, `routine-prev`, `routine-next` | `MentalMapsPanelRenderer.CliControlIdAt` |
| the travel plan's routine box | `routines-prev`, `routines-next`, `routines-confirm`, `routines-return` | `TravelRoutinesBox.CliControlIdAt` |

The protagonist screen's ids go through `ManagementMenuRenderer.CliControlIdAt`, by tab. Names are
lower case with underscores for spaces. None of these is a hover-sound id: `GetHoverElementId` is
untouched, so no screen sounds different.

## Fragilities

TODO

## Replays

TODO
