# FEATURES

## fighting rules :

- skill consumme cinetic points
- can do as many skills as cinetic points allows
- however, sae skill can only be done once
- ATTACK_LEVEL dices and DEFENSE_LEVEL dices are rolled, if the number of attack 6s is greater than the defense 6s, attack touch.
- if attack touch, wound added to one of the attack possible localization
- a wound heals on its own if it is low or medium handicap: the wound healing derived stat
  (viscera) sets how many days it takes, from 1000 down to 100. High handicap wounds are permanent,
  and so are the wounds a character was created with. The clock only advances on travel and work,
  so recovery is measured in journeys.
- where an attack has several possible localizations, ONE is drawn before the dice are rolled —
  it has to be, because the defender's armour for that section is counted into the defense pool.
- if the drawn localization has no wound authored for it, the blow leaves a generic wound instead
  (cut / puncture / contusion), chosen to match the attack's damage type

## fight skill learning :

- known fighting skill are defined by the known Modus Mentis
- each Modus Mentis have a list of fighting attack they unlock
- multiple MM can unlock same skill, in this case, levels add up
- however, each fighting skill is linked to a "main" MM
- unknowns skills are fighting skills not unlocked by any MM
- each medium have a sorted list of fighting skill
- all known fighting skills can be used
- the first unknown skill of a medium skill list can also be attempted
- when attempting an unknown skill, first a "learning in fight" check need to be succeeded
- to succeed it, dices are throwns and more 6 than the "learning in fight" difficulty level need to be rolled to succeed
- the number of dices roll is defined by the "" level (derived stats of the cerebellum)
- the difficulty level is defined by the index of the skill in the medium list
- if succeeded, the main MM of the attempted skill is learned (added to the working memory) and the skill process start

## anatomy

- attack localization is based on human anatomy
- some attack can let the player choose the localization, in this case, the opponenent specific anatomy is used on the UI to let the player choose where to attack
- if an attack is localized to a body part/organ that don't exist on the specific ennemy anatomy,
  that localization is simply skipped; if none of the attack's localizations exist there, the blow
  lands as a generic wound rather than failing outright
- containment cascades at every tier: a localization gathers its own wounds, its organs' and its
  organ-parts' — aiming at the legs can break a knee, aiming at the visage can take an eye
- each anatomy has its own wound catalogue: a beast suffers broken forelegs and torn-off fangs,
  never the human list

## bleeding

- special effect of some attack
- if bleeding, at each turn, vital heat is consummed from body humors queue
- stop after the fight
- quantity of vital heat consumed depends of the bleeding level

## pushback

opponent pushback 1d6 cells in the opposite direction of the attack. Subish terrain penalty of the passed cells. If encounter hard obstacle, pushback stop but opponent receive a new backbone contending attack half the level of the original attack

## knockdown

opponent can not attack this turn unless it succeed a knockdown recovery


## charge

Normal melee attack only available on neighbor cells opponent but charge attack available on a radius defined by the attack to simluate charge distance before the attack

The attacker actually closes the ground: on use, they move to the nearest free cell beside the
target and strike from there, all in one action. If no route within the charge distance exists the
charge fails and the Cinetic Points are spent on the attempt.

# MODUS MENTIS

1. Pugilatus
2. Uppercut
3. Acrobatics
4. Athletics
5. Brute Force
--- new ---
6. Swordsmanship
7. Brawling
8. Iron Fist
9. Low Blow
10. Battlecraft
11. Incisiveness
12. Tactics
13. Marksman
14. Deadeye
15. Predator
16. Ferocity
17. Survivalism
18. Cold Blood
19. Rage
20. Blood lust
21. Iron Nerves
22. Vigilance

# MEDIUM

## BEAST BODY PART

### fangs

1. Flesh Tear
2. Flesh Clamp
3. Throat Grip

### claws

1. Scratch
2. Lacerate
3. Gut Ripper

## HUMAN BODY PART

### hands

