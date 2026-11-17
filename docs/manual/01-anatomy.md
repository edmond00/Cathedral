# I. Anatomy

A character's body decides what it can do. Its organs carry scores, and almost every number in the
game is computed from one of those scores.

**In short**

1. The body has three levels: regions, organs and organ parts. Only organ parts store a score; an organ's score is the sum of its parts, and a region's score is the sum of its organs.
2. Each **derived value** (a number computed from the body, such as Health Points or Cinetic Points) is read from exactly one organ part, organ or region. All of them are listed in one table in §6.
3. A Low wound costs 1 health point. A Medium wound also takes 1 from the score of what it hits. A High wound switches what it hits off, and every value drawn from it falls to its worst value.
4. Current health is Health Points minus the number of wounds. The character dies when it reaches 0.
5. Humans can speak, use their hands finely and think abstractly. Beasts and reptiles can do none of these.
6. A protagonist starts with every organ part at 1 and spends 25 more points among them.

## 1. Regions, organs and organ parts

The body is divided into three levels, from largest to smallest.

**Rules**

1. A **region** is a major division of the body, such as the encephalon (brain) or the trunk.
2. A region is the unit a blow in a fight is aimed at.
3. An **organ** belongs to exactly one region.
4. An **organ part** belongs to exactly one organ. It is the smallest unit and the only one that stores a score.
5. An organ with a single part has the same name as that part. A paired organ, such as the eyes, has two parts (left and right).
6. **Organ score** = the sum of the scores of its parts.
7. **Region score** = the sum of the scores of its organs.
8. Maximum scores add up the same way: a region's maximum is the sum of every maximum below it.
9. An organ part's maximum score is 5, unless its kind sets another maximum, and a species can change it again (§4).
10. An organ part's score is always between 0 and its maximum.

## 2. The human body

The human body has 5 regions, 22 organs and 28 organ parts.

| Region | Maximum | Organs (maximum score of each part) |
|---|---|---|
| **Encephalon** | 23 | anamnesis 5 · cerebellum 5 · cerebrum 5 · hippocampus 5 · pineal gland 3 |
| **Visage** | 22 | eyes (left 3, right 3) · ears (left 3, right 3) · nose 3 · tongue 5 · teeth 2 |
| **Trunk** | 28 | backbone 4 · heart 5 · viscera 5 · hepar 3 · spleen 3 · paunch 3 · pulmones 3 · genitories 2 |
| **Upper limbs** | 10 | arms (left 2, right 2) · hands (left 3, right 3) |
| **Lower limbs** | 10 | legs (left 2, right 2) · feet (left 3, right 3) |

