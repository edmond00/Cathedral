# VIII. Combat

Combat is a turn-based fight on a grid of cells, with a fixed budget of points per turn. It is the
only part of the game where the body acts directly rather than through a modus mentis (one thing a
character has learned; see [Chapter II §1](02-memory.md#1-what-a-modus-mentis-is)).

**In short**

1. Each turn, a fighter has **cinetic points** (action points) equal to the backbone score × 2, and
   spends them on movement and skills.
2. A blow is settled by counting sixes: the attacker wins only with more sixes than the defender.
3. A fight started by striking someone opens with that blow already resolved, and that first blow can
   never kill.
4. A fight ends when one side has nobody standing. Losing a fight the protagonist is in is the
   protagonist's death, and fleeing is the only alternative.
5. Heat costs in combat drain the same humor queues that pay for travel.

## 1. Turn order and budget

Turn order decides who acts when; the budget decides how much each fighter can do in a turn.

**Rules**

1. Turn order is set once, at the start of the fight.
2. Each fighter rolls one die and adds its **Initiative**: the ears score, at least 1 (an
   organ-derived value, see [Chapter I §6](01-anatomy.md#6-derived-values)).
3. The fighters act in order of these totals, highest first. On a tie, a party member acts before an
   enemy.
4. A fighter's **cinetic points** per turn are the **Cinetic Points** value: the backbone score × 2,
   at least 1.
5. Cinetic points pay for movement and for every skill used.
6. A turn ends when the cinetic points are spent, or when the fighter ends it.
7. Movement is measured in steps. A straight step costs 1, and a diagonal step costs 1.5.
8. One cinetic point buys as many steps as the fighter's **Move Speed**, which is set by the legs
   score (see the table).
9. A move costs its total steps ÷ Move Speed in cinetic points, rounded up.

| Legs score | 0 | 1 | 2 | 3 | 4 |
|---|---|---|---|---|---|
| Move Speed (steps per cinetic point) | 1 | 2 | 4 | 6 | 8 |

**Example.** A fighter with backbone score 3 has 6 cinetic points. With legs score 2, each point buys
4 steps. A move of 3 straight steps and 2 diagonal steps costs 3 + 3 = 6, so it costs 2 cinetic
points, and 4 remain.

## 2. Terrain

Each cell of the combat grid has a type, which decides how it can be crossed.

**Rules**

1. There are six types of cell, listed in the table.
2. Entering a soft obstacle costs 3 times the movement of an ordinary step: 3 for a straight step,
   4.5 for a diagonal one.
3. A fighter entering treacherous or dangerous terrain may **slip**. The chance falls as the fighter's
   **Equilibrium** rises: the feet score, at least 1 (an organ-derived value, see
   [Chapter I §6](01-anatomy.md#6-derived-values)).
4. A fighter who slips stops moving and loses the rest of the turn. The fighter is marked as fallen
   over until the start of its next turn.
5. On dangerous terrain, a slip also deals a random **Low wound** (a wound that costs 1 health point
   and nothing else; see [Chapter I §7](01-anatomy.md#7-wounds)).
6. Adjacency is eight-way, for attacks as well as for movement.
7. Range is measured in a straight line, so the reach of a bow is a circle.
8. The eight surrounding cells always count as in reach, whatever the straight-line distance.
9. Reason: without rule 8, a diagonal neighbour would be about 1.4 cells away, outside the reach of a
   hand, and two fighters side by side could not hit each other.

| Type | Effect |
|---|---|
| Free space | Passable. |
| Hard obstacle | Impassable, except with a jumping skill. |
| Soft obstacle | Passable; each step into it costs 3 times the usual movement. |
| Treacherous terrain | Passable; may cause a slip, which ends the turn. |
| Dangerous terrain | Passable; may cause a slip, which ends the turn and deals a Low wound. |
| Exit | The way out of the fight: a fighter must stand on it to flee (§8). |

| Equilibrium | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|---|
| Slip chance, treacherous | 45 % | 40 % | 35 % | 30 % | 25 % | 20 % |
| Slip chance, dangerous | 72 % | 64 % | 56 % | 48 % | 40 % | 32 % |

The slip chance is 50 % − 5 % per point of Equilibrium on treacherous terrain (never below 5 %),
and 80 % − 8 % per point on dangerous terrain (never below 10 %).

## 3. Mediums

A **medium** is the physical thing a blow is struck with.

**Rules**

1. There are three kinds of medium: an organ, a body region, and a weapon.
2. Organ mediums include the hands, the feet, the fangs, the claws, a reptile's horns and its
   thagomizer (tail spikes or club).
3. A body region medium, such as the whole upper limbs, has a level equal to the region's total
   score.
4. A weapon medium must be held in a hand whose arm can bear its burden (see
   [Chapter IV §3](04-items.md#3-anchors)). Its level is the weapon's own level.
5. If an organ's parts act independently, each part is a separate medium. It strikes separately and
   uses only its own score.
6. The hands, the feet and the arms act independently. The left and right hand are two mediums, and
   each can strike once in the same turn.
7. If an organ's parts act together, the whole organ is one medium. It acts once per turn, at the
   sum of its parts' scores.
8. The legs act together, since both are used to run and dodge.
9. An organ with a single part is always one whole-organ medium.

## 4. Fighting skills

A **fighting skill** is an action a fighter can use in combat, such as an attack or a guard.

### When a skill can be used

1. A fighting skill can be used only if all three conditions hold: the fighter holds the modus mentis
   that unlocks it; the body can still carry that modus mentis; and the medium the skill needs is
   present and not disabled.
2. The body can no longer carry the skill when the summed level of its modi mentis is below 0. Each
   modus mentis counts at its usable level, capped by its maximum level
   ([Chapter II §3](02-memory.md#3-level-and-maximum-level)).
3. This is a separate test from the medium. The medium asks whether there is a limb; this asks
   whether the technique can still be held.
4. A sum of exactly 0 still counts as usable. Reason: 0 is also the level of an unlearned skill, and
   the opening blow (§5) draws from unlearned skills on purpose.
5. A skill lost to injury is still shown but cannot be chosen, so that its loss is visible as a
   result of the injury.

### Dice and cost

1. Each skill names one **primary** modus mentis and may name **secondary** ones. The levels of all
   of them that the fighter holds are added together.
2. The skill's dice = base dice + (medium level × medium multiplier) + (summed modus mentis level ×
   modus mentis multiplier).
3. Most skills have no base dice and get all their dice from the two levels.
4. A skill costs cinetic points, and may also cost vital heat (the energy carried by humors; see
   §10).
5. There are six kinds of effect, listed in the table.

| Effect | What it does |
|---|---|
| Attack | Wounds the target if the attacker's sixes win. |
| Defence posture | Raises the defence pool until the fighter's next turn. |
| Defence | A reactive guard: adds defence dice against one or all incoming blows. |
| Special movement | Allows a move that is otherwise blocked, such as jumping a hard obstacle. |
| Utility | Any other effect. |
| Buff | Applies a state to the user. No target, no dice, no wound. |

**Example.** A skill has no base dice, a medium multiplier of 1 and a modus mentis multiplier of 1.
It is used through a medium of level 2 by a fighter whose relevant modi mentis sum to 3. It rolls
2 + 3 = 5 dice.

### Buffs

A **buff** is a skill that applies a state to its user.

1. A buff rolls no dice, since it has no target and nothing resists it.
2. A buff costs vital heat equal to 10 − the skill's total level, kept within 1 to 10.
3. A higher level makes a buff cheaper, not stronger. This is the opposite of every other formula in
   this manual.
4. A buff's whole effect is the state it applies.

| Skill total level | 0 | 3 | 6 | 9 | 10 or more |
|---|---|---|---|---|---|
| Heat cost | 10 | 7 | 4 | 1 | 1 |

### Guards

A **guard** is a defensive skill that adds dice to the defence pool.

1. Defence is a number of dice, so guards are bought, not rolled.
2. A parry, a dodge, a cover and a defensive stance each cost cinetic points, and add dice to the
   defence pool for the turn at once.
3. A skill aimed at its own user can never cause a wound.
4. The feint is the only self-targeted skill that rolls dice, since someone must be convinced. Its
   sixes become bonus attack dice, not wounds.

## 5. The opening blow

The **opening blow** is the blow that starts a fight when a character strikes someone outside
combat. It is resolved before the fight opens, and uses no turn, no cinetic points and no defence
pool.

### What can strike

1. If an implement was combined with the act, only that implement can strike. It must be a weapon
   deliberately taken up for the act, not one merely carried.
2. If no implement was combined, every medium of the body is gathered: each organ medium and each
   body region medium with a score above 0 and not disabled by a **High wound** (a wound that
   disables what it hits).
3. Every attacking skill those mediums offer is a candidate. This includes skills not yet learned,
   as long as the anatomy could hold the modus mentis behind them.
4. Guards, postures, buffs and movement skills are excluded.
5. If there is no working medium, or the implement offers no attacking skill, the act is refused
   before any dice are rolled. This is a rule of action
   ([Chapter VI §6](06-narration.md#6-rules-of-action-and-rules-of-choice)).

### The blow

1. One candidate skill is chosen at random. It decides both the blow and what is learned.
2. The blow uses the dice already rolled for the act. There is no second roll.
3. Armour does not stop the opening blow. Armour only adds dice to a defence pool, and no defence
   pool is built here.
4. The wound is drawn from the wounds allowed at the place struck, as in §6.
5. Any state the skill applies, such as knockdown or bleeding, is already active when the fight
   starts.
6. The blow and its effects are applied before the fight starts, so the target begins the fight
   already hurt.
7. An opening blow can never kill. If the target is at its last health point, the blow deals no
   wound, the lesson is still learned, and the fight starts normally.
8. Killing outright is a separate act with its own difficulty, and requires a blade
   ([Chapter VI §11](06-narration.md#11-what-a-killing-leaves)).

### What is learned

1. The lesson is the modus mentis behind the chosen skill, not a fixed lesson of striking.
2. An unarmed blow teaches the modus mentis of fist-fighting. A blow with an axe teaches the modus
   mentis of weapons.
3. This is the only act whose lesson is not fixed before the act is done.

### A miss, and small creatures

1. A missed opening blow still starts the fight.
2. After a miss, the target's side acts before the party, whatever the initiative rolls.
3. A missed opening blow teaches nothing.
4. Small wildlife has no anatomy to wound and gives no fight. Against it, the opening blow simply
   kills, and narration continues.
5. Against small wildlife, the choice of medium and skill, and the lesson, are the same as above.

## 6. Resolving an attack

An attack in combat is settled by comparing the sixes rolled by two pools of dice.

**Rules**

1. The attack pool = the skill's dice (§4) + the attacker's **Natural Attack** (the genitories score,
   in dice; an organ-derived value, see [Chapter I §6](01-anatomy.md#6-derived-values)) + any
   active bonuses.
2. The defence pool is the sum of the contributions in the table.
3. Both pools are rolled, and only sixes are counted.
4. The attacker wins only with more sixes than the defender. A tie goes to the defender.
5. Armour counts only for the body section that is hit, so the hit location must be known before
   the dice are rolled.
6. An attack with no chosen location rolls its location first. Each part's chance is its share of
   all the wounds the target could take, so a large region is hit more often than a small organ.
7. If the skill names a location, or the attacker chooses one, that location is used instead.
8. A blow that wins deals a wound, drawn from the wounds that location allows.
9. No later roll reduces the wound's severity. A body's resilience shows instead in how fast its
   wounds heal: the **Wound Healing** value of the viscera (see
   [Chapter I §7](01-anatomy.md#7-wounds)).
10. A blow that wins gives 1 experience point to every modus mentis that contributed to the skill,
    except those already at their maximum level.

| Defence contribution | Source |
|---|---|
| Natural Defence | The lower limbs region score ÷ 3, rounded down, in dice. A beast or reptile has no lower limbs and uses its legs score ÷ 4, rounded down. |
| Guards and stances | All defensive skills currently in effect. |
| Armour | The defence dice of every worn garment covering the section hit. |

**Example.** An attacker rolls 6 dice and gets two sixes. The defender has 3 dice of Natural
Defence, 2 from a parry and 1 from armour on the arm that is hit. The defender rolls 6 dice and gets
two sixes. The tie goes to the defender, and no wound is dealt.

## 7. States

A **state** is an effect a winning blow can put on its target.

**Rules**

1. A state lasts for one of three durations: this turn, this round, or the whole fight.
2. A "this turn" state ends at the **end** of the bearer's own turn.
3. Reason: the start of the bearer's next turn comes only after every other fighter has acted, so
   ending it there would make a one-turn state last a full round.
4. A "this round" state lasts until the start of the bearer's next turn, so it is still active while
   the others attack.
5. Some "whole fight" states never end.
6. Some states stack instead of being added twice: a second bleeding makes the first one worse.
7. A bleeding fighter loses humors at the start of each of its turns: one humor per level of the
   bleeding, consumed in rotation from the four queues (see
   [Chapter III §3](03-humors.md#3-vital-heat)).

## 8. Knockdown, learning and fleeing

These rules cover getting up, learning a skill during a fight, and leaving a fight.

### Knockdown

1. A knocked-down fighter rolls dice equal to its **Knockdown Recovery** (the pineal gland score, at
   least 1; an organ-derived value, see [Chapter I §6](01-anatomy.md#6-derived-values)) at the
   start of its turn.
2. One six or more and the fighter stands up and takes its turn.
3. With no six, the fighter stays down and its turn is skipped.

### Learning during a fight

1. A fighter can attempt an unknown fighting skill during a fight. It rolls dice equal to its
   **Fight Learning** (the cerebellum score, at least 1).
2. The sixes needed equal the skill's position in the list of skills its medium offers: 1 six for
   the first skill, 2 for the second, and so on.
3. On a success, the fighter gains the skill's modus mentis at level 1 and performs the skill at
   once.
4. On a failure, the fighter pays the skill's cinetic points.
5. Enemies learn by the same rule.
6. An enemy with no known attack that can reach a target attempts the first attacking skill each of
   its mediums offers, at most once per skill per turn.
7. An enemy that learns a skill then chooses its action again, now knowing that skill, and pays for
   the blow normally.
8. Enemies prefer a known attack to an untried one of equal value, since the learning check can
   fail.

**Example.** A fighter with cerebellum score 3 tries the second skill its hands offer. It rolls 3
dice and needs 2 sixes.

### Fleeing

1. A fighter can try to flee only while standing on the exit cell.
2. An attempt costs 1 cinetic point, and can be made once per turn unless a state allows a retry.
3. The fighter rolls dice equal to its **Runaway Dice** (the feet score, at least 1), and needs at
   least one six.
4. Fleeing a fight inside a place leaves the battlefield but not the place. The party then heads for
   the place's entrance as a runaway.
5. Anyone who confronts the fleeing party in a later area stops it there, under the rules of
   [Chapter VI §7](06-narration.md#7-crime-and-being-seen). The area of the fight itself is not
   checked again.
6. Fleeing ends the fight and nothing else. The enemy is still an enemy on later visits, is still
   armed in the room, and any failed act in front of them starts the fight again.

## 9. The end of a fight

A fight ends when one side has nobody left standing.

**Rules**

1. Every party member who fights counts for the party, and every enemy for the other side. Nothing
   else is counted.
2. A companion's fall does not end the fight while any other party member still stands.
3. The protagonist's fall does not end the fight either, if a companion still stands. The companion
   fights on alone.
4. If the last fighters of both sides fall at the same time, the enemy wins.
5. A fighter falls in one of two ways, which count the same: its health reaches 0, or all four of
   its humor queues become **critical** (filled entirely with black bile; see
   [Chapter III §5](03-humors.md#5-black-bile)).
6. Critical queues are the usual result of bleeding left to run. They kill a body even at full
   health.
7. The dead do not get up again. Each leaves a corpse where it fell
   ([Chapter VI §11](06-narration.md#11-what-a-killing-leaves)).
8. An enemy who falls is gone from that place permanently
   ([Chapter VI §9](06-narration.md#9-what-a-place-remembers)).

### Who fights

1. On the road, the whole party fights together.
2. In a place, each party member stands in an area of its own
   ([Chapter VI §10](06-narration.md#10-the-party-in-a-place)). A fight there involves the members
   standing in the area where it breaks out, and nobody else.
3. The protagonist may be among those elsewhere, and so absent from the fight.
4. A fight lost without the protagonist in it kills the companions who fought it, and the run goes
   on.
9. A companion who falls is gone from the party permanently
   ([Chapter V §9](05-travel.md#9-companions-and-their-loss)).
10. Losing a fight is the protagonist's death and ends the run, whether the fight happened on the
    road or inside a place.
11. Fleeing is therefore the only alternative to fighting to the end. Its cost is an enemy that
    remains after the visit.

## 10. Vital heat in combat

**Vital heat** is the energy a humor carries. In combat it is drawn from the same humor queues that
pay for travel.

**Rules**

1. A combat cost of N heat consumes N humors, in rotation from the four queues (see
   [Chapter III §3](03-humors.md#3-vital-heat)).
2. In combat, each humor consumed counts as 1 toward the cost, whatever heat it carries.
3. Fighting therefore drains the same reserve that pays for travel.
4. A character who fights a lot while crossing a region may not have enough left to cross the next
   stretch.

## Terms

| Term | Meaning |
|---|---|
| Modus mentis (plural modi mentis) | One thing a character has learned (see Chapter II). |
| Cinetic points | Points spent each turn on movement and skills: the backbone score × 2, at least 1. |
| Initiative | The ears score, added to one die to set turn order. |
| Move Speed | Steps one cinetic point buys, from the legs score: 1, 2, 4, 6 or 8. |
| Step | One cell of movement: 1 straight, 1.5 diagonal, 3 times as much into a soft obstacle. |
| Equilibrium | The feet score, at least 1; lowers the chance of slipping. |
| Slip | Losing footing on treacherous or dangerous terrain; ends the turn. |
| Medium | What a blow is struck with: an organ, a body region or a weapon. |
| Fighting skill | An action usable in combat, unlocked by a modus mentis. |
| Primary, secondary modus mentis | The modi mentis whose levels add to a skill's dice. |
| Buff | A skill that applies a state to its user; costs heat, rolls nothing. |
| Guard | A defensive skill that adds dice to the defence pool. |
| Feint | The one self-targeted skill that rolls; its sixes become bonus attack dice. |
| Opening blow | The blow that starts a fight, resolved before the fight opens. |
| Attack pool, defence pool | The dice each side rolls; only sixes count. |
| Natural Attack | The genitories score, in attack dice. |
| Natural Defence | Defence dice from the lower limbs (score ÷ 3) or a beast's legs (score ÷ 4). |
| State | An effect a blow puts on its target, lasting a turn, a round or the fight. |
| Knockdown Recovery | Dice rolled to stand up: the pineal gland score, at least 1. |
| Fight Learning | Dice rolled to learn a skill mid-fight: the cerebellum score, at least 1. |
| Runaway Dice | Dice rolled to flee: the feet score, at least 1. |
| Vital heat | The energy carried by a humor. |
| Critical | A humor queue filled entirely with black bile. |
| Low wound, High wound | A wound that costs 1 health only; a wound that disables what it hits. |

---

Previous: [VII. Social Interaction](07-social.md) · Next: [IX. Mental Maps and Routines](09-routines.md)
