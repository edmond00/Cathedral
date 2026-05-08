# sections

- **Shore** — where land meets sea; beach or rocky foreshore
- **Clifftop** (rocky coast variant only) — elevated edge above the water
- Variant: **Estuary** replaces Shore in ~20% of coasts (river mouth, brackish water, different ecology)

# areas

Pick 3–5 from:
- Sandy Beach (open, gentle, easy access)
- Rocky Shore (barnacled, uneven, tide pools)
- Cliff Base (dramatic, wet from spray)
- Cliff Top (exposed, high view)
- Tide Pool Zone (rocky shore detail, rich in small life)
- Estuary Flat (muddy, wading birds, different fish — estuary variant only)

# spots

**Shore finds:**
- Driftwood Pile → Driftwood ×2, Rope Fragment
- Kelp Bed → Seaweed ×2
- Tide Pool → Crab, Mussel, Shell, Stone
- Stranded Net → Net (damaged), Fish (herring or mackerel)

**Cliff:**
- Cliff Face (preexisting — CLIMB_UP / CLIMB_DOWN connection between Cliff Base and Cliff Top)
- Seabird Nest (on ledge) → Egg ×1, Feather ×2
- Rock Crevice → Shell, Flint

**Camp (Sandy Beach — present when fisherman is in residence):**
- Bedroll → (rest spot)
- Drying Frame → Fish ×2 (drying catch)
- Net Pile → Net (repaired/folded)

**Estuary:**
- Mud Flat → Clay ×2, Reed
- Willow Bank → Branch, Bark

# roads/doors

Paths and connections within the coast:
- Shore Path: Sandy Beach ↔ Rocky Shore
- Shore Path: Rocky Shore ↔ Tide Pool Zone
- Shore Path: Sandy Beach ↔ Cliff Base
- Cliff: Cliff Base ↔ Cliff Top (CLIMB_UP / CLIMB_DOWN)
- Estuary Track: Estuary Flat ↔ Sandy Beach (estuary variant)

# npc

**Human (rare — ~30% chance a fisherman is present):**

**Fisherman** ×0–1
- When present, follows a week schedule (not yet implemented): ~3 days at coast, ~2 days at nearest village to sell fish and resupply
- Day schedule while at coast:
  - Dawn → Sandy Beach (launch boat, set nets — player sees boat departing or absent)
  - Morning → (at sea, absent from location)
  - Afternoon → Rocky Shore or Sandy Beach (returning, sorting catch)
  - Evening → Sandy Beach (camp, dry nets, eat)
  - Night → Sandy Beach (sleeps at camp)
- Carries: Net, Fishing Line, Hook, Basket, Knife

**Beast:**
- Seal (non-hostile, rare — 20%, rests on rocks)

**Shallow:**
- Seagull (always present)
- Heron (1–2, wading at water's edge)
- Crab (Tide Pool zone)
- Sandpiper / Turnstone (estuary variant)

# items

**Fish (primary output — type varies by coast):**
- Herring, Cod, Mackerel (one dominant type per coast)
- Crab, Mussel

**Shore materials:**
- Seaweed, Driftwood, Shell, Rope Fragment, Stone, Flint
- Clay, Reed (estuary only)

**Fisherman tools (carried):**
- Net, Fishing Line, Hook, Basket, Knife (knife from village forge)

**Animal drops:**
- Feather (seabirds), Seal Pelt (rare)

# comments

**RNG rules:**
- Coast identity: sandy beach, rocky shore, or clifftop — affects section pool and area selection
- Sandy: more driftwood, easy access, fewer shellfish
- Rocky: tide pools, more shellfish, harder to move through
- Clifftop: adds Cliff Top area and CLIMB connection
- Estuary variant (20%): different fish (perch, eel), more clay/reed, wading birds instead of seagulls
- Fish species assigned at world level: herring (northern/cold coast), cod (open coast), mackerel (warmer coast)
- Fisherman absent most of the time (70%); camp may remain with drying fish and net pile
- Seal is rare and noteworthy — treat as a discovery moment

**Economy connections:**
- Fish → trade to village (food supply alongside farm produce)
- Knife carried by fisherman originates from Village Forge
- Driftwood minor supplement to timber supply
- Clay (estuary) → potential future use (pottery)
