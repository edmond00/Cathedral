# Plan: World Content v1 — Medieval Economy Locations

## Purpose

This devplan covers the first full-content release of the Cathedral world. The goal is to populate all location types with grounded, medieval-economy-aware content so that a coherent material flow can be traced through the world: iron ore mined in a cave becomes a bar smelted in a village forge, which becomes a saw used by a carpenter or a sickle carried by a field reaper. No economy simulation is built — the flow exists only as authored item presence across locations, providing a believable backdrop for narrative exploration.

The plan also introduces two new structural systems required by this content release: path-based connections (FOLLOW verb) and vertical cliff connections (CLIMB_UP / CLIMB_DOWN verbs). Travelling NPCs (woodcutter/miner/fisherman moving between locations on a weekly schedule) are defined as archetypes but their travel logic is deferred.

---

**TL;DR:** 9 location types implemented using the existing `SceneFactory` → `Scene` system. Deliverables in 7 phases: new verbs/transitions → shared item library → NPC archetypes → reusable subfactories → factory rewrites (plain, farm) → new factories (village, field, forest, cave, mountain, peak, coast) → registration.

---

## Phase 1 — New Transition Types

Two new `PointOfInterest` subclasses + three new `Verb` subclasses. `VerbRegistry` auto-discovers verbs via reflection — no changes needed there.

**PathPointOfInterest** — named open connection (Village Road, Forest Track, River Path). Always passable from either end.
**FollowPathVerb** — `BaseDifficulty=1`, checks target is `PathPointOfInterest`, returns `AreaMoveOutcome`.

**CliffPointOfInterest** — holds `BottomArea` / `TopArea`, optional `IcyCliff` flag.
**ClimbUpVerb** / **ClimbDownVerb** — `BaseDifficulty=6` (icy=8), directional; mirrors `GoUpStairsVerb` pattern.

New files:
- `src/game/scene/building/PathPointOfInterest.cs`
- `src/game/scene/building/CliffPointOfInterest.cs`
- `src/game/scene/verbs/FollowPathVerb.cs`
- `src/game/scene/verbs/ClimbUpVerb.cs`
- `src/game/scene/verbs/ClimbDownVerb.cs`

---

## Phase 2 — World Item Library (`src/game/narrative/world/items/`)

Abstract bases in one file (`WorldItemBases.cs`): `FruitItem`, `VegetableItem`, `HerbItem`, `WoodRawItem`, `StoneRawItem`, `MetalItem`, `ToolItem`, `TextileItem`, `AnimalProductItem`, `SeaFoodItem` — each sets the right `ItemType`, `Weight`, and `Size` defaults.

Concrete files (only items **not** in the existing `items/` directory):

| File | Contents |
|---|---|
| `VegetableItems.cs` | Radish, Parsnip, Onion, Leek, Pea, Beetroot |
| `FruitItems.cs` | Pear, Plum, Cherry, Blackberry, Bilberry, Sloe, HawthornBerry, Elderberry, Acorn, Beechnut |
| `HerbItems.cs` | Thyme, Sage, Mint, Chamomile, Wormwood, WildThyme, WildMint, Valerian, Gentian |
| `WoodItems.cs` | Log, Plank, Twig, BirchSap |
| `StoneItems.cs` | Clay, Lichen |
| `MetalItems.cs` | IronOre, IronBar, Coal, Nail, IronHoop |
| `ToolItems.cs` | Saw, Axe, Pick, Shovel, Hammer, Tongs, Chisel, Mallet, Shears, Rake, Hoe, Scythe |
| `TextileItems.cs` | Thread, Cloth, Flax, Linen |
| `FarmProductItems.cs` | Milk, Butter |
| `ProcessedGoodItems.cs` | Flour, Ale, Mug, Barrel, Lantern, Sack, Net, Hook, FishingLine, Basket |
| `SeaItems.cs` | Herring, Cod, Mackerel, Crab, Mussel, Shell, Seaweed, Driftwood, RopeFragment |
| `WildPlantItems.cs` | Nettle, Fern, Ivy, Bramble, Reed, Watercress |
| `AnimalDropItems.cs` | BoarTusk, WolfPelt, DeerHide, GoatHide, LynxPelt, SealPelt, EagleFeather, Feather |

