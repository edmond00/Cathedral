# sections

- **Forest Edge** — lighter canopy, more undergrowth, visible sky
- **Deep Wood** — dense canopy, darker, quieter, larger trees

# areas

Pick 3–5 from:
- Clearing (open space within wood — also serves as camp site for the rare woodcutter)
- Thicket (dense undergrowth)
- Old Growth (massive ancient trees, sparse undergrowth)
- Streamside (follows a creek, mud banks)
- Deadwood Patch (fallen and rotting trees)
- Slope Section (forest climbing a hillside)

# spots

**Living Trees (1–3 per area, species set by forest type):**
- Oak → Acorn, Branch, Bark
- Beech → Beechnut, Branch, Bark
- Ash → Branch, Bark
- Birch → Branch, Bark, Birch Sap
- Pine → Branch, Bark, Pine Cone, Pine Needle, Pine Sap
- Yew → Branch, Bark (occasional)

**Cut/Fallen Wood:**
- Felled Log → Log ×2, Bark
- Tree Stump → Mushroom, Moss
- Deadfall Pile → Branch ×2, Twig

**Ground:**
- Undergrowth Patch → Fern, Ivy, Nettle, Bramble (pick 2)
- Mushroom Cluster → Mushroom ×2
- Stream Bank → Clay, Watercress, Stone
- Moss Bank → Moss ×2

**Camp (Clearing only — present when woodcutter is in residence):**
- Bedroll → (rest spot)
- Fire Pit → Ash, Coal (remnant)
- Sack → Log, Bark (gathered that day)

# roads/doors

Paths connecting areas within the forest:
- Forest Track: Clearing ↔ Thicket
- Forest Track: Clearing ↔ Old Growth
- Forest Track: Old Growth ↔ Deadwood Patch
- Stream Path: Streamside ↔ Clearing
- Stream Path: Streamside ↔ Slope Section
- Narrow Path: Thicket ↔ Slope Section

# npc

**Human (rare — ~30% chance any woodcutter is present):**

**Woodcutter** ×0–1
- When present, follows a week schedule (not yet implemented): ~3 days in forest, ~2 days at nearest village to sell timber and resupply
- Day schedule while in forest:
  - Morning → Old Growth or Thicket (felling, chopping)
  - Afternoon → Deadwood Patch or Clearing (gathering, dragging logs)
  - Evening → Clearing (camp, rest, eat)
  - Night → Clearing (sleeps rough at camp)
- Carries: Axe (from village forge), Rope, Sack

**Charcoal Burner** ×0–1 (10% chance — independent of woodcutter)
- Day schedule similar to woodcutter but tied to smouldering mound in Clearing
- Week schedule: same pattern as woodcutter (forest/village rotation)

**Beast:**
- Boar (hostile, 40%)
- Deer (shy, 50%)
- Wolf (hostile, 20%)
- Badger (non-hostile, 30%)

**Shallow:**
- Squirrel, Wood Mouse, Robin, Woodpecker, Owl (always some present)

# items

**Wood (primary economy output):**
- Log, Branch, Twig, Plank (rough-hewn by woodcutter)
- Bark, Birch Sap, Pine Sap, Pine Cone, Acorn, Beechnut

**Flora:**
- Mushroom, Moss, Fern, Ivy, Nettle, Bramble, Watercress, Clay

**Woodcutter carried:**
- Axe, Rope, Sack

**Charcoal Burner (if present):**
- Coal (output), Log (input)

**Animal drops:**
- Deer Hide, Deer Antler, Boar Tusk, Wolf Pelt, Feather

# comments

**RNG rules:**
- Forest identity: deciduous (oak/beech/ash dominant) vs coniferous (pine dominant) vs mixed — drives all tree spawns
- Deep Wood section always gets Old Growth or Thicket; Edge gets Clearing or Slope
- Woodcutter: absent most of the time (70%); when present, leaves clear signs (felled logs, stump, camp)
- Charcoal Burner (10%): independent of woodcutter spawn, adds Coal to world supply; smouldering mound prop in Clearing
- Stream present in ~50% of forests (drives Streamside area and Clay availability)
- No human NPCs by default — the forest is wild; human presence is the exception

**Economy connections:**
- Log/Plank → Village Carpenter → Barrel, Beam, Plow Handle
- Coal (charcoal burner) → Village Forge (fuel for smelting)
- Axe carried by woodcutter originates from Village Forge
