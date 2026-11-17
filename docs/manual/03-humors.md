# III. Humors

Humors are units of bodily fluid held in four queues. They pay for physical effort, and they can be
spent to change a die after it has been rolled.

**In short**

1. Each of four organs keeps a queue of 49 humors, and each queue is always full.
2. A healthy organ produces mostly blood; a damaged organ produces mostly black bile.
3. Effort costs vital heat, which is paid by using up humors from the four queues in turn.
4. The humor at the back of each queue can be spent to change one die of a roll.
5. Black bile cannot leave a queue. When all four queues are full of black bile, the character dies.

## 1. The four queues

A queue is the ordered row of humors that one organ holds.

**Rules**

1. The four **sanguific organs** (the organs that produce humors) are the *hepar* (liver), the
   *paunch* (stomach), the *pulmones* (lungs) and the *spleen*.
2. Each sanguific organ has its own queue of exactly 49 places.
3. A queue is always full. Nothing enters a queue unless something else leaves it.
4. New humors enter at the **front** of the queue.
5. Humors leave from the **back** of the queue, where the oldest humor is.
6. **Secretion**: the organ produces a new humor at the front, every humor moves one place back, and
   the humor at the back leaves.
7. **Consumption**: the humor at the back is taken and used up, and the organ produces a new humor at
   the front to replace it.

## 2. Which humor an organ produces

When an organ secretes, the humor it produces is chosen at random, with chances set by its score.

**Rules**

1. The chances depend only on the score of the organ that secretes.
2. The four chances always add up to 100 %.
3. The four basic humors are blood, yellow bile, black bile and phlegm.
4. A high score produces mostly blood. A low score produces mostly black bile.
5. The chance of phlegm barely changes with the score.
6. Human sanguific organs reach a score of 3 at most. Some animal species reach 4.
7. The four temperaments (sanguine, bilious, melancholic and so on) are not separate rules. They are
   this one table read at different organ scores.

| Organ score | Blood | Yellow bile | Black bile | Phlegm |
|---|---|---|---|---|
| 0 | 0 % | 40 % | 50 % | 10 % |
| 1 | 12 % | 35 % | 42 % | 11 % |
| 2 | 25 % | 30 % | 33 % | 12 % |
| 3 | 37 % | 25 % | 25 % | 13 % |
| 4 | 49 % | 20 % | 17 % | 14 % |

## 3. Vital heat

**Vital heat** is the energy a humor carries. It pays for effort.

**Rules**

1. Every humor carries a vital heat value from −5 to +3.
2. Vital heat pays for crossing cells of the world map and for some actions in combat.
3. Humors are consumed one at a time, in rotation: hepar, paunch, pulmones, spleen, then hepar
   again.
4. The rotation does not restart with each cost. It continues from the queue after the last one
   used.
5. A critical queue (a queue made entirely of black bile, see §5) is skipped in the rotation.
6. The four basic humors carry little heat: blood carries +1; yellow bile, phlegm and black bile
   carry 0.
7. The other humors come from eating and drinking, intoxication, disease and emotions. They carry the
   extreme values, from −5 (Venom) to +3.
8. What a character has taken in therefore stays in its queues, as fuel or as poison, for up to 49
   places.

### Paying for travel

1. A crossing's heat cost is paid by consuming humors in rotation until their heat adds up to the
   cost.
2. A humor with 0 heat pays nothing, but is still consumed.
3. A humor with negative heat does not reduce the cost. It adds its heat back to the cost, but never
   more than the heat already paid in the current payment.

### Paying in combat

