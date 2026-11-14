# Narration on the location map

The agreed design of the refactor that replaces the scrolling text and its clickable keywords with
the location map. The draft was the developer's; the gaps were filled and each open point settled in
an interview (2026-11-14). Where a line says **decided**, it was asked and answered; the rest are the
defaults proposed with the questions and not objected to.

This is a design note, not the manual. The manual chapters (VI, V, IX, VII) are rewritten from the
code once the work is done.

## 1. The screen

- The location map is the narration screen. There is no scrolling text, no map/text switch (Tab and
  the header's ↹ go), and no footer button.
- The header keeps the acting member's name, their noetic points and the trespass marker.
- The generation preview box stays: every generated text is first read there (CONTINUE, or YES / NO
  for a doubt). On CONTINUE it becomes a **symbol on a tree**; hovering that symbol shows the text
  again.
- The mind popup stays (which modus mentis). *What* is done is decided by the symbol clicked, so the
  Think / Observe / Speak About menu goes. The act popup (Execute / Use Tool / Reconsider) stays.
- Conversations (`GameMode.Dialogue`) keep their own scrolling text screen. The journal is fed from
  tree nodes instead of from the scroll buffer's blocks.
- `DebugLocationMap` is renamed: it is no longer a debug view.

### Symbols

One symbol per element, of four kinds, so the player knows what a click will look at:

| kind | what |
|---|---|
| way | a neighbouring area, a door, a stair, a path, a climb, a crossing, a landscape |
| person | a living thing that does not count the acting member an enemy (shallow wildlife included) |
| hostile | one that does |
| thing | every other point of interest (corpses, sleepers, containers), and items disclosed by one |

Further symbols: a party token per member, the exit, *think*, *speak*, a generated text, an act
waiting, a success, a failure.

### An element's three states

- **unknown** — an anonymous symbol of its kind in the card's symbol row. Hover says "observe". A
  click asks the observing mind to choose among the unknown elements of that kind in that area.
- **known** — remembered from an earlier visit, or disclosed by a container. Named on hover. A click
  observes *that* element. It must still be observed to root a tree.
- **observed** — the root of a tree for this member, this visit.

Known marks live in the place's `MentalMap` and are forgotten with it. A person is known by role once
observed and by name once introduced (the affinity rule, unchanged).

### Trees — **decided: hang under the card, always unfolded, kept in full**

- The card keeps one row of symbols for what is not yet observed. An observed element leaves that
  row and drops under the card as a root; its tree grows downward, drawn over the map.
- The map's layout is fixed during a visit; the rows between areas are widened so most trees fit.
- Every tree of every known area is always unfolded. A finished branch (act succeeded, failed, or
  refused) stays where it grew, dimmed, until the location is left.
- Lines join every clickable symbol to its root: root → text → act → outcome, a refusal as a failure
  symbol, and a *think* stub while the member can still think about it.
- Trees are **private to a member**: the map shows the trees of the member being played.
- Trees are never saved and never kept between visits.

## 2. Observation

- One click, one element, one text. No bearings, no overall description, no follow-up objects, no
  focus transition, no keyword extraction, no "arrival names somebody".
- Groups go (families of three, door-and-wall wholes). Containers stay.
- **Every observation costs one noetic point.** There is no free opening observation.
- A click on an anonymous symbol: the mind picks its best fit among the unknown elements of that
  kind there, or declines the kind. The symbol then belongs to that element.
- **Decided: a mind may decline once.** A decline (of a kind, or of a named element) spends the
  point and leaves a failure symbol with its words. Asked again about the same thing, that mind must
  observe. A speaking mind never declines a person; a will keeps its own rule (it takes what begins
  its course and otherwise says what it wants). The two opening phases never decline.
- **Decided: a container's contents are disclosed as *known* children**, linked to it. Each needs
  its own observation before it can be thought about.
- **Decided: thinking on a container proposes its own acts and its contents' acts**; thinking on a
  child proposes only that item's. A corpse is a container like any other.
- Observing a way marks the area beyond as seen, as today.

## 3. Thinking and acting

- The logic is unchanged: goal → means → reasoning → doubt → willingness → act, the retries, the
  choice rules, Reconsider, Use Tool, the action rules at the click, the dice, the failure ladder,
  the sentiment.
- The dice pool is still the chain observing mind → thinking mind → acting mind (+ implement). A
  root nobody observed (a corpse added by a killing) contributes no observing mind.
- **Thinking needs no presence**: a member may think about an observed element in any area.
- **Observation and action need presence**: the member walks there first (§5).
- **Decided: the noetic pool refills per act.** A member's pool refills when one of their acts
  succeeds and not when it fails. Acts waiting on other trees stay clickable afterwards unless the
  change made them impossible. An act is not proposed again while its symbol is still waiting; the
  refused-goal and declined-object rules keep their meaning per member for the visit, cleared for a
  member when their pool refills.
- At zero points a member can still execute waiting acts, walk, leave, or be switched away from.

## 4. The exit

- The exit symbol is LEAVE; hovering it reads LEAVE or RUNAWAY by the acting member's situation.
- LEAVE walks **every** member out from where they stand, by known ways. It is stopped as any walk
  is (§5).
- RUNAWAY is unchanged: the feet's roll, one six, checked again at every area on the way where
  somebody would stop the member.

## 5. Movement and confrontation

- **Go Back is deleted.** Walking by known ways is free and instant, never through a door locked
  against the member and never from other ground into private. A far symbol with no known way to it
  is dimmed and its hover says so.
- A move along a way not yet taken is still an act (a thought on the way's symbol, a roll).
- **Decided: the lessons Go Back taught** (Retracing, Backtrail for a beast; Withdrawal with an
  enemy in sight, Nightwalking by night, Houseways on private ground) are taught by a move act along
  a way not yet taken that arrives in an area already visited.
- **Decided: a walk that enters an area where an enemy or a witness would confront the member stops
  there, with no roll.** The intended observation or act is dropped, the member is confronted, and
  their exit reads RUNAWAY. The same holds for LEAVE.
- **Decided: while a member is confronted**, they may not observe or think anew. Symbols that answer
  the confronter and the exit stay lit. **The discreet exception is kept**: acts already waiting
  stay clickable; a non-discreet means is refused at the click as today, a discreet one may try.
  Members elsewhere are unaffected.

## 6. Time and state

One reconciler runs after every outcome and every change of hour, and keeps scene and trees
coherent:

- an observed NPC that moved to a known area takes their tree with them; to an unknown area, the
  tree is hidden until that area is discovered;
- a killed NPC loses their tree, and their corpse appears as an observed thing;
- an element that is gone loses its tree; a waiting act that is no longer possible is removed;
- the unknown symbols of every visited area are shown live (**decided: everything, live**).

The corpse opener, the threat opener and the "came to look" arrival no longer open a text: they are
what the map shows. Time is one clock for the whole party.

## 7. What a place remembers

- Known elements (above), beside the visited and seen areas and ways.
- The Meet routine is removed, old saves included. Go to, Buy, Sell, Work, Gather and Craft stay.

## 8. The party — **decided: fully independent**

- Each member has their own area and their own token. A move moves only the member who made it.
- The player switches member freely by clicking a token; switching costs nothing.
- A fight or a confrontation takes only the members standing in that area. The protagonist may be
  absent from a companion's fight.
- **Decided: speaking is a symbol on an observed root**, offered when the member holds a speaking
  mind and a companion stands in the same or an adjacent area. It costs the speaker a point and
  plants the spoken line as a root, for that element, on the listener's trees. The view stays on the
  speaker.
- Every member enters a location at its entry area.

## 9. Childhood and get-up

- **Decided: both play on the get-up map** (Under the Tree).
- Reminescence: only the "myself" token is interactive; the scenery is drawn dim and inert. A click
  on myself observes a fragment of the current memory, a thought follows, Remember carries on to the
  next memory. **Decided: the branches accumulate under myself**, and are cleared when get-up begins.
- Get-up: **decided: the tree scene, enriched** — myself carries the three states of exhaustion, and
  a few things of scenery can be observed and thought about. Every thought ends on Get Up.

## 10. Tests and tools

- `click keyword` goes; 226 of 278 scripts are rewritten against symbols, elements and acts named,
  never coordinates.
- The flags that pinned text behaviour (`--refuse-focus`, the group scripts) go or are redefined.
