# Working under `src/game/history/`

Every world carries a history: the empire's (the lore, hardcoded, the same everywhere) and its own
(generated from the seed, over its regions). Nothing in the game reads it yet except the K overlay
and the CLI; it is the source that books, chronicles and inscriptions will be written from, each one
a deliberate distortion of it (see `lore/11_texts_and_sources.md`).

`lore/` is the prose source of truth for the empire. `lore/EmpireLore*.cs` is that prose as objects.
When one changes, the other must follow, and `--history-audit` checks the objects.

## The shape of it

| | |
|---|---|
| `HistoricDate` | a round (360 days) in FC, with a precision: exact, circa, **unknown**. `HistoryCalendar` holds the fixed points: Foundation 0, Hatching 3579, present 4191 (612 AH, the same on every world), local history from −1000 (moved up from −2000 to keep a past readable: every round is another generation of rulers) |
| `HistoricInfo` | what history is about: `HistoricFigure`, `HistoricFaction` (and `Realm`), `Religion`, `Deity`, `Place`, `War`, `WorldInfo`. Each has a `Scope`, Empire or World |
| `HistoricEvent` | a dated, typed thing that happened, naming its infos (`Involved`) and describing itself in one neutral sentence (`Describe`). Typed where code needs to ask (`AccessionEvent`, `ImperialEvent`, `WarStartedEvent`...), `ChronicleEvent` for everything that only needs telling |
| `Chronology` | the ordered event list. Recording links the event into its infos' `Events`, **only for infos of the chronology's own scope** |
| `EmpireLore` | the lore catalogue, built once, no randomness. Fields for everything code refers to |
| `WorldHistory` | one world's result: its chronology, infos, region names, who owns each region at the present, the proscribed faiths, the hash |
| `HistorySimulation` | the engine and the one door every change to a world goes through |
| `WorldProfile` | which seeds a world gets. `GenericWorldProfile` by imperial status; the lore profiles in `worlds/LoreWorldProfiles.cs` |
| `LoreMoons` | which sky ordinals are lore worlds, their names and forced variants |

## Sow and sprout

A seed (`HistorySeed`) is something that must happen, sown before anyone knows how: a realm will be
founded, the empire will come, someone will die. The simulation keeps them in a queue by round (then
`Priority`, then sowing order), advances the chronology's cursor to each one's round, and asks
`CanSprout`: a seed that no longer makes sense (a war with one realm left, a death of the already dead)
is skipped and counted. A seed that sprouts changes the world through the simulation's helpers,
records events **at the current round**, and may sow more seeds **in the future**.

`PulseSeed`s are the background rhythm (war, settlement, union, breakup, faith, building, catastrophe):
they act or find nothing to act on, and always sow their next beat. `ScriptedSeed` is a lore beat: an
action at a date. Lore profiles are mostly made of these.

## The rules that keep a generated past coherent

- **Nothing is written into the past.** `Chronology.Record` throws on an event before the cursor, and
  `Sow` throws on a seed due before it. So a person invented at round R (a realm's founder) cannot
  have a birth event: their `Born` stays **unknown**, and only their death is sown. Anyone born during
  the simulation (`HistorySimulation.Birth`) has both.
- **Every change goes through `HistorySimulation`.** `SetRegion` keeps the ownership table and each
  realm's `Regions` in step, and gives a realm proclaimed with nothing (a province at the start of a
  conquest) its capital in the first region it gains. `Transfer` dissolves a realm left with nothing;
  `Kill` opens the succession. A seed that edits `Realm.Regions` directly makes the two disagree,
  which the audit names but nothing else would.
- **The empire catalogue is shared and must never be written by a world.** A world may crown an empire
  figure (Jelebanne rules the Kingdom of Belune), name one in an event, or adopt an empire faith, but
  never kill one, give one a child, marry one, or change an empire info's fields. `Kill`, `Crown`'s
  heir seeds, `Succeed`'s choice of heir and `FoundRealm`'s affiliation all check `Scope`. The
  audit fingerprints the catalogue before and after generating forty worlds.
- **Proscription is per world** (`WorldHistory.Proscribed`), never `Religion.Proscribed`, which is
  descriptive and empire-level. The Inquisition proscribing Medusosianism on Belune must not proscribe
  it on the next world generated.
- **Never identify content by a string** (the root rule). Imperial works are places whose `Builder` is
  an empire faction (`Place.IsImperialWork`), not places whose name ends in "trading post".
- **Determinism.** Two streams: `GameRng.For("history-language")` for names, `GameRng.For("history")`
  for everything else, both fresh `Random`s off the master seed. Iterate lists, never a `HashSet` or a
  `Dictionary` whose order you would then draw from. The audit generates every world twice and
  compares hashes, because Continue refuses a save whose regenerated history hashes differently.

## The viewer