1. Punch
2. Uppercut
3. Palm Strike

### upper limbs

(body-part medium — level is the whole region's total score: hands + arms)

1. Seize
2. Chokehold

### feet

1. high kick
2. trip
3. back kick

### viscera

1. Survival Instinct
2. Cold Blood
3. Rage
4. Blood lust
5. Iron Nerves

### teeth

1. Bite
2. Flesh Tear

### leg

1. sprint
2. knee strike
3. dodge
4. jump
5. defensive posture

### arm

1. Push
2. elbow strike

## MELLEE WEAPON

### long blade

1. Cleaving Strike
2. Counter Strike
3. Forward Lunge
4. Feint

### short blade

1. Snap thurst
2. Needle thurst
3. Parry
4. Deep Pierce

### saber

1. Snap thurst
2. Feint
3. Needle thurst
4. Counter Strike

### blunt weapon

1. Smash
2. Crushing Blow
3. Heavy Strike
4. Mighty Swing

### axes

1. Chop
2. Heavy Strike
3. Cleaving Strike
3. Driving Lunge

### pickaxes

1. Piercing Blow
2. Deep Pierce
3. Mighty Swing
4. Crushing Blow

### spear

1. Forward Lunge
2. Piercing Blow
3. Driving Lunge
4. Deep Pierce

## LONG-RANGE WEAPON

### bows

1. Quickshot
2. Pinpoint Shot
3. Longshot

### crossbows

1. Sighted Shot
2. Pinpoint Shot
3. Deadeye Shot 

## DEFENSIVE WEAPON

### shield

1. Cover
2. Parry
3. Shield Bash


# SKILLS

1. Flesh Tear

- Mediums : fangs #1 / teeth #2
- Cinetic Points : 2
- Main MM : Ferocity
- Secondary MM : Predator / Blood Lust
- Type : Attack
- Damage Type : cutting
- Localization : Trunk / Upper Limbs
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2
- Special Effect : Bleeding (1)

2. Flesh Clamp

- Mediums : fangs #2
- Cinetic Points : 2
- Main MM : Predator
- Secondary MM : Ferocity / Brawling
- Type : Attack
- Damage Type : piercing
- Localization : Upper Limbs / Trunk
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : Immobilize

3. Throat Grip

- Mediums : fangs #3
- Cinetic Points : 4
- Main MM : Predator
- Secondary MM : Ferocity / Incisiveness
- Type : Attack
- Damage Type : piercing
- Localization : Pulmones
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 2
- Special Effect : Immobilize, bleeding (3)

4. Scratch

- Mediums : claws #1
- Cinetic Points : 1
- Main MM : Predator
- Secondary MM : Ferocity / Brawling
- Type : Attack
- Damage Type : cutting
- Localization : Trunk / Lower Limbs
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1

5. Lacerate

- Mediums : claws #2
- Cinetic Points : 3
- Main MM : Predator
- Secondary MM : Ferocity / Incisiveness / Blood Lust
- Type : Attack
- Damage Type : cutting
- Localization : Trunk
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2
- Special Effect : None

6. Gut Ripper

- Mediums : claws #3
- Cinetic Points : 5
- Main MM : Predator
- Secondary MM : Ferocity / Incisiveness
- Type : Attack
- Damage Type : cutting
- Localization : Viscera
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 3
- Special Effect : Bleeding (3)

7. Punch

- Mediums : hands #1
- Cinetic Points : 1
- Main MM : Pugilatus
- Secondary MM : Brawling / Brute Force
- Type : Attack
- Damage Type : contending
- Localization : CHOOSE
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : None

8. Seize

- Mediums : upper limbs #1
- Cinetic Points : 2
- Main MM : Brawling
- Secondary MM : Pugilatus / Brute Force
- Type : Attack
- Damage Type : contending
- Localization : CHOOSE
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : Immobilize

9. Uppercut

- Mediums : hands #2
- Cinetic Points : 3
- Main MM : Uppercut
- Secondary MM : Pugilatus / Iron Fist
- Type : Attack
- Damage Type : contending
- Localization : Visage / Encephalon
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2

10. Chokehold

- Mediums : upper limbs #2
- Cinetic Points : 4
- Main MM : Brawling
- Secondary MM : Brute Force / Predator
- Type : Attack
- Damage Type : contending
- Localization : Pulmones / Visage
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 2
- Special Effect : Immobilize

11. Palm Strike

- Mediums : hands #3
- Cinetic Points : 3
- Main MM : Iron Fist
- Secondary MM : Pugilatus / Battlecraft
- Type : Attack
- Damage Type : contending
- Localization : Visage / Trunk
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 3

12. High Kick

- Mediums : feet #1
- Cinetic Points : 2
- Main MM : Acrobatics
- Secondary MM : Athletics / Low Blow
- Type : Attack
- Damage Type : contending
- Localization : Visage / Trunk
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2

13. Trip

- Mediums : feet #2
- Cinetic Points : 2
- Main MM : Low Blow
- Secondary MM : Brawling / Acrobatics
- Type : Attack
- Damage Type : contending
- Localization : Legs / Feet
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : Knockdown

14. Back Kick

- Mediums : feet #3
- Cinetic Points : 3
- Main MM : Athletics
- Secondary MM : Acrobatics / Battlecraft
- Type : Attack
- Damage Type : contending
- Localization : Trunk / Lower Limbs
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2
- Special Effect : Pushback

15. Survival Instinct

- Mediums : viscera #1
- Cinetic Points : 1
- Vital Heat : 2
- Main MM : Survivalism
- Secondary MM : Iron Nerves / Vigilance
- Type : Other
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : allow to retry runaway dice roll as many times you want this turn

16. Cold Blood

- Mediums : viscera #2
- Cinetic Points : 1
- Vital Heat : 4
- Main MM : Cold Blood
- Secondary MM : Tactics / Iron Nerves
- Type : Other
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect :  during this turn, succeeding a defense interrupt the ennemy turn

17. Rage

- Mediums : viscera #3
- Cinetic Points : 1
- Vital Heat : 6
- Main MM : Rage
- Secondary MM : Ferocity / Blood Lust
- Type : Other
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : during this turn, succeeding an attack refill the cinetic points (only once by turn)

18. Blood Lust

- Mediums : viscera #4
- Cinetic Points : 1
- Vital Heat : 8
- Main MM : Blood Lust
- Secondary MM : Rage / Ferocity
- Type : Other
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : always choose the wound with the highest severity when selecting the wound, for all the fight, not just this turn

19. Iron Nerves

- Mediums : viscera #5
- Cinetic Points : 1
- Vital Heat : 10
- Main MM : Iron Nerves
- Secondary MM : Cold Blood / Vigilance
- Type : Other
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : Allow to redo skill already done this turn (except run away)

20. Bite

- Mediums : teeth #1
- Cinetic Points : 1
- Main MM : Ferocity
- Secondary MM : Predator / Blood Lust
- Type : Attack
- Damage Type : piercing
- Localization : arms
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1

21. Sprint

(named "sprint", not "run": the fight UI already has a RUN AWAY button for fleeing combat)

- Mediums : leg #1
- Cinetic Points : 1
- Main MM : Athletics
- Secondary MM : Acrobatics / Survivalism
- Type : Other
- Localization : Self
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : Double move speed this turn

22. Knee Strike

- Mediums : leg #2
- Cinetic Points : 2
- Main MM : Pugilatus
- Secondary MM : Brawling / Low Blow
- Type : Attack
- Damage Type : contending
- Localization : Viscera / Genitories / Legs
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : None

23. Dodge

- Mediums : leg #3
- Cinetic Points : 2
- Main MM : Acrobatics
- Secondary MM : Athletics / Vigilance
- Type : Defense
- Localization : Self
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2
- Special Effect : Add LEVEL defenses to the next attack received this turn, then is spent
  (whether or not it turned the blow)

24. Jump

- Mediums : leg #4
- Cinetic Points : 1
- Main MM : Athletics
- Secondary MM : Acrobatics / Brute Force
- Type : Other
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : This turn, movement may cross hard obstacles — routes run straight through
  them instead of around. You cannot come to rest on one: a vault clears the rock, it does not
  perch on it, so obstacles open as a way through and never as a destination. The distance itself
  is still bought with Cinetic Points as usual.

25. Defensive Posture

- Mediums : leg #5
- Cinetic Points : 3
- Main MM : Vigilance
- Secondary MM : Battlecraft / Iron Nerves
- Type : Defense
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : Add LEVEL defenses to all the attacks received this turn

26. Push

- Mediums : arm #1
- Cinetic Points : 1
- Main MM : Brute Force
- Secondary MM : Brawling / Athletics
- Type : Attack
- Damage Type : contending
- Localization : Trunk
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : Pushback (1 step)

27. Elbow Strike

- Mediums : arm #2
- Cinetic Points : 2
- Main MM : Brawling
- Secondary MM : Pugilatus / Iron Fist
- Type : Attack
- Damage Type : contending
- Localization : Visage / Trunk
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1

28. Cleaving Strike

- Mediums : long blade #1 / axes #3
- Cinetic Points : 3
- Main MM : Swordsmanship
- Secondary MM : Brute Force / Battlecraft
- Type : Attack
- Damage Type : cutting
- Localization : visage / trunk / arms
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 2

29. Counter Strike

- Mediums : long blade #2 / saber #4
- Cinetic Points : 1
- Main MM : Tactics
- Secondary MM : Swordsmanship / Incisiveness
- Type : Attack
- Damage Type : cutting
- Localization : Trunk
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 3
- Special Effect : attack only executed if a melee attack is successfully defended this turn

30. Forward Lunge

- Mediums : long blade #3 / spear #1
- Cinetic Points : 2
- Main MM : Swordsmanship
- Secondary MM : Athletics / Battlecraft
- Type : Attack
- Damage Type : piercing
- Localization : Trunk
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 2
- Special Effect : Charge (3)

31. Feint

- Mediums : long blade #4 / saber #2
- Cinetic Points : 1
- Main MM : Tactics
- Secondary MM : Incisiveness / Swordsmanship
- Type : Other
- Localization : Self
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 3
- Special Effect : Do not damage, but all 6s rolled during a feint are added to the next attack of this turn

32. Snap Thrust

- Mediums : short blade #1 / saber #1
- Cinetic Points : 1
- Main MM : Swordsmanship
- Secondary MM : Incisiveness / Battlecraft
- Type : Attack
- Damage Type : piercing
- Localization : Trunk / Upper Limbs / Visage
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : None

33. Needle Thrust

- Mediums : short blade #2 / saber #3
- Cinetic Points : 2
- Main MM : Incisiveness
- Secondary MM : Swordsmanship / Tactics
- Type : Attack
- Damage Type : piercing
- Localization : CHOOSE
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2

34. Parry

- Mediums : short blade #3 / shield #2
- Cinetic Points : 1
- Main MM : Vigilance
- Secondary MM : Battlecraft / Swordsmanship
- Type : Defense
- Localization : Self
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 1
- Special Effect : Add LEVEL defenses to the next attack received this turn, then is spent
  (whether or not it turned the blow)

35. Deep Pierce

- Mediums : short blade #4 / spear #4
- Cinetic Points : 5
- Main MM : Incisiveness
- Secondary MM : Swordsmanship / Predator
- Type : Attack
- Damage Type : piercing
- Localization : CHOOSE
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 3
- Special Effect : Bleeding (3)

36. Smash

- Mediums : blunt weapon #1
- Cinetic Points : 1
- Main MM : Brute Force
- Secondary MM : Battlecraft / Brawling
- Type : Attack
- Damage Type : contending
- Localization : Visage / Trunk / Encephalon
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : None

37. Crushing Blow

- Mediums : blunt weapon #2 / pickaxes #4
- Cinetic Points : 3
- Main MM : Brute Force
- Secondary MM : Battlecraft / Iron Fist
- Type : Attack
- Damage Type : contending / piercing
- Localization : Visage / Trunk / Encephalon
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 2

38. Heavy Strike

- Mediums : blunt weapon #3 / axes #2
- Cinetic Points : 4
- Main MM : Battlecraft
- Secondary MM : Brute Force / Swordsmanship
- Type : Attack
- Damage Type : contending / cutting
- Localization : CHOOSE
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 2
- Special Effect : Pushback

39. Mighty Swing

- Mediums : blunt weapon #4 / pickaxes #3
- Cinetic Points : 5
- Main MM : Battlecraft
- Secondary MM : Brute Force / Ferocity
- Type : Attack
- Damage Type : contending / piercing
- Localization : CHOOSE
- Medium Level Multiplicator : 3
- Skill Level Multiplicator : 2
- Special Effect : knockdown

40. Chop

- Mediums : axes #1
- Cinetic Points : 2
- Main MM : Battlecraft
- Secondary MM : Brute Force / Swordsmanship
- Type : Attack
- Damage Type : cutting
- Localization : Upper Limbs / Lower Limbs
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2

41. Driving Lunge

- Mediums : axes #4 / spear #3
- Cinetic Points : 4
- Main MM : Battlecraft
- Secondary MM : Brute Force / Swordsmanship
- Type : Attack
- Damage Type : cutting / piercing
- Localization : Trunk
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 2
- Special Effect : Charge (5)

42. Piercing Blow

- Mediums : pickaxes #1 / spear #2
- Cinetic Points : 3
- Main MM : Incisiveness
- Secondary MM : Battlecraft / Brute Force
- Type : Attack
- Damage Type : piercing
- Localization : CHOOSE
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2

43. Quickshot

- Mediums : bows #1
- Cinetic Points : 1
- Main MM : Marksman
- Secondary MM : Athletics / Battlecraft
- Type : Attack
- Damage Type : piercing
- Localization : Trunk
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : None

44. Pinpoint Shot

- Mediums : bows #2 / crossbows #2
- Cinetic Points : 3
- Main MM : Marksman
- Secondary MM : Deadeye / Tactics
- Type : Attack
- Damage Type : piercing
- Localization : CHOOSE
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2

45. Longshot

- Mediums : bows #3
- Cinetic Points : 4
- Main MM : Deadeye
- Secondary MM : Marksman / Tactics
- Type : Attack
- Damage Type : piercing
- Localization : Trunk / limbs / visage
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2
- Special Effect : double range

46. Sighted Shot

- Mediums : crossbows #1
- Cinetic Points : 2
- Main MM : Marksman
- Secondary MM : Deadeye / Tactics
- Type : Attack
- Damage Type : piercing
- Localization : heart
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 2

47. Deadeye Shot

- Mediums : crossbows #3
- Cinetic Points : 5
- Main MM : Deadeye
- Secondary MM : Marksman / Incisiveness
- Type : Attack
- Damage Type : piercing
- Localization : Trunk / limbs / visage
- Medium Level Multiplicator : 2
- Skill Level Multiplicator : 3

48. Cover

- Mediums : shield #1
- Cinetic Points : 2
- Main MM : Vigilance
- Secondary MM : Battlecraft / Iron Nerves
- Type : Defense
- Localization : Self
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1
- Special Effect : Add LEVEL defenses to all the attacks received this turn. Breaks the moment a
  blow gets through.

49. Shield Bash

- Mediums : shield #3
- Cinetic Points : 2
- Main MM : Battlecraft
- Secondary MM : Brute Force / Brawling
- Type : Attack
- Damage Type : contending
- Localization : Visage / Trunk
- Medium Level Multiplicator : 1
- Skill Level Multiplicator : 1