Existing items (`Turnip`, `Carrot`, `Apple`, `Egg`, `Wool`, etc.) are reused directly — no duplication.

---

## Phase 3 — NPC Archetype Library (`src/game/npc/archetypes/`)

Abstract bases: `PeasantArchetype` (field/farm roles), `CraftsmanArchetype` (village workshop workers), `WildernessNpcArchetype` (woodcutter/miner/fisherman — week-travel pattern stubbed).

New concrete archetypes:
- **Village:** `BlacksmithArchetype`, `ApprenticeArchetype`, `CarpenterArchetype`, `WeaverArchetype`, `CooperArchetype`, `MillerArchetype`, `BakerArchetype`, `BrewerArchetype`
- **Field:** `ReeveArchetype`, `PlowmanArchetype`, `ReaperArchetype`, `HaywardArchetype`, `BondmanArchetype`
- **Farm:** `ShepherdArchetype`, `SwineherdArchetype`, `DairymaidArchetype`, `PoultryKeeperArchetype`
- **Wilderness:** `WoodcutterArchetype`, `CharcoalBurnerArchetype`, `MinerArchetype`, `FishermanArchetype`

---

## Phase 4 — Reusable Subfactories (`src/game/scene/shared/`)

**`TerrainSubfactory`** (static) — tree PoI builders (`BuildOakTree`, `BuildBirchTree`, `BuildPineTree`, `BuildWillowTree`, etc.), rock features (`BuildBoulder`, `BuildRockOutcrop`, `BuildRockFace`), water spots (`BuildStreamBank`), vegetation (`BuildFlowerPatch`, `BuildBerryBush`, `BuildMushroomCluster`). Used by plain, mountain, peak, forest, cave.

**`WorkshopSubfactory`** (static) — returns complete `Area` instances for each workshop type: `BuildForge()`, `BuildCarpenterWorkshop()`, `BuildCooperWorkshop()`, `BuildWeaverWorkshop()`, `BuildMill()`, `BuildBakery()`, `BuildAlehouse()`. Each area has appropriate PoIs (Tool Rack, Stock Shelf, Finished Goods Rack, etc.) with the correct items for the economy flow. Used exclusively by `VillageSceneFactory`.

**`AnimalPenSubfactory`** (static) — `BuildSheepPen()`, `BuildPigsty()`, `BuildDairyShed()`, `BuildChickenCoop()`. Extracts what's currently inline in `FarmSceneFactory`. Used by farm rewrite and potentially field/village.

**`CampSubfactory`** (static) — `BuildForestCamp()`, `BuildMineCamp()`, `BuildCoastCamp()`. Sparse wilderness camps with bedroll + site-specific PoIs. Used by forest, cave, coast factories.

---

## Phase 5 — Rewrite Existing Factories (*depends on Phases 1–4*)

**`PlainSceneFactory`** — rebuild per `plain.md`: 3 sections (Flatlands, Highlands, optional Wetlands 25%), area pool of 7 types, tree species tied to area identity (oak/hawthorn for open, elder/hawthorn for heath, willow/elder for wetland), `PathPointOfInterest` for area connections. Use `TerrainSubfactory`. Beast/shallow NPCs via archetypes.

**`FarmSceneFactory`** — rebuild per `farm.md`: area pool drawn from spec (Courtyard hub + optional pens + Farmhouse via existing `HouseBuilder`). `AnimalPenSubfactory` replaces inline pen builders. Extended NPC roster (Farmer, Farmhand×1–3, Shepherd, Dairymaid, Swineherd, PoultryKeeper) with day-cycle schedules per spec. Animals as shallow NPCs.

---

## Phase 6 — New Location Factories (*depends on Phases 1–4; can run parallel with Phase 5*)

**`FieldSceneFactory`** — 2 sections, 5 area types; grain crop drawn from `[wheat, barley, rye]` once per seed; vegetable beds pick 2–3 types; `PathPointOfInterest` connects strips. NPC day-cycle: Reeve, Plowman, Reaper, Hayward, Bondman.