With viewers on (`--view`, or `--debug` without `--hidden`) the world-selection screen opens the
**world history viewer** (`src/debug/WorldHistoryWindow.cs`): tabs for an overview, the chronology
(filters by era and kind, can merge the empire's own events), realms, figures, faiths, places, wars,
regions and the empire's lore, each a searchable list with a details pane whose linked entries jump
to their own tab on a double-click. It follows the chosen moon, then the run's world once one exists.

The chosen moon's history is built **before** the moon is taken, by `WorldHistoryPreview`: headless,
off the main thread, cached by seed. That is only safe because generation takes its seed as a
parameter and draws through `GameRng.ForWorld`, which reseeds nothing global; `HeadlessWorld` does the
same. Never make either reach for `GameRng.For` or `Reseed` again: a preview would then race the game
for the master seed. `cli/system/world_history_viewer.cli` checks that the previewed history is the
one the run gets (`preview=match`); under the CLI the preview is built even with no window, so the
check runs in the suite.

## Faiths: open, hidden, gone

`WorldHistory.Religions` is every faith that ever reached the world; `Presence` says how each lives
**now**: `Open`, `Clandestine` (a hidden sect) or `Extinct`. `Proscribed` is separate: who has banned
it, not whether anyone still keeps it. Change any of the three only through the simulation
(`Adopt`, `Smuggle`, `Proscribe`, `Surface`, `DieOut`), which keep them consistent.

- **Proscription drives a faith underground** (or, by chance, ends it): the Inquisition, or the
  strongest realm banning a faith no realm keeps.
- **Any empire faith can reach any world the empire touched, in hiding.** Each first contact sows a
  few `ClandestineArrivalSeed`s before the Hatching, carried by a flavoured story (a book in a
  sailor's kit, an exile preaching at night, a palimpsest's lower text, a red thread on a door...).
  Faiths forbidden at that date (`Religion.ForbiddenSince`) travel most.
- **`Religion.ClandestineAbroad`** marks the empire faiths that may be open only on their own lore
  world (Medusosianism on Belune, the Fallen God on Oox's worlds, Beatildism on New Varam, the Last
  Empress on Golden Avoria; the Far Lore, the Rede cult and the Stillness nowhere).
  `WorldProfile.MayHoldOpenly` is the one question, and `Adopt` obeys it: adopting such a faith
  elsewhere smuggles it as a hidden sect instead. The audit fails any faith open where it may not be.
- `ClandestinePulseSeed` is a hidden sect's life: it spreads, is found out (martyrs, sometimes the
  end), dies out, or surfaces, which needs a realm, a world that allows it, and no imperial
  province watching.

## Organisations

`Organisation` (a `HistoricFaction`) is everything organised that is not a realm: knightly orders,
craft, merchant and miners' guilds, monastic orders, colleges, healers, bards, hunters' lodges,
mercenary and pirate companies, thieves and assassins, secret societies, and **imperial branches**.
Each has a home region, maybe a patron realm, a faith it serves, an empire counterpart it is a branch,
cell or heir of, and a `Clandestine` flag.

- `OrganisationPulseSeed` founds one when the world can carry it (guilds need towns, pirates a coast,
  knights a realm worth serving, a secret society often a hidden faith), up to a cap of a fifth of the
  regions. Names come from `OrganisationGenerator`, several templates per kind.
- `OrganisationLifePulseSeed`: a new master of its home charters it, bans it or breaks it up; an order
  whose faith is banned goes underground, whose faith is gone ends; it spreads, splits (over a
  quarrel that fits its kind), is absorbed, comes out of hiding, or dwindles with age.
- The empire brings **branches** (`ImperialBranchSeed`: IISTG factor-houses, Plebeian courts, Knights'
  chapters, Inquisition tribunals) and **hidden cells** (`ClandestineCellSeed`: Ticklers, Beatildist
  wells, Far Lodges, Red Tribunal remnants, the Wakeful). The Inquisition's arrival is a crackdown
  (`Crackdown.Apply`). A branch is **exempt from the life pulse while the empire stands**: its fate is
  the empire's, not a local master's.
- **After the Hatching**, every branch becomes a local faction or ends (`BranchAfterHatchingSeed`: a
  Knights' chapter becomes the Stranded Knights or the Order of the Last Ship, a tribunal the Grey
  Brotherhood or a lynching, a factor-house the Guild of the Rusting Hulls...). The successor
  **keeps the institution as its `ImperialCounterpart`**, which is its lineage. Every hidden cell
  either goes its own way under its own name or dwindles (`CellAfterHatchingSeed`). The audit fails
  any branch still standing at the present, and fails if no branch on the sampled held worlds ever
  survives as a faction of its own; it prints what became of each on three worlds.
- Lore worlds found their own (`LoreWorldProfile.OrganisationBeat`): the Physicians of the Springs,
  the Salt Brothers, the Wells of New Varam, the Priests of the Pit...

## The moon box

The world-selection box reads the chosen moon's history, built in the background by
`WorldHistoryPreview` the moment the moon is chosen (always, not only with viewers): its relation to
the empire (`WorldHistory.EmpireRelation`), and how many realms, faiths and factions stand today, with
the hidden ones counted apart. `inspect world-preview` carries the same values for scripts.

