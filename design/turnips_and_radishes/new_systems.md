# NEW TRANSVERSAL SPOTS

- currently : doors (OPEN/UNLOCK verb)
- new kind of spots relying locations to add
  - road like :
    - use with FOLLOW verb
    - example : path, river, ...
  - cliff like : 
    - used with CLIMB_UP and CLIMB_DOWN verb, higher difficulty
    - different verb depending on the direction
    - example : ice cliff, rock cliff, ...

# TRAVELLING NPC

- Allow some npc to be a travel between locations schedule
- example : woodcutter/miners travelling from cave/forest to village to sell ore/wood


# SERVICE FROM NPC

- special dialogue tree to get special service from an NPC (hospitality, inhury care, ...)


# WORK FOR NPC

- spcial service/dialogue tree to request to work for a master/reeve
- work effect (salary, experience)
- work UI menu where you have :
  - duration slider to choose work duraiton in months
  - info on work effect for the selected duration
    - salary
    - new skills / skill experience
    - affinity gain with master, if master
# BUSINESS

- special service opening special menu UI to sell or buy items with an NPC

# VISITED LOCATION SHORTCUT

- allow to skip narration to directly get the outcome of an action that was already succeeded before :
  - meet already known npc
  - receive service from npc that was laready received before
  - go to already known area of this location

# HUMOR CONSUMPTION DURING SKILL CHECK

consume humor to change a dice value of a skill check dice roll

# BETTER TRAVEL

## NO OCEAN/SEA TRAVEL

Forbid to travel through sea and ocean.
Will be implemented in futur vesrion through ship acquisition.

## TRAVEL WAYPOINTS

When clicking on a world map cell, the travel does not directly start.
Instead a way point is set on the world map.
Up to 4 (4 will be a setting in the config) waypoints can be set by clicking multiple times.
If clicked more than 4 times, first waypoint is removed so the new waypoint can be added without the queue being more than 4.
By cliking on the same cell a second time, the waypoint is removed (other waypoints of other cells remains).
The waypoints form a queue, the last clicked waypoint is the final destination.
On the bottom of the screen, a new UI box will show some info about the currently set travel (risk of encounters, travel duration, risk of starvation, ...).
This box will also contains a "TRAVEL" button, if clicked travel will started, going from each waypoints one after the other, until last waypoint is reached.

## TRAVEL VITAL HEAT CONSUMPTION

travelling will consume as much humor as needed depending on the travel length and travelled biomes.
Each biome need a specific vital heat to be crossed.
humors will be comsummed to allow crossing until the biome vital heat level is reached (humor with negative vital heat increase required vital heat).
If during travel, the protegonist body humors become filled with black bile, death by starvation is called.
A risk of death by starvation is estimated before travel start and diplayed on the travel UI box.

## TRAVEL ENCOUNTERS

risk of ennemies attack during travel.
each biome have list of possible ennemies encounters (wolfs, bears, ...) and small % chance encounters happens.
the total chance of encounter is estimated before a travel and displayed on the trave UI box.


# MODUS MENTIS EXPERIENCE AND EXPERTISE

- modus mentis when used to succeed in an action gain xp
- if enough xp, level up

# FOOD/DRINK/SMOKE CONSUMPTION

- some items can be consumed (eat, drink or inhale)
- consumable have chemical properties = inqueue humors when consumed

# NEW ACTION OUTCOME

- succeeding some action can give new modus mentis
- failing/succeeding some action can inqueue some humors

# WATCH SYSTEM

- some action illegal
- npc can detect illegal action if preset in the area
- trigger special dialogue tree that can ends up in a fight

# CORPSE BUTCHERY SYSTEM

- killing npc, either after fight or narrative action, spawn its corpse, depending of its anatomy
- corpse can be cut to collect some of its part (meat, ...)

# CHILDHOOD REMINESCENCE

- special modus mentis, first modus mentis of the protagonist
- can be used during childhood reminescene phase to remember childhood info
- after childhood phase, its content is overwrittent to fit the specific childhood of the protagonist

# NARRATION FALLBACK OUTCOME

- default outcome of a narration frontend scene when no action nether succeed or fail (player spend all noetic points on observation for example)
- example : leave the location, attacked by a present ennemy, transit to other narration scene...
- bottom button on the narration menu allowing to directly trigger this default outcome