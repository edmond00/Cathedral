# sections

- **Farmyard** — central mud yard, animal sounds, main activity hub
- **Farm Grounds** — working land: garden, orchard, enclosures
- **Farmhouse** — interior rooms (built by HouseBuilder)

# areas

- Courtyard (hub, always present)
- Sheep Pen
- Pigsty
- Chicken Coop
- Dairy Shed
- Vegetable Garden
- Orchard
- Storage Shed
- Hall (communal living, eating)
- Kitchen
- Pantry (optional)
- Bedroom ×1–3

# spots

**Sheep Pen:** Trough → Hay; Shearing Post → Wool, Shears

**Pigsty:** Trough → Kitchen Scraps

**Chicken Coop:** Nest Box → Egg ×2, Feather ×3

**Dairy Shed:** Churn → Butter; Mold Rack → Cheese; Pail → Milk

**Vegetable Garden:** Beds → Turnip, Radish, Carrot, Onion, Leek, Cabbage (2–3 types per farm)

**Orchard:** Fruit Tree → Apple *or* Pear *or* Plum *or* Cherry (1–2 species per farm)

**Storage Shed:**
- Hay Stack → Hay ×2, Straw
- Grain Sack → Grain ×2
- Tool Rack → Sickle, Hatchet, Rope
- Barrel → Ale *or* Preserved Vegetable

**Hall:**
- Hearth (preexisting)
- Long Table → Bread, Mug

**Kitchen:**
- Pot (preexisting)
- Pantry Shelf → Flour, Salt, Dried Herb

**Bedroom:**
- Bed ×1–2 (preexisting)
- Chest → Cloth, Coin

# roads/doors

Paths and doors connecting areas within the farm:
- Farmyard Track: Courtyard ↔ Chicken Coop
- Farmyard Track: Courtyard ↔ Pigsty
- Farmyard Track: Courtyard ↔ Sheep Pen
- Farmyard Track: Courtyard ↔ Storage Shed
- Garden Path: Courtyard ↔ Vegetable Garden
- Garden Path: Courtyard ↔ Orchard
- Path: Dairy Shed ↔ Courtyard
- Door (main): Courtyard → Hall (locked at night)
- Door: Hall ↔ Kitchen
- Door: Kitchen ↔ Pantry
- Door: Hall ↔ Bedroom (×per bedroom)
- Farmyard Track: Courtyard ↔ Dairy Shed

# npc

**Farmer** (reeve/owner) ×1
- Dawn→Courtyard (survey); Morning→Garden or Sheep Pen; Noon→Kitchen (eat); Afternoon→Orchard or Courtyard; Evening→Hall (eat, rest); Night→Bedroom

**Farmhand** ×1–3
- Farmhand 1 — Dawn→Chicken Coop (collect eggs, feed); Morning→Pigsty; Noon→Hall (eat); Afternoon→Garden; Evening→Hall; Night→Bedroom
- Farmhand 2 — Dawn→Courtyard; Morning→Storage Shed (maintenance); Noon→Hall; Afternoon→Orchard; Evening→Hall; Night→Bedroom

**Shepherd** ×1 (if Sheep Pen present)
- Dawn→Sheep Pen; Morning→Sheep Pen; Noon→Courtyard (eat packed meal); Afternoon→Sheep Pen; Evening→Hall; Night→Bedroom

**Dairymaid / Cowherd** ×1 (if Dairy Shed present)
- Dawn→Dairy Shed (milking); Morning→Dairy Shed (butter, cheese); Noon→Kitchen; Afternoon→Dairy Shed; Evening→Hall; Night→Bedroom

**Swineherd** ×1
- Dawn→Pigsty; Morning→Courtyard edge (pigs rooting); Noon→Courtyard; Afternoon→Pigsty; Evening→Hall; Night→Bedroom

**Poultry Keeper** ×1 (or merged with Farmhand)
- Dawn→Chicken Coop; Morning→Orchard edge; Noon→Hall; Afternoon→Chicken Coop; Evening→Hall; Night→Bedroom

**Animals (shallow NPC):**
- Sheep ×2–6
- Pig ×1–3
- Chicken ×3–7
- Cow ×1–2

# items

**Livestock products:**
- Wool (sheep → weaver), Milk (cow → dairy), Butter, Cheese, Egg
- ChickenMeat, ChickenFeather, PorkMeat, MuttonMeat

**Produce:**
- Apple, Pear, Plum, Cherry (orchard — 1–2 types)
- Turnip, Radish, Carrot, Onion, Leek, Cabbage (garden)
- Grain, Hay, Straw, Flour, Bread, Ale, Salt

**Tools present (made in village forge):**
- Sickle, Hatchet, Rope, Shears

# comments

**RNG rules:**
- Each farm picks one fruit tree species for orchard (apple, pear, plum, or cherry)
- Sheep Pen present in ~60% of farms (needed for wool economy)
- Dairy Shed present if cow(s) present
- Garden vegetable mix draws 2–3 types from the list (always includes at least one root vegetable)
- Farmhouse size: 1-storey (1 bedroom, simple) or 2-storey (2–3 bedrooms, prosperous) — affects NPC capacity
- Swineherd may be absent on smaller farms; pig still present but roams farmyard
- Farmhand count scales with farm prosperity (1 on poor farms, up to 3 on large ones)

**Economy connections:**
- Wool (farm) → Village Weaver → Cloth
- Milk (farm) → Dairy Shed → Butter, Cheese
- Grain (from field, stored in shed) → Village Mill → Flour
- Shears, Hatchet, Sickle → sourced from Village Forge