Each organ is used by a group of modi mentis (the character's learned abilities, see [Chapter II](02-memory.md)) and is the source of one or more derived values (§6). The body panel's organ descriptions call an organ's modi mentis its **disciplines**; the two words mean the same thing.

### What each organ does

The exact formulas are in §6.

| Organ | Region | What its score sets |
|---|---|---|
| **Anamnesis** (long-term memory) | encephalon | residual memory; how many places are remembered |
| **Cerebellum** | encephalon | procedural memory; how easily a fighting skill is learned during a fight |
| **Cerebrum** | encephalon | semantic memory |
| **Hippocampus** | encephalon | sensory memory |
| **Pineal gland** | encephalon | the experience cost of every level of every modus mentis; getting up after being knocked down |
| **Eyes** | visage | the reach of a ranged attack |
| **Ears** | visage | initiative, which sets turn order in a fight |
| **Nose** | visage | telling humors apart by smell; avoiding encounters while travelling |
| **Tongue** | visage | speech fluency |
| **Teeth** | visage | chewing hard food; few modi mentis use them |
| **Backbone** | trunk | how much can be carried; cinetic points (the action budget of a fight turn) |
| **Heart** | trunk | lifespan, youthfulness and the number of companions |
| **Viscera** (guts) | trunk | how fast wounds heal; how many humors can be spent on one roll |
| **Hepar** (liver), **spleen**, **paunch** (stomach), **pulmones** (lungs) | trunk | the four **sanguific organs**: each produces humors (bodily fluids) into its own queue, according to its score; see [Chapter III](03-humors.md) |
| **Genitories** | trunk | sex; the natural attack dice of a blow; nothing else |
| **Arms** | upper limbs | how heavy a thing each hand can hold (see [Chapter IV §3](04-items.md#3-anchors)) |
| **Hands** | upper limbs | how well implements are used; bonus dice when making things |
| **Legs** | lower limbs | movement speed in a fight |
| **Feet** | lower limbs | footing on dangerous ground; the dice for running away |

## 3. Animal bodies

Two other anatomies exist: the bestial anatomy for beasts and the reptilian anatomy for giant reptiles.

### The bestial body

**Rules**

1. The bestial body has 4 regions: encephalon, muzzle, trunk and limbs.
2. The bestial encephalon has **no pineal gland**.
3. Without a pineal gland, a beast cannot learn any modus mentis that uses it.
4. Without a pineal gland, every level costs a beast the full 12 experience points, and it gets up from a knockdown with 1 die.
5. The bestial trunk has **no genitories**, so every value drawn from them is missing for a beast.
6. The **snout** is the beast's nose, much more developed. The **fangs** are its teeth and its main weapon.
7. Names are shared wherever the bodies share a part: a beast's legs are *legs* and its trunk is *trunk*, just as for a human.
8. So a rule that checks an organ by name works for both bodies. A rule that depends on how the organ is built may not.

| Region | Organs |
|---|---|
| **Encephalon** | anamnesis · cerebellum · cerebrum · hippocampus |
| **Muzzle** | eyes · ears · snout · tongue · fangs |
| **Trunk** | backbone · heart · pulmones · viscera · paunch · hepar · spleen |
| **Limbs** | legs (fore and hind, left and right) · claws (fore and hind, left and right) |

### The reptilian body

**Rules**

1. The reptilian body is the bestial body with three changes. Everything else is the same: no pineal gland, no genitories, shared names.
2. It has **no claws**. A reptile cannot learn any modus mentis that uses the claws.
3. It has **horns**: every horn on the head counted together as one organ with one part. The horns are in the muzzle.
4. It has a **thagomizer**: the armed end of the tail (spikes or a club). It is in the trunk, because the tail continues the spine.
5. Horns and thagomizer are weapons in a fight and are used by modi mentis like any other organ.
6. A reptile can suffer every beast wound except those to the claws.
7. A reptile can also suffer four wounds of its own: a broken horn, a torn-off horn, a broken spike, and a severed tail, which is a High wound to the thagomizer.
8. Only the giant reptiles use this body. A lizard small enough to step on has no anatomy at all; it is one of the small creatures that do not fight.

| Region | Organs |
|---|---|
| **Encephalon** | anamnesis · cerebellum · cerebrum · hippocampus |
| **Muzzle** | eyes · ears · snout · tongue · fangs · **horns** |
| **Trunk** | backbone · heart · pulmones · viscera · paunch · hepar · spleen · **thagomizer** |
| **Limbs** | legs (fore and hind, left and right) |

## 4. Species

A **species** names one anatomy and can change the maximum score of individual organ parts.

**Rules**

1. A species is not an anatomy. It uses one anatomy and may change organ part maximums.
2. Humans are the only species with the human anatomy.
3. Wolf, bear, boar, dog, cat and fox all use the bestial anatomy. They differ only in their maximums.
4. For example, a wolf's fangs reach 5 where other species' fangs reach less, its forelegs reach 5, and its paunch reaches 3.
5. Reptile species differ the same way, mostly in their weapons. Where horns or a thagomizer are a species' real weapons, their maximum is 5. Where they are only bony bumps or a heavy tail, it can be as low as 1.
6. A two-legged reptile is not another anatomy. It is the reptilian body with its forelegs at a maximum of 1.
7. **Values are scaled against the body's own maximum, not a fixed number.** A creature whose heart maximum is lower than a human's still reaches the full range its heart allows. Its bar is shorter, not impossible to fill.

## 5. Capabilities

A **capability** is something an anatomy can do that its organs alone do not settle.

A man cannot learn a modus mentis that uses fangs, because he has none. But a wolf has a tongue and a cerebrum, so something else must stop it from arguing or picking a lock. That is what capabilities do.

**Rules**

1. **Speech**: holding a conversation with a person, in language, not just making noise. A beast can still howl and snarl, because those modi mentis do not require speech. It can never open a conversation.
2. **Handcraft**: fine use of the hands: implements, locks, knots, carrying, cutting. It is what separates a hand from a paw.
3. **Abstraction**: thinking that rests on human institutions rather than the senses: letters, numbers, law, coin, doctrine. A beast can track, remember and plan. It cannot count or read a charter.
4. The human anatomy has all three capabilities.
5. The bestial and reptilian anatomies have none.
6. An anatomy lists the capabilities it has. A modus mentis or an act lists the capabilities it requires.
7. Something that requires no capability, which is the usual case, is open to every anatomy.
8. A body can hold a modus mentis only if it has every organ the modus mentis uses *and* every capability it requires.
9. If either is missing, the modus mentis is refused outright. It is never held uselessly.

## 6. Derived values

A **derived value** is a number computed by a formula from the score of exactly one source: an organ part, an organ or a region.

**Rules**

1. Every derived value has one source and one formula, listed in the table below.
2. A body has every derived value whose source it has, and no others.
3. The score used is the **effective score**: the source's score minus 1 for each Medium wound that counts against it (§7).
4. A High wound that counts against the source switches it off.
5. If the source is missing from the body, switched off by a High wound, or its effective score is below 0, the value falls to its **worst value**.
6. For most values the worst value is the lowest one. For a duration or a cost, where lower is better, it is the highest one.
7. Every value is kept within its own minimum and maximum, shown in the formula column.
8. In the formulas, *score* means the source's effective score, and *÷* rounds down unless stated otherwise.

### The table of derived values

The last column gives the full range for a human, from a score of 0 to the source's human maximum (§2).

| Derived value | Source | Formula | Human range |
|---|---|---|---|
| **Noetic Points** | encephalon (region) | score ÷ 2 + 1 (minimum 1) | 1 to 12 |
| **Working Memory** | encephalon (region) | score + 2, between 1 and 25 | 2 to 25 |
| **Countenance** | visage (region) | score ÷ 5, in dice | 0 to 4 dice |
| **Health Points** | trunk (region) | score ÷ 3 + 1 (minimum 1) | 1 to 10 |
| **Natural Defence** | lower limbs (region); for a beast or reptile, its legs (organ) | score ÷ 3, in dice; for a beast or reptile, score ÷ 4 | 0 to 3 dice |
| **Max Travel Distance** | lower limbs (region) | 4.0 + 10.8 × score ÷ region maximum, in map units, rounded to 0.1 | 4.0 to 14.8 |
| **Residual Memory** | anamnesis | score × 4, between 1 and 20 | 1 to 20 |
| **Mental Maps** | anamnesis | score × 4 (minimum 0) | 0 to 20 |
| **Procedural Memory** | cerebellum | score × 4, between 1 and 20 | 1 to 20 |
| **Fight Learning** | cerebellum | score, in dice (minimum 1) | 1 to 5 dice |
| **Semantic Memory** | cerebrum | score × 4, between 1 and 20 | 1 to 20 |
| **Sensory Memory** | hippocampus | score × 4, between 1 and 20 | 1 to 20 |
| **Experience per Level** | pineal gland | 12 − (score − 1) × 3, between 6 and 12. **Lower is better** | 12 to 6 |
| **Knockdown Recovery** | pineal gland | score, in dice (minimum 1) | 1 to 3 dice |
| **Attack Range** | eyes | score, in cells (minimum 1) | 1 to 6 cells |
| **Initiative** | ears | score (minimum 1); a fighter's turn-order roll is 1 die + this | 1 to 6 |
| **Encounter Avoidance** | nose | score × 10 % | 0 % to 30 % |
| **Humor Recognition** | nose | number of humors told apart by smell: 0 at score 0, 1 at score 1, 3 at score 2, all at score 3 or more | 0 to all |
| **Speech Fluency** | tongue | score, between 0 and 5 | 0 to 5 |
| **Chewing Strength** | teeth | strong at any score above 0, weak at 0 | weak or strong |
| **Maximum Weight** | backbone | one of 5 tiers: 10, 50, 100, 150, 200 (see below) | 10 to 200 |
| **Cinetic Points** | backbone | score × 2 (minimum 1) | 1 to 8 |
| **Lifetime** | heart | 10 800 + 32 400 × score ÷ heart maximum, in days, rounded | 10 800 to 43 200 days |
| **Max Companions** | heart | score | 0 to 5 |
| **Youthfulness** | heart | score × 10 %, at most 50 % | 0 % to 50 % |
| **Wound Healing** | viscera | 1 000 − 900 × score ÷ viscera maximum, in days, rounded. **Lower is better** | 1 000 to 100 days |
| **Humor Modifier Limit** | viscera | score (minimum 1) | 1 to 5 |
| **Gender** | genitories | the sex set when the character was generated; if none was set, male at any score above 0, female at 0 | male or female |
| **Natural Attack** | genitories | score, in dice | 0 to 2 dice |
| **Tool Usage Proficiency** | hands | None at 0, Low at 1–2, Medium at 3–4, High at 5–6 | None to High |
| **Handiwork** | hands | score ÷ 2, in dice, at most 3 | 0 to 3 dice |
| **Right Hand Load**, **Left Hand Load** | right arm, left arm (organ parts) | heaviest thing that hand can hold: light at 0, medium at 1, heavy at 2 or more | light to heavy |
| **Move Speed** | legs | cells per cinetic point: 1, 2, 4, 6, 8 at scores 0, 1, 2, 3, 4 (minimum 1) | 1 to 8 |
| **Runaway Dice** | feet | score, in dice (minimum 1) | 1 to 6 dice |
| **Equilibrium** | feet | score (minimum 1) | 1 to 6 |

Each sanguific organ (hepar, spleen, paunch, pulmones) also gives 4 values: the percentage chance that it produces each of the four basic humors. They are given in [Chapter III](03-humors.md).

### What the values are used for

1. **Noetic Points**: the budget for thinking and acting in a scene (see [Chapter VI](06-narration.md)).
2. **Working, Residual, Procedural, Semantic and Sensory Memory**: the sizes of the five memory modules that hold modi mentis (see [Chapter II §5](02-memory.md)).
3. **Countenance**: dice added for appearance in a conversation check (see [Chapter VII §2](07-social.md)).
4. **Health Points**: the maximum health. It ignores wounds, because every wound is already subtracted from current health once (§7).
5. **Natural Defence**, **Natural Attack**, **Attack Range**, **Initiative**, **Cinetic Points**, **Move Speed**, **Equilibrium**, **Fight Learning**, **Knockdown Recovery** and **Runaway Dice**: fight values (see [Chapter VIII](08-fighting.md)). Equilibrium gives footing on dangerous ground. Runaway Dice is the pool for fleeing.
6. **Max Travel Distance**: how far one journey can reach (see [Chapter V §3](05-travel.md)).
7. **Mental Maps**: how many places are remembered (see [Chapter IX §1](09-routines.md)).
8. **Experience per Level**: the experience points one level of any modus mentis costs (see [Chapter II §4](02-memory.md)).
9. **Encounter Avoidance**: each map cell's chance of an encounter on a journey is reduced by this percentage (see [Chapter V §5](05-travel.md)).
10. **Speech Fluency**: the most replies offered at each turn of a conversation. Fewer are offered if fewer speaking modi mentis are held.
11. **Humor Modifier Limit**: the most humors that can be spent on one roll (see [Chapter III §4](03-humors.md)).
12. **Maximum Weight**: the burden that can be carried (see [Chapter IV §1](04-items.md)).
13. **Tool Usage Proficiency**: which implements the hands can make use of (see [Chapter IV §7](04-items.md)).
14. **Handiwork**: dice added to everything made at a station (see [Chapter X §6](10-crafting.md#6-at-the-station)).
15. **Hand Load**: the heaviest item each hand can hold (see [Chapter IV §3](04-items.md#3-anchors)).
16. **Lifetime**, **Youthfulness** and **Wound Healing**: see §7 and §8. Youthfulness also fills the humor queues of a new protagonist (§9).

### Values that scale with a maximum

Four values are scaled against the source's own maximum, so they differ by species. For a human:

| Score | Lifetime (heart, max 5) | Wound Healing (viscera, max 5) |
|---|---|---|
| 0 | 10 800 days | 1 000 days |
| 1 | 17 280 days | 820 days |
| 2 | 23 760 days | 640 days |
| 3 | 30 240 days | 460 days |
| 4 | 36 720 days | 280 days |
| 5 | 43 200 days | 100 days |

| Lower limbs score | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Max Travel Distance | 4.0 | 5.1 | 6.2 | 7.2 | 8.3 | 9.4 | 10.5 | 11.6 | 12.6 | 13.7 | 14.8 |

**Maximum Weight** picks a tier: tier number = score ÷ backbone maximum × 4, rounded to the nearest whole number (tier 0 is 10, tier 4 is 200). For a human, whose backbone maximum is 4:

| Backbone score | 0 | 1 | 2 | 3 | 4 |
|---|---|---|---|---|---|
| Maximum Weight | 10 | 50 | 100 | 150 | 200 |

### Contribution to the level limit

Every organ and every region also has one more value: its **contribution to the maximum level** of the modi mentis that use it.

**Rules**

1. An organ contributes from 0 to +3: 3 × score ÷ organ maximum, rounded to the nearest whole number.
2. A region contributes from 0 to +6: 6 × score ÷ region maximum, rounded to the nearest whole number.
3. These contributions set how high a modus mentis can be raised (see [Chapter II §3](02-memory.md#3-level-and-maximum-level)).
4. Unlike every other value, this one can go below 0. It is −1 when Medium wounds push the effective score below 0, and −2 when a High wound switches the source off (§7).

Example: a human heart at 3 out of 5 contributes 3 × 3 ÷ 5 = 1.8, rounded to +2.

## 7. Wounds

A **wound** is one injury on one body, of a fixed kind from the game's list of wounds.

### What a wound hits

**Rules**

1. A wound kind is shared by every body. A wound instance is one injury on one body, and only the instance records the day it was taken.
2. A wound hits an organ part, an organ, a whole region, or nothing in particular.
3. A wound that hits nothing in particular is a **wildcard** wound. The generic ones are the contusion, the cut and the puncture, left by a glancing blow.
4. Which generic wound a near miss leaves depends on the weapon: an edge cuts, a point punctures, a blunt weight leaves a contusion.
5. Two other wildcard wounds are **mishaps**: the **scrape** (the body was dragged along something rough) and the **strain** (the body overworked itself).
6. A failed act can leave a mishap (see [Chapter VI §8](06-narration.md#8-failure)). A blow never does.
7. A stumble on dangerous ground in a fight counts as a mishap and can leave one.

### Severity

| Severity | Effect on what it hits | Health cost |
|---|---|---|
| **Low** | none | 1 |
| **Medium** | −1 to the effective score | 1 |
| **High** | switches it off: every value drawn from it falls to its worst value | 1 |

**Rules**

1. Medium wounds add up: two Medium wounds on the same source give −2.
2. An effective score can fall below 0. Every value drawn from it then falls to its worst value.
3. A wildcard wound never changes a score, whatever its severity.
4. The contribution to the maximum level is not just lowered but reversed: −1 when Medium wounds push the effective score below 0, and −2 under a High wound. So a wound can make a modus mentis unusable, not just stop it improving (see [Chapter II §3](02-memory.md#3-level-and-maximum-level)).

### Which wounds count against a value

A wound counts against a derived value only if it hits the value's source, or a larger part that contains it, as follows:

| Value drawn from | Wounds that count against it |
|---|---|
| an organ part | wounds to that part, or to its whole organ |
| an organ | wounds to that organ, or to its whole region |
| a region | wounds to that whole region |

1. A wound to one part of a paired organ (one eye, one arm) does not count against a value drawn from the whole organ.
2. A wound to one organ does not count against a value drawn from its region.

### Health and death

**Rules**

1. **Current health = Health Points − the number of wounds.** Health is not a pool that is spent.
2. Every wound costs 1 health, whatever its severity.
3. The body dies when current health reaches 0.
4. Death by wounds is checked at every moment, wherever the body is. A wound can come from a blow in a fight or from a failed act. The fatal one kills the protagonist the instant it is taken. Nothing waits for the scene to end or for the return to the map.
5. A companion who takes a fatal wound is lost to the party at that instant (see [Chapter V §9](05-travel.md#9-companions-and-their-loss)).
6. Running out of humors is checked the same way. Bleeding and the effort of a fight draw on the humor queues, so they can empty at any moment. A body whose every queue is critical dies where it stands (see [Chapter III §5](03-humors.md#5-black-bile)).
7. Death by age is not checked this way. It is checked only on the return to the map, together with healing. Reason: the calendar moves only through journeys and work, so little time passes inside one visit.

### Wounds to the heart and viscera

1. Lifetime and Wound Healing use the effective score, so wounds to their organs change them.
2. A Medium wound to the **heart** lowers Lifetime by real days. A High wound drops it to 10 800 days, which kills outright anyone already older than that.
3. A wound to the **viscera** makes every other wound on the body take longer to heal.

### Healing

**Rules**

1. Only Low and Medium wounds heal. A High wound is permanent.
2. A wound heals once Wound Healing days have passed since the day it was taken.
3. Healing is not counted down. It is worked out from the calendar each time it is checked, so it is always correct.
4. A healed wound is removed, and its point of health comes back.
5. A wound with no day of infliction is **historical**: the scars an NPC carries from its past, or the injuries a protagonist starts with. Historical wounds never heal.

Example: a human with viscera 3 takes a Medium wound on day 100. Wound Healing is 460 days, so the wound heals on the first check from day 560.

## 8. Age and lifespan

Age is measured in days of the world calendar.

**Rules**

1. A body records the calendar day of its birth. Age is never stored.
2. Age = today's day − the birth day. Every creature ages as the calendar moves.
3. A body's lifespan is its Lifetime value (heart, §6).
4. A body dies of age once its age reaches its Lifetime.
5. A body created already old is capped at 9/10 of its own Lifetime, so no one is created past their death.

## 9. Creating a protagonist

A protagonist is created by spending a budget of points on the organ parts of the human body.

**Rules**

1. Every organ part starts at 1.
2. The budget is **25 points**. Each point raises one organ part by 1.
3. No part can be raised past its maximum.
4. The 28 parts have 65 points of room above 1 between them, so the budget buys less than half of the possible maximum. Creating a protagonist is a choice of what to give up.
5. When creation ends, the memory is built with the sizes the final scores give (see [Chapter II](02-memory.md)). Later changes do not rebuild it.
6. When creation ends, the humor queues are filled by production from the final scores of the four sanguific organs, with no black bile. Then a share of the places equal to Youthfulness is replaced with the humor of youth (see [Chapter III](03-humors.md)).
7. A protagonist starts with exactly one modus mentis, childhood recollection, at level 1. Everything else is learned in play.

## Terms

| Term | Meaning |
|---|---|
| Region | One of the major divisions of the body; the unit a blow is aimed at |
| Organ | A part of a region, made of one or two organ parts |
| Organ part | The smallest unit of the body, and the only one that stores a score |
| Organ score | The sum of an organ's part scores |
| Region score | The sum of a region's organ scores |
| Maximum | The highest score an organ part, organ or region can reach on a given body |
| Effective score | A score minus 1 per Medium wound that counts against it |
| Derived value | A number computed from one organ part, organ or region (§6) |
| Worst value | What a derived value falls to when its source is missing or switched off |
| Anatomy | The layout of a body: human, bestial or reptilian |
| Species | A kind of creature: one anatomy plus its own maximums |
| Capability | Speech, handcraft or abstraction: what an anatomy can do beyond its organs |
| Modus mentis (plural modi mentis) | A learned ability held in memory; see [Chapter II](02-memory.md) |
| Discipline | The body panel's word for an organ's modi mentis |
| Sanguific organ | One of the four humor-producing organs: hepar, spleen, paunch, pulmones |
| Humor | A unit of bodily fluid; see [Chapter III](03-humors.md) |
| Cinetic points | The action budget of one fight turn |
| Noetic points | The budget for thinking and acting in a scene |
| Wound | One injury; Low, Medium or High |
| Wildcard wound | A wound that hits no particular part |
| Mishap | A scrape or strain, left by a failed act or a stumble |
| Historical wound | A wound with no day of infliction; never heals |
| Health Points | Maximum health; current health is this minus the number of wounds |
| Lifetime | The age in days at which a body dies |

---

Next: [II. Memory](02-memory.md)
