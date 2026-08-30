# sections

- **Cave Mouth** — entrance zone, daylight reach, relatively safe
- **Tunnel Network** — deeper passages, dark, uneven ground

# areas

Pick 3–5 from:
- Entrance Hall (always present — lit by daylight; also serves as camp when miner is in residence)
- Main Shaft (primary dig passage)
- Ore Chamber (iron ore deposit — always present in iron-rich cave)
- Coal Seam (separate chamber — only in coal-bearing caves)
- Underground Pool (water seepage, dark)
- Collapsed Tunnel (dead end, rubble)
- Side Alcove (small offshoot, minor finds)

# spots

**Mining targets:**
- Ore Vein → Iron Ore ×3 (Ore Chamber)
- Coal Seam Deposit → Coal ×2 (Coal Seam area)
- Rock Face → Stone ×2, Flint

**Water:**
- Underground Pool → Stone, Clay

**Camp (Entrance Hall — present when miner is in residence):**
- Bedroll → (rest spot)
- Lantern Hook → Lantern
- Ore Pile → Iron Ore ×1–2 (staged for transport to village)

**Other:**
- Bat Roost (spot, no items — shallow NPC marker)
- Rubble Pile → Stone, Flint (Collapsed Tunnel)
- Tool Cache → Pick, Shovel, Rope

# roads/doors

Paths and connections within the cave:
- Passage: Entrance Hall ↔ Main Shaft
- Passage: Main Shaft ↔ Ore Chamber
- Passage: Main Shaft ↔ Side Alcove
- Passage: Main Shaft ↔ Coal Seam (coal-bearing variant only)
- Flooded Passage: Main Shaft ↔ Underground Pool
- Mineshaft Ladder: Entrance Hall → deeper Main Shaft (CLIMB_DOWN / CLIMB_UP verb)

# npc

**Human (rare — ~25% chance a miner is present):**

**Miner** ×0–1
- When present, follows a week schedule (not yet implemented): ~3 days in cave, ~2 days at nearest village to sell ore and resupply
- Day schedule while in cave:
  - Morning → Ore Chamber (digging, picking)
  - Afternoon → Ore Chamber or Main Shaft (sorting, hauling)
  - Evening → Entrance Hall (sorting ore, eating)
  - Night → Entrance Hall (sleeps at camp, near lantern light)
- Carries: Pick, Shovel, Sack, Lantern

**Beast:**
- Cave Spider (hostile, rare — 15%)

**Shallow:**
- Rat (common in Entrance Hall / Main Shaft)
- Bat (common in deep areas)

# items

**Primary outputs (economy):**
- Iron Ore → Village Forge → Iron Bar → tools
- Coal → Village Forge (fuel)

**Stone/mineral:**
- Stone, Flint, Clay

**Miner tools (carried or cached):**
- Pick (from village forge), Shovel (from village forge), Rope, Sack, Lantern

**Animal drops:**
- Rat Pelt (shallow)

# comments

**RNG rules:**
- Cave type: iron-rich (most common), stone-quarry (no ore vein, more Rock/Flint output), coal-bearing (coal seam added)
- Iron-rich: Ore Chamber always present, Coal Seam 20% chance
- Stone-quarry: different economy role — supplies stone for building rather than ore for smelting
- Underground Pool present in ~40% of caves
- Collapsed Tunnel adds atmosphere; 30% chance
- Miner absent most of the time (75%); signs of activity (ore pile, tool cache) may remain even when absent
- Lantern required for deep areas — Entrance Hall is the only lit area by default

**Economy connections:**
- Iron Ore → Village Forge → Iron Bar → Axe, Saw, Pick, Sickle, Shovel, Nail, Horseshoe, etc.
- Coal → Village Forge (fuel, alongside charcoal from forest)
- Pick and Shovel are forged in village, used back in cave — traceable closed loop