1. A combat cost of N heat consumes N humors in rotation, whatever heat each one carries.
2. Bleeding consumes humors in the same way at the start of each of the bleeding fighter's turns
   (see [Chapter VIII §7](08-fighting.md#7-states)).

### Starvation

1. When every queue is critical, no cost can be paid, and the character dies of starvation.
2. Starvation is the only way the humor system kills directly.
3. It is checked wherever the character is: during travel, where heat is paid stretch by stretch,
   and inside a place, where bleeding or the effort of a fight can empty the queues.
4. Death happens as soon as all four queues are critical. It does not wait for the next journey (see
   [Chapter I §7](01-anatomy.md#7-wounds)).

**Example.** A payment of 2 heat is due during a crossing, and the rotation is at the hepar. The hepar's back humor
is blood (+1): 1 heat is still owed. The paunch's is Venom (−5): only 1 heat has been paid so far,
so the cost goes back up by 1, to 2. The pulmones' is phlegm (0): nothing is paid. The
spleen's and the hepar's next humors are blood: 2 heat is paid, and the payment is settled. In
combat, a skill costing 2 heat would have consumed only the hepar's blood and the paunch's Venom.

## 4. Transmuting virtues

A **transmuting virtue** is a dice modifier carried by a humor. It changes one die of a roll after
the roll and before the result is accepted.

**Rules**

1. Every humor except black bile carries one transmuting virtue.
2. Only the rearmost humor of each queue that is not black bile can be spent. At most four humors
   are therefore available for a roll, one per organ.
3. Which four they are follows from everything the body has secreted and consumed since it was made.
4. Blood adds 1 to a die.
5. Yellow bile subtracts 1 from a die.
6. Phlegm carries the null virtue, which changes nothing.
7. The other humors carry conversions and rerolls, some harmful. Venom turns a 6 into a 1; Voluptas
   turns any face into a 6.
8. Numeric changes are kept within 1 to 6.
9. Spending a humor is not consumption. The humor is removed, the organ **does not secrete a
   replacement**, and the next humor in the queue becomes the one available.
10. To keep the queue full, a phlegm takes the empty place at the front.
11. The most humors that can be spent on one roll is the **Humor Modifier Limit**: the viscera
    score, never less than 1 (an organ-derived value, see
    [Chapter I §6](01-anatomy.md#6-derived-values)).
12. Reason for the null virtue: it changes no die, but spending it uncovers the humor behind it.
    Without it, a phlegm at the back would block that organ for the whole roll.

| Form | Written | Effect |
|---|---|---|
| Null | N → N | Applies to any die and changes nothing. |
| Numeric | N → N ± X | Adds or subtracts a fixed amount from any die, within 1 to 6. |
| Conversion | S → T, or N → T | Turns a die showing S into T. The N form applies to any face. |
| Reroll | S → ?, or N → ? | Rerolls a die showing S. The N form applies to any face. |

**Example.** A character with viscera score 2 rolls 5, 3 and 1. The usable humors are blood (hepar),
phlegm (paunch), yellow bile (pulmones) and Euphora (spleen, rerolls a 1). The character spends the
blood to turn the 5 into a 6, then the Euphora to reroll the 1. The Humor Modifier Limit of 2 is
reached, and the result is accepted.

## 5. Black bile

Black bile is the humor of a failing organ, and it breaks the rules above.

**Rules**

1. Black bile has no transmuting virtue and 0 vital heat. It cannot change a die or pay a cost.
2. Black bile cannot leave a queue by normal movement.
3. When a humor must leave the back of a queue, the rearmost humor that is not black bile leaves
   instead. The black bile behind it moves forward to fill the gap.
4. Black bile therefore builds up at the back of a queue and stays there.
5. Each black bile that builds up leaves fewer places for every other humor.
6. A queue made entirely of black bile is **critical**.
7. Nothing can be added to or taken from a critical queue, and it is skipped in the rotation that
   pays heat.
8. When all four queues are critical, the character dies.

## 6. Starting queues

The starting queues are the humors a character has when it is created.

**Rules**

1. The protagonist's queues are filled at the end of character creation, by secretion from the final
   scores of the four sanguific organs.
2. For this first filling no black bile is produced. Its chance is shared out among blood, phlegm
   and yellow bile in proportion to their own chances.
3. The protagonist therefore starts with no black bile, whatever the organ scores.
4. **Youthfulness** is the heart score × 10 %, at most 50 % (an organ-derived value, see
   [Chapter I §6](01-anatomy.md#6-derived-values)).
5. After the first filling, each place has a chance equal to youthfulness of being replaced with
   **Juvenescence** (the humor of youth).
6. Juvenescence carries +2 vital heat and adds 2 to a die.
7. A warm heart therefore gives a larger reserve of heat and better dice, until the Juvenescence is
   used up.
8. Other characters are filled as the grown bodies they are: every place is secreted normally from
   its organ's score, black bile included.
9. Their chance of Juvenescence is youthfulness × the fraction of their lifetime still ahead of them,
   rounded to a whole percent: the full value at birth, falling evenly to 0 at the end of life,
   whatever the species' lifetime.
10. A companion joins the party with queues filled this way.

**Example.** A heart score of 3 gives youthfulness 30 %. A protagonist with that heart starts with
about 30 % of its places as Juvenescence. Another character with the same heart, a quarter of the
way through its life, has about 22 %; three quarters through, about 7 %.

## 7. What moves a queue

A queue moves whenever humors are added to it or removed from it.

**Rules**

1. A queue moves when vital heat is paid: for crossing a cell of the world, for a combat skill that
   costs heat, or for keeping a buff active.
2. A queue moves when a transmuting virtue is spent on a die.
3. A queue moves when something is ingested. Its whole content is pushed into the queue of the organ
   that receives it: the paunch, the hepar or the pulmones, depending on how it was taken (see
   [Chapter IV §6](04-items.md#6-consumables)).
4. A queue moves when an act produces a result that an emotion reacts to. Emotion humors are then
   pushed into the spleen (see §8).
5. Everything that enters a queue pushes something out at the back.
6. Everything consumed is replaced by the organ according to its score.
7. A character's humors at any later time therefore depend only on what it has done and on its organ
   scores.

## 8. Emotion humors

**Emotion humors** are humors produced by what a character has just done, rather than by an organ.

**Rules**

1. Emotion humors go only into the spleen.
2. No organ secretes them.
3. They come from a modus mentis with the Emotion function. (A **modus mentis**, plural *modi
   mentis*, is one thing a character has learned; see
   [Chapter II §1](02-memory.md#1-what-a-modus-mentis-is).)
4. Such a modus mentis lists in advance which kinds of result move it, and which humor each result
   produces.
5. Only the kind of result counts (for example, coin gained or a blow struck). What was gained, or
   who was hit, does not count.
6. Every modus mentis the character holds is checked, not only the one used for the act.
7. If several react to one act, exactly one is chosen at random among them. Level plays no part.
8. The amount is one die roll, 1 to 6. That many copies of the humor enter the front of the spleen's
   queue, and the same number leave its back.
9. Emotion humors carry vital heat and transmuting virtues like any other humor. They are consumed
   and spent in the normal rotation.
10. So an emotion felt now affects the next journey (through its heat) and the next roll (through its
    virtue).
11. Most emotion humors act on the extreme faces: they turn a 6 into a lower face, or a 5 into a 6.
    Constantia (steadfastness), produced by a character hardened against its own hurt, turns a 1
    into a 3.
12. Their effects are therefore stronger, in both directions, than those of the secreted humors,
    which move a die by 1.

**Example.** A character holds a modus mentis that produces Voluptas (pleasure) when coin is gained,
and another that produces Pudor (shame) when a blow is struck. The character steals coin. The coin
gain moves the first modus mentis, even though the coin came from stealing rather than bargaining. A
die shows 4, so 4 Voluptas enter the front of the spleen's queue and 4 humors leave its back. Each
Voluptas carries +3 heat and can turn any die into a 6.

## Terms

| Term | Meaning |
|---|---|
| Humor | One unit of bodily fluid in a queue; carries vital heat and, except black bile, a transmuting virtue. |
| Sanguific organ | One of the four organs that produce humors: hepar, paunch, pulmones, spleen. |
| Hepar | The liver. |
| Paunch | The stomach. |
| Pulmones | The lungs. |
| Queue | An organ's ordered row of 49 humors; new ones enter at the front, old ones leave from the back. |
| Secretion | An organ producing a new humor at the front, pushing the oldest out at the back. |
| Consumption | Using up the back humor to pay heat; the organ replaces it at the front. |
| Spending | Using the rearmost non-black-bile humor's virtue on a die; the organ secretes no replacement, and a phlegm fills the front. |
| Vital heat | The energy a humor carries, from −5 to +3; pays for effort. |
| Transmuting virtue | A humor's dice modifier: null, numeric, conversion or reroll. |
| Humor Modifier Limit | How many humors can be spent on one roll: the viscera score, at least 1. |
| Black bile | The humor of a failing organ; no virtue, no heat, and it cannot leave a queue. |
| Critical | A queue made entirely of black bile; skipped when paying heat. |
| Starvation | Death when all four queues are critical. |
| Youthfulness | The heart score × 10 %, at most 50 %; the starting share of Juvenescence. |
| Juvenescence | The humor of youth: +2 heat, adds 2 to a die. |
| Emotion humor | A humor put into the spleen by a modus mentis with the Emotion function. |
| Modus mentis (plural modi mentis) | One thing a character has learned (see Chapter II). |

---

Previous: [II. Memory](02-memory.md) · Next: [IV. Items](04-items.md)
