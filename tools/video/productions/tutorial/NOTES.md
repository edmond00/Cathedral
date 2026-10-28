# The tutorial run: notes

`tutorial.cli` plays the whole tutorial scenario (BRIEF.md) by itself under `--record`, from the main
menu to a calm closing frame. This file is what it depends on: the flags, what each section relies
on, the code changes made on this branch for it, and every fragility known.

(Draft: sections marked TODO are filled in as the runs come back.)

## Launch

```bash
tools/video/.venv/Scripts/python tools/video/make_video.py record \
    tools/video/productions/tutorial/tutorial.cli build/rec_tutorial --timeout 14400 --flags \
    "--childhood-path 'sobs,waves,sharp,golden arch' --force-encounter wolf,wolf --no-encounters \
     --npc-static --start-area 'Longhouse Hall,Cobbler''s Shop' --observe-only farmer,cobbler \
     --npc-affinity DistantAcquaintance --grant-mm diligence,provisioning,swordsmanship:2 \
     --job-only farmhand --humors all=voluptas"
```

No `--seed` (it would skip the world-selection screen), no `--skip-childhood`, no `--playground`.

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
another.