**`ForestSceneFactory`** — forest identity (deciduous/coniferous/mixed) gates tree pool. `CampSubfactory.BuildForestCamp()` in Clearing when woodcutter present (30%). Cut-wood PoIs (Felled Log, Tree Stump, Deadfall). Beast/shallow NPCs.

**`CaveSceneFactory`** — cave type (iron-rich/stone-quarry/coal-bearing) gates ore presence. `CliffPointOfInterest` for Mineshaft Ladder. `CampSubfactory.BuildMineCamp()` in Entrance Hall. MinerArchetype (25%).

**`MountainSceneFactory`** — slope character (sunny/damp) affects area pool. `CliffPointOfInterest` for Cliff Ascent to peak. Optional Door to cave (50%). Alpine herb patches.

**`PeakSceneFactory`** — very sparse; always accessed via `CliffPointOfInterest` from mountain. Cairn (60%). Eagle always spawns. Ice Shelf atmosphere-only.

**`CoastSceneFactory`** — coast identity (sandy/rocky/clifftop) gates area pool. `CliffPointOfInterest` between Cliff Base ↔ Cliff Top. `CampSubfactory.BuildCoastCamp()` in Sandy Beach when fisherman present (30%).

**`VillageSceneFactory`** ← most complex — Square + Forge + Mill always present; roll 2–5 optional from pool (Carpenter, Cooper, Weaver, Bakery, Alehouse, Craftsmen Hall, Sleeping Quarters). `WorkshopSubfactory` builds each workshop area. `PathPointOfInterest` for all village roads and lanes. Door from Craftsmen Hall → Sleeping Quarters. Full NPC roster with day-cycle schedules.

---

## Phase 7 — Factory Registration (*depends on Phase 6*)

In `src/game/LocationTravelModeLauncher.cs`, after the existing `"farm"` registration (line 146), add:

```csharp
gameController.RegisterSceneFactory("plain",    new PlainSceneFactory());
gameController.RegisterSceneFactory("field",    new FieldSceneFactory());
gameController.RegisterSceneFactory("forest",   new ForestSceneFactory());
gameController.RegisterSceneFactory("village",  new VillageSceneFactory());
gameController.RegisterSceneFactory("cave",     new CaveSceneFactory());
gameController.RegisterSceneFactory("mountain", new MountainSceneFactory());
gameController.RegisterSceneFactory("peak",     new PeakSceneFactory());
gameController.RegisterSceneFactory("coast",    new CoastSceneFactory());
```

---

## Verification

1. `dotnet build` passes after each phase
2. Each location type is reachable in-game and entry node populates
3. `FollowPathVerb` appears as "Follow" on road/track connections; `ClimbUpVerb`/`ClimbDownVerb` appear at cliff connections
4. Economy flow checks: village forge Finished Goods Rack contains Saw/Axe/Knife; carpenter workshop has Saw; field tool rest has Sickle; cave Ore Chamber yields IronOre
5. NPC day-cycle schedules produce correct area assignments per time period
6. `logs/graph_location_*.txt` written for all new location types

---

## Decisions

- **SceneFactory system retained** — all factories extend `SceneFactory`; bridged to narrative pipeline by existing `SceneSyntheticGraphFactory`
- **Travelling NPCs skipped** — archetypes defined but week-schedule travel is stubbed
- **Economy simulation not included** — item presence only models the flow as world content

---

## Open Questions

1. **Item namespace**: Distinct `Cathedral.Game.Narrative.World.Items` or merge with existing `Cathedral.Game.Narrative.Items`? Recommend distinct — keeps legacy items isolated.
2. **Subfactory location**: New `src/game/scene/shared/` folder vs. extending `src/game/scene/building/`? Recommend `shared/` — terrain/camp subfactories aren't "building" types.
3. **Village area count**: Always exactly 5–8 total areas, or a fixed optional count (e.g., always roll 2–5 optional on top of the 3 mandatory)?