## Regenerated, never saved

Like the terrain, a history is a pure function of the seed, rebuilt by `GenerateWorld` on New and on
Continue. The save stores only `HistoryHash`; `TryContinueSavedRun` refuses a mismatch, which can only
mean the generator lost its determinism. Generation takes 50 to 550 ms.

## Balance

Two opposed pressures keep the map at the lore's post-imperial shape (realms and free cities, neither
one crown over a world nor one per region), and the audit fails outside three to half-the-regions
standing realms:

- **Consolidation**: conquest takes border land and swallows beaten small realms; marriage unites
  neighbours; free land goes to neighbours. When the world is **crowded** (more than a quarter as many
  realms as regions) routine breakaways and new foundations hold back.
- **Fragmentation**: `BreakupPulseSeed` presses on the largest realm once it passes a sixth of the
  world; contested successions split; unions that would build a great realm do not happen.

The imperial province is exempt from both while the empire stands (its revolts are `RevoltPulseSeed`'s),
and the Stranding's breakup is `forced`, because a stranded province comes apart however crowded the
map already is.

## Lore worlds

`LoreMoons.Named` puts 27 lore worlds on fixed sky ordinals, all at 20 or above, since scripts name the
first moons (Armoth, Belavel). `SkyMoons.Name` and `WorldVariants.ForSeed` both consult it, so a lore
moon shows its lore name in the moon box and is forced to the terrain the lore describes (the
climate variants carry the lore's deserts and ice: Golden Avoria is Arid, Green Avoria Tropical,
Zuilkansia Tabular, New Varam Glacial, Nadirine Polar). Eleven more moons are
Oox's unnamed worlds and twenty-one Fogun's, chosen by a fixed hash, keeping their sky names. **Moving
an ordinal re-rolls that moon and invalidates saves on it.**

A lore profile keeps the generic native past and replaces the imperial chapter with the lore's dates,
people and places. The audit fails any `ScriptedSeed` that did not sprout, which is how a beat sown
past the present, or one whose world was not in the state it expected, gets noticed.

## Adding to it

- **A new seed**: a class in `engine/seeds/`, changing the world only through `HistorySimulation`,
  guarded by `CanSprout`. Sow it from a profile or from another seed.
- **A new lore world**: an info in `EmpireLore.BuildWorlds`, an entry in `LoreMoons.Named` (a free
  ordinal of 20 or more), a profile. Run `--history-audit`.
- **A new event type**: only if code needs to ask about it; otherwise it is a `ChronicleEvent`.
- **A new place kind**: a `PlaceKind`, its category in `PlaceSites.CategoryOf` and its ground in
  `PlaceSites.Fits`, a location key in `SettlementSprawl.LocationKeyOf`, a glyph in
  `BiomeDatabase.SettledGlyphs`, and a programme in `HistoricSceneFactory`. `--history-audit` fails a
  place standing where its kind may not.

## History on the map

Every world place stands on a **vertex** (`Place.Vertex`), picked when it is founded by
`PlaceSites.PickSite` among its region's cells of the right ground: urban places (citadel, port,
palace, imperial school) on plain, mountain or the steppes, never jungle or snowfield, ports and
commanderies by water; rural ones (castle, fortress, temples, commandery) on livable ground; isolated
ones (monastery, mine, sanctuary, burial field, pyramid, and every ruin) where their kind belongs. A
kind no cell of the region can host is not drawn at all (`DrawPlaceKind`'s filter), and
`HistorySimulation.NewPlace` falls back to the owner's other regions, then to any.

**Castles are sown, not drawn**: `RealmFoundationSeed` sows a `CastleSeed` for every realm that rises
on free land (the initial founders and later free-land foundings — never a split or a revolt), and the
seed re-sows itself until a livable cell is free. The audit holds every such realm that lived twenty
years to having one.

**The sprawl is a second pass, after history and before play** (`SettlementSprawl`, run from
`MicroworldInterface.BuildSettlement`): every standing place takes its own cell; urban places sprawl
one to three city cells, and from those and from every rural place the farmland grows outward one to
three cells per ring over two or three rings, a third of the outer cells turning into a settlement or
stock instead. What grows where is `SettlementTable` (the location table): one column per livable
biome. A ruin does not sprawl, and **no sprawl crosses a border**: a cell is taken only if its region is held by the realm holding the place (`realmAt`). Free space within the realm is the only limit, the biome under a location never changes, and
forest is not livable. `WorldVariant.SettlementDensity` scales the branching.

**A factory finds its place by its vertex.** `WorldSites` publishes the map, the history and the biome
lookup; `HistoricSceneFactory` names a castle "the castle of Varsk" from it, `RuinSceneFactory` says
what a ruin was, `CitySceneFactory` names its streets after the place it grew round, and every
settled factory builds in the material of the ground it stands on. With no world published (the
audits), each falls back to a generic place of its kind. `SettledSceneFactories` is the one list of
location key to factory, read by the launcher and every audit.
