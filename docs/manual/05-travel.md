# V. Travel

The world is a sphere divided into cells. Travel is movement from one cell to another, and it costs
days, vital heat (the energy the body draws from its humors) and the risk of meeting danger on the
way.

**In short**

1. A destination can be set only within the travel radius, which grows with the lower limbs score.
2. Each cell crossed costs days and vital heat according to its biome; mountain costs 12 days a cell, plain 5.
3. Each cell crossed can bring an encounter, at a chance set by its biome.
4. Vital heat is paid from the humor queues while walking; when every queue is all black bile, the character dies of starvation.
5. The calendar moves only when a journey ends, when hired work ends, and when night turns to dawn during a visit.
6. The party holds at most 1 companion per point of heart score.

## 1. Choosing a world

Every world is generated from a single number, and the world is chosen by picking a moon.

**Rules**

1. One number fixes the whole world: its terrain, where each location stands, the cost of every step between two cells, and the people inside each place.
2. Two players given the same number get the same world, and no part of it is decided later.
3. The number is not entered directly; it is chosen by picking one of **383 moons** in a sky of stars.
4. Each moon stands for one world, and each has a name.
5. The name, the moon and the world under it are the same for every player and never change.
6. The sky is generated from a fixed constant, not from the world's number.
7. Reason: a sky generated from the world's number would rearrange itself the moment a moon was picked.
8. A run begins by picking a moon; until then no world exists.
9. Picking a moon fixes the number, and the world is generated before the protagonist is made ([Chapter I §9](01-anatomy.md#9-creating-a-protagonist)).
10. Looking costs nothing, and a pick can be undone at no cost, since nothing has been made yet.
11. The moon of the world the character stands on is missing from that world's sky, so 382 moons are visible.
12. The same holds when returning to a world that was set aside: its moon is missing from its own sky while it is played.

## 2. The kind of world

Each world is built in one of **20 dispositions** (styles of world), chosen by the world's number.

**Rules**

1. The terrain is read from four noise fields laid over the sphere: one for water, one for relief, one for land cover, one for heat.
2. A disposition is a set of thresholds and scales for reading those fields, plus how thickly the land is settled.
3. A disposition sets the six things in the table below.
4. The disposition follows from the world's number by a fixed rule, so the moon that names a world also names its disposition.
5. The disposition is known before the world is made; two players who pick the same moon get the same kind of ground.
6. A disposition changes only how the sphere is laid out; it changes no cost, no hazard chance and no body threshold.
7. Every disposition is checked to give a minimum of walkable land, a minimum of settled country, and a single landmass with enough settled country to live on.
8. Reason for rule 7: a world that failed any of these would generate correctly but be unplayable, so the bounds are checked rather than assumed.

| Setting | What it decides |
|---|---|
| Waterline | how much of the sphere is sea, and so how much land there is |
| Reach of the shore | how much of a coastline counts as shore rather than inland country, and how far below the waterline shallow sea turns to deep ocean |
| Treeline | where ground turns to mountain, and where mountain turns to bare peak |
| Land cover | what share of walkable ground is forest, and how thickly settled country spreads over the rest |
| Size of features | how broad a continent is before the sea breaks it, and how long a mountain range runs |
| Climate | how far the whole world is shifted toward heat or cold |

**Example.** A high waterline leaves narrow land between long arms of sea. No route on foot crosses
water (§3), so such a world is split into separate countries. A low treeline puts mountain and peak
across the middle of the land. Mountain costs 12 days a cell against plain's 5 (§4), so every journey
there is longer, costs more heat and meets more mountain hazards. A sparsely settled disposition
spreads country thinly around each historical place; a thickly settled one spreads it far wider
(§2a). A mountain still costs 12 days in every world; the disposition decides only how much mountain
there is.

### Shores

A shore (coast) is the land cell at the edge of the sea.

**Rules**

1. The noise fields first propose shore wherever the water reading stands a little above the waterline.
2. Every proposed shore cell that borders neither sea nor ocean is then turned into plain.
3. Every land cell that borders the sea becomes shore, whatever the noise made of it.
4. A mountain range or forest that reaches the water therefore ends in a shore; in cold country that shore is sea ice.
5. Only shore and sea ice ever border the sea.
6. A disposition can change how completely a coastline counts as shore, never how far shore reaches inland.
7. Reason: in a world of large continents the water field slopes gently, and without rule 2 the shore band would put beaches far inland.

### Heat and cold

The heat field is read last and turns temperate ground into hot or cold ground.

**Rules**

1. A cell whose heat reading is above **0.48** is hot country.
2. A cell whose heat reading is below **−0.48** is cold country.
3. Hot and cold country change the ground as shown in the table; every other ground keeps its temperate type (a hot shore is a shore, a cold forest is a forest).
4. Any temperate plain that touches desert becomes **canyon**.
5. Sea next to sea ice freezes into sea ice too, ring by ring outward.
6. Each ring needs 0.08 more cold than the ring inside it: ring 1 freezes below −0.56, ring 2 below −0.64, and so on.
7. A frozen sea is sea ice, and is walked on like land.
8. A barely cold world therefore has an iced shore; only a world far below −0.48 has a frozen sea.
9. 10 of the 20 dispositions are temperate (climate shift 0); each still has about 7 to 9 small pockets each of hot and cold country, a few per cent of its land.
10. The other 10 are shifted hot or cold by between **0.62** and **0.80**, and are hot or cold over half their land or more.
11. Hot steppe, jungle, cold steppe and snowfield are settled in their own way.
12. Desert, canyon, glacier and sea ice are **barren**: they hold nothing but isolated historical places (§2a).

| Temperate ground | In hot country | In cold country |
|---|---|---|
| Plain | desert | cold steppe |
| Mountain | hot steppe | snowfield |
| Peak | hot steppe | glacier |
| Forest | jungle | forest |
| Shore | shore | sea ice |

### Where the character starts

The starting cell is always on land connected to settled country.

**Rules**

1. No route on foot crosses water, so the landmass the character starts on is the whole of the world they can reach.
2. The starting cell is picked from the first of these three sets that has any cell in it.
3. Set 1: open ground (a livable cell or a shore with no location on it) that borders a city, farmland, a settlement or stock.
4. Set 2: open ground with no location, anywhere, for a world whose history left it unsettled.
5. Set 3: any land at all, so that the game can always begin.
6. Reason: starting beside settled country ensures there is somewhere to go and that the landmass has people on it.

## 2a. Settled country

The land is settled by the world's history in two passes, both fixed by the world's number.

### Historical places

**Rules**

1. Every place the world's history founds (a citadel, a castle, a temple, a mine and so on) stands on one cell of its region.
2. That cell is chosen among the cells whose ground its kind of place can stand on, as shown in the table.
3. A kind of place that no cell of a region can carry is never founded there.
4. Every realm that rises on free land builds a **castle** for its founder soon after it is founded.
5. A place that history ruins stays on its cell as a **ruin**, which counts as isolated whatever it was before.

| Kind of place | Examples | Stands on |
|---|---|---|
| Urban | citadel, port, palace, imperial school | plain, mountain, hot steppe or cold steppe; never jungle or snowfield; a port beside water |
| Rural | castle, fortress, temple, imperial temple, commandery | any livable ground; a commandery beside water |
| Isolated | monastery, mine, sanctuary, burial field, pyramid | the ground its kind needs, livable or barren |

### How settled country spreads

**Livable ground** is plain, mountain, hot steppe, cold steppe, jungle and snowfield. Forest stays
wild, and barren ground is never settled.

**Rules**

1. Settled country spreads outward from every place still standing, over free livable cells.
2. An urban place first builds **1 to 3 city cells** around itself; where no city ground is free beside it, it spreads farmland directly.
3. From each city cell and from every rural place, **farmland** spreads in rings 2 or 3 deep, each cell adding 1 to 3 more on the free livable cells beside it.
4. **1 farmland cell in 3** becomes a **settlement** or a place where **stock** is kept instead, in equal shares where that ground keeps stock.
5. Settled country never crosses a border: every cell it takes lies in a region held by the realm that holds the place.
6. A place on a frontier therefore spreads only on its own side.
7. Every count of 1 to 3 is scaled by the disposition's density and kept between 1 and 5.
8. Only a lack of free livable ground inside the realm stops the spread.
9. Isolated places and ruins spread nothing.
10. Each livable biome has its own crops, its own kind of settlement and its own stock; jungle and snowfield keep no stock.
11. A location never changes the ground under it: a field on cold steppe is crossed as cold steppe.
12. A location is built in the materials of the ground it stands on.

## 3. Travel radius and waypoints

A destination can be set only within a fixed distance of the character's position.

**Rules**

1. The radius is the **Max Travel Distance**, a value derived from the lower limbs score (the summed score of the legs and feet; see [Chapter I §6](01-anatomy.md#6-derived-values)).
2. Max Travel Distance runs in a straight line from **4.0 units** at a lower limbs score of 0 to **14.8 units** at the region's maximum score (10 for a human).
3. Distance is measured as a straight line through the sphere, not as a count of steps.
4. Neighbouring cells are about 0.74 units apart, so the radius reaches about 5 cells at score 0 and about 20 at the maximum.
5. Up to **4 waypoints** can be set inside the radius, and the route is found through them in order.
6. Waypoints let the character insist on a particular way round instead of the shortest route.
7. No route on foot can enter **sea** or **ocean**.

| Lower limbs score (human) | 0 | 2 | 4 | 6 | 8 | 10 |
|---|---|---|---|---|---|---|
| Radius (units) | 4.0 | 6.2 | 8.3 | 10.5 | 12.6 | 14.8 |
| About (cells) | 5 | 8 | 11 | 14 | 17 | 20 |

## 4. The cost of a journey

Each biome sets what one of its cells costs to cross, and the route's cost is the sum over its cells.

**Rules**

1. Every cell entered is charged; the cell the character starts on is not.
2. A cell with a location on it is crossed as the biome under it.
3. A biome not in the table costs 6 days and 1 vital heat per cell, with no hazard.
4. Sea and ocean have values recorded (in italics below), but no route on foot enters them.
5. A single cell costs several days, and a long journey runs into hundreds of days; time is the main cost of travel.

| Biome | Days per cell | Vital heat per cell | Hazards, chance per cell |
|---|---|---|---|
| Plain | 5 | 2 | wolf 2 %, bandit 1 % |
| Coast | 6 | 3 | smuggler 1 % |
| Forest | 8 | 4 | wolf 6 %, bear 2 %, brigand 2 % |
| Mountain | 12 | 6 | bear 3 %, rockfall 2 % |
| Peak | 18 | 10 | blizzard 5 %, ice wraith 1 % |
| Canyon | 8 | 2 | puma 3 %, rockfall 2 %, carnotaurus 2 % |
| Desert | 9 | 1 | hyena 3 %, sandstorm 3 %, giant monitor 2 % |
| Hot steppe | 11 | 2 | lion 3 %, hyena 3 %, triceratops 2 % |
| Jungle | 11 | 2 | jaguar 4 %, tiger 2 %, fever 3 %, crocodile 2 % |
| Cold steppe | 6 | 5 | white wolf 3 %, blizzard 2 % |
| Sea ice | 8 | 8 | white bear 3 %, thin ice 2 % |
| Snowfield | 13 | 9 | snow leopard 2 %, blizzard 4 % |
| Glacier | 20 | 14 | crevasse 4 %, blizzard 5 % |
| *Sea* | *10* | *8* | *storm 3 %, pirate 2 %* |
| *Ocean* | *14* | *16* | *leviathan 1 %, storm 4 %* |

**Example.** A route of 3 forest cells and 2 mountain cells costs 3 × 8 + 2 × 12 = 48 days and
3 × 4 + 2 × 6 = 24 vital heat.

## 5. Encounters

An encounter is a hazard met on the way: a creature, a person or an event such as a rockfall.

**Rules**

1. Each hazard's chance is the separate probability that it happens when **one** cell of that biome is crossed.
2. The hazards of a cell are checked in table order, and the first that happens is the only one met in that cell.
3. Over a whole route, the chance of meeting a given hazard is 1 − the product of (1 − p) over every cell of the route, where p is its chance per cell.
4. The chance of meeting anything at all is the same product taken over every hazard.
5. A short crossing of forest is a modest risk; a long one is not.
6. The nose lowers the chances: its **Encounter Avoidance**, a value derived from the nose score ([Chapter I §6](01-anatomy.md#6-derived-values)), is 10 % per point of nose score.
7. Every per-cell chance is multiplied by (1 − Encounter Avoidance) before the product is taken.
8. An encounter stops the journey in the cell where it happens.

| Nose score | 0 | 1 | 2 | 3 |
|---|---|---|---|---|
| Encounter Avoidance | 0 % | 10 % | 20 % | 30 % |
| Forest wolf chance per cell (6 %) | 6 % | 5.4 % | 4.8 % | 4.2 % |

## 6. Paying heat, and starvation

The vital heat a route costs is paid while walking, not on arrival.

**Rules**

1. Each cell entered adds its vital heat cost to a running debt.
2. When the debt reaches 1 or more, the character stops where they stand until it is paid.
3. The debt is paid by consuming humors one at a time, in rotation: hepar (liver), paunch, pulmones (lungs), spleen, then hepar again (see [Chapter III](03-humors.md)).
4. Each humor consumed pays its own vital heat off the debt.
5. A queue that is **critical** (holding nothing but black bile, the humor that carries no heat) is skipped in the rotation.
6. A humor of 0 heat pays nothing; the queue still moves on.
7. A humor of **negative** heat pays nothing and puts back on the debt up to its heat in what was already paid for the current stretch.
8. A body carrying poison or intoxication therefore pays for its journey twice.
9. When the debt is paid, walking resumes, and the next cells open a new debt.
10. When every queue is critical, the debt cannot be paid and the character **dies of starvation**.
11. Starvation is the usual way a body whose humors have soured is killed.
12. The forecast of a route says whether the humor queues as they now stand would carry the character to its end.
13. Reason for the forecast: the reserve is drawn down all the way, so a body can set out with enough and run out halfway.
14. Consuming humors moves the queues the whole way, so the character arrives with different humors at the back of each queue; a long journey changes what can be spent on the first roll at the destination.

## 7. The calendar

The world counts time in **days**, and every span of time is given in days.

**Rules**

1. The calendar moves on only at three moments.
2. When a journey ends, it moves on by the journey's whole duration.
3. When a stint of hired work ends, it moves on by the days worked ([Chapter VII §5](07-social.md#5-hired-work)).
4. During a visit, it moves on by 1 day each time the period passes from night to dawn (§8).
5. Narration, conversation and combat cost no days; a day can hold any number of them.
6. The calendar moves on **before** the destination is built, so anything there that depends on the date uses the date after the journey.

### On returning to the map

Two checks are made, in this order, when the character returns to the map.

1. **Wounds are checked for healing**: those that have lasted their healing time close, and the health they cost is restored.
2. **Age is checked**: a body that has reached its lifetime (a heart-derived value, see [Chapter I §8](01-anatomy.md#8-age-and-lifespan)) dies of old age.

**Rules**

1. If the protagonist dies of old age here, the run ends.
2. A companion's death of age is caught by the standing check of §9, which can happen at any moment.
3. Healing comes first because a heart wound shortens lifetime; a heart wound that closed during the journey must not still count against lifetime a moment later.
4. A day passed during a visit triggers neither check; both wait for the next return to the map.
5. These two checks are all the map settles.
6. Wounds and spent humors are not waited for here; both are checked at any moment, wherever the body is ([Chapter I §7](01-anatomy.md#7-wounds)).
7. Age is the only cause of death tied to a fixed moment, because the calendar itself moves only at fixed moments.

## 8. The periods of the day

A day is divided into six **periods**: dawn, morning, noon, afternoon, evening and night. After night
comes dawn again.

### How the period moves

**Rules**

1. Each place has its own **hour of arrival**, fixed once and for all; it is always one of the 5 daylight periods, never night.
2. Every visit to a place opens at its hour of arrival.
3. A routine is the exception: it opens at the period it was learned in ([Chapter IX](09-routines.md)).
4. After arrival, the period moves only through acts that wait and through failures that waste time ([Chapter VI](06-narration.md)).
5. A successful wait (sitting out an interval) lasts until whichever of the other 5 periods the character chooses.
6. When a will (a modus mentis that desires particular things; see [Chapter II §1](02-memory.md#1-what-a-modus-mentis-is)) proposed the wait as a step of its course, the wait lasts until the period the will needs, and nothing is chosen.
7. An act that waits for something to happen moves the period forward one at a time, until the thing happens or the day runs out.
8. A failure that wastes time moves the period forward by 1.
9. Whatever moves it, passing from night to dawn moves the calendar on by 1 day (§7).
10. A place's other periods can be reached only by waiting in it, or by a routine.

### Seats and hiding

**Rules**

1. Sitting out an interval needs a seat.
2. Nearly every place has 1 to 3 seats in its open ground, such as a stump, a flat rock or a bench against a wall.
3. Indoors, any bench, stool, barrel or crate is a seat, so a store, a cellar or a chapel is as good for waiting as a square.
4. A room without a seat, as most bedrooms and every passage are, allows no sitting out.
5. Hiding needs a hiding place instead of a seat.

### Night in someone else's house

**Rules**

1. This applies when the character is trespassing in a building that someone lives in and comes home to.
2. A wait there cannot run through the night.
3. A sitting out, or a failure that wastes time, that brings the period to night also brings the householder: they enter the room and find the character.
4. A wait chosen to last past night stops at night instead.
5. What follows is in [Chapter VI §7](06-narration.md#7-crime-and-being-seen).
6. Hiding brings nobody, since hiding is meant to avoid exactly that.

### What the period decides

**Rules**

1. Where each inhabitant of a place stands is decided by their schedule and the current period.
2. A building's entrance is shut at night.
3. What can be attempted in a place follows from both.
4. A settled place is never empty at its hour of arrival while someone can be spared to be there.
5. At that hour, someone is in the area the visit opens in: an inhabitant taken from the open ground, a private room, a shared hall or counter, or wherever they were away to.
6. That person is never the only keeper of a counter or a store.
7. That person is never taken so as to leave a household's hall unattended for a second period of the day.
8. A place of 1 or 2 inhabitants, each the only keeper of something, can therefore still be found empty.
9. The wild is not bound by rule 4.

## 9. Companions and their loss

The number of companions is limited by the heart.

**Rules**

1. The party holds at most 1 companion per point of heart score; this is the **Max Companions** value derived from the heart ([Chapter I §6](01-anatomy.md#6-derived-values)).
2. The limit is checked before a destination can be chosen.
3. A party over the limit must dismiss companions before it can travel.
4. A protagonist accepted straight from character creation has a heart score of 1, and so can keep 1 companion.
5. A companion dies the same three ways a protagonist does, listed in the table.
6. Death is checked at every moment from the state of the body, so a companion who dies leaves the party at once, not on the next return to the map.
7. A fight is the one exception: it holds the whole party until it ends, and a companion who falls in it leaves the party when it ends ([Chapter VIII §9](08-fighting.md#9-the-end-of-a-fight)).
8. A companion's fall does not end a fight.
9. Losing a companion is **permanent**: they leave the party, no longer count against the limit, and nothing brings them back.
10. A companion who dies inside a place leaves a body in the area where they fell, like any other death ([Chapter VI §11](06-narration.md#11-what-a-killing-leaves)).
11. A beast's body leaves what can be cut from it; a person's leaves what they carried.
12. A companion who dies on the map, between places, leaves no body, since there is no area to hold one.

| Cause of death | When it happens |
|---|---|
| Wounds | the wounds borne use up all the health the body has ([Chapter I §7](01-anatomy.md#7-wounds)) |
| Humors | every queue is critical, so effort can no longer be paid for ([Chapter III §5](03-humors.md#5-black-bile)) |
| Age | the days lived reach the lifetime the heart sets ([Chapter I §8](01-anatomy.md#8-age-and-lifespan)) |

## 10. Arrival

Arriving at a cell enters it.

**Rules**

1. If the cell has a location, the location is built and its narration begins.
2. If the cell has only a biome, the biome is entered instead.
3. There is no separate act of entering a place.
4. A place the character already stands on cannot be entered again without leaving first.
5. What remains from an earlier visit is described in [Chapter VI §9](06-narration.md#9-what-a-place-remembers).

## Terms

| Term | Meaning |
|---|---|
| Biome | The type of ground of a cell, such as plain or forest; it sets the cell's travel cost and hazards |
| Barren ground | Desert, canyon, glacier and sea ice: never settled, holding only isolated historical places |
| Calendar | The count of days; it moves only when a journey or hired work ends, or when night turns to dawn |
| Cell | One unit of the world sphere |
| Companion | A party member other than the protagonist |
| Critical | A humor queue holding nothing but black bile |
| Disposition | One of 20 styles of world, setting water, relief, cover, feature size, climate and settlement density |
| Encounter | A hazard met on the way, which stops the journey |
| Encounter Avoidance | Nose-derived value: 10 % per point of nose score, cutting every hazard's chance |
| Hour of arrival | The fixed daylight period at which every visit to a place opens |
| Humor | A unit in one of the body's four queues, carrying vital heat |
| Livable ground | Plain, mountain, hot steppe, cold steppe, jungle and snowfield |
| Location | A place built on a cell, such as a village or a castle |
| Max Companions | Heart-derived value: 1 companion per point of heart score |
| Max Travel Distance | Lower-limbs-derived value: 4.0 to 14.8 units, the radius within which a destination can be set |
| Moon | One of 383 in the sky; each names one world |
| Period | One of six parts of the day: dawn, morning, noon, afternoon, evening, night |
| Score | An organ's or body region's number, the sum of its parts ([Chapter I](01-anatomy.md)) |
| Settled country | City, farmland, settlement and stock cells spread around historical places |
| Starvation | Death when a heat debt is owed and every humor queue is critical |
| Vital heat | The energy humors carry, spent on travel and effort |
| Waypoint | A cell the route must pass through; up to 4 per journey |

---

Previous: [IV. Items](04-items.md) · Next: [VI. Narration](06-narration.md)
