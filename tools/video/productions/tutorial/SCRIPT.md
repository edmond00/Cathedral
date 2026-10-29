# The tutorial video: narration script (draft for approval)

Plain instructor voice (alan, R4). **E** is explanation. **R** is the game's own text, read in the
reading voice, exactly as it appears on screen. Every R passage is listed by `passages.py` from the
take's LLM logs, so the readings follow whatever the final take wrote; the examples quoted here are
from take A. **[D]** marks a manim diagram. Each rule stated is from the manual at HEAD (synced to the
code), chapter in brackets.

Sections follow the take's clips, in order.

---

## 1. New run

*Footage: the main menu, New is clicked.*

E: This is Proscribed Palimpsest, a narrative role-playing game. Everything you read in it is written
while you play, by a language model running on your own computer. This video plays the first hour of
a run, from the main menu to a pair of new boots, and explains each system as it comes up.

E: A word about that model first, because it is unusual. It runs entirely on your machine: no account,
no server, nothing sent anywhere. It runs on the processor alone, or on any graphics card, and the
game checks your hardware the first time it starts. It does not run the game. Rules, dice, wounds,
coin and time are ordinary code. The model is asked narrow questions inside that code: what this mind
notices, what it wants, how it tells what happened.

## 2. Selecting a world

*Footage: the star-field, two moons pointed at, Fenumoth chosen, its card, confirm.*

E: A run begins in the sky. Each of these moons is a world: three hundred and eighty-three of them,
each with its own name. The sky is the same on every machine, so a moon always names the same world,
and two players who pick the same moon walk the same land, with the same towns and the same people.

E: The world is generated when you choose it, history included: realms, castles and temples are laid
down first, and the farmland, villages and cities spread out from them. We take Fenumoth, a wooded
world.

## 3. Creating the protagonist: the body

*Footage: the anatomy screen, before any point is spent.*

E: Characters have no strength or charisma scores. A character is a body: five regions, twenty-two
organs, twenty-eight organ parts. Every part starts at one, and you have twenty-five points to raise
them. That is less than half of what the body could reach, so creating a character means choosing what
to give up.

E: Every measure the game uses comes from one organ or one region. Hovering an organ shows what it
feeds, on the right. We'll go through them by the system each one serves.

**[D] Organs → systems.** The five regions on the left; the systems on the right (thinking and memory,
the senses, speech, the humors, life, the body in a fight); one line per organ to what it feeds,
lighting group by group as the footage reaches it.

### Thinking and memory

*Footage: anamnesis, cerebrum, cerebellum raised; hippocampus and pineal gland pointed at.*

E: The encephalon, the whole head region, sets your noetic points: how many times you can look and
think in one scene. Its score halved, plus one.

E: What you learn are called modi mentis: ways of thinking. They are kept in five memories. New ones
arrive in working memory, which is sized by the whole encephalon and works as a queue: when it is full,
the oldest is lost for good. To keep one, you file it into the long-term memory of its kind: the
cerebellum holds practised skills, the cerebrum knowledge, the hippocampus the senses. The anamnesis
holds what you set aside, and also how many places you remember: four per point.

E: The pineal gland sets how much experience each level costs, from twelve points down to six. From
two points on, it also lets a mind doubt its own plan before it acts.

### The senses

*Footage: eyes, ears raised, nose.*

E: The eyes set the reach of a ranged blow. The ears add to your initiative in a fight. The nose
lowers the chance of meeting danger on the road, ten per cent per point, and tells you what is in a
food before you eat it.

### Speech

*Footage: tongue raised twice, teeth.*

E: The tongue sets how many replies you are offered in a conversation, up to five. With the tongue at
nought you cannot hold a conversation at all. The teeth only decide whether you can eat hard food.

### The humors

*Footage: paunch, hepar, spleen, pulmones pointed at; viscera raised four times.*

E: Four organs make humors: the paunch, the hepar, the spleen and the pulmones. Each keeps a queue of
forty-nine. Humors pay for effort, like each step of a journey, and they can change a die after it is
rolled. We'll see that at the first roll.

E: The viscera set how many humors you can spend on a single roll, and how fast wounds close. We raise
them to five.

### Life

*Footage: heart raised four times, genitories raised.*

E: The heart sets your lifespan, how many companions you can lead, and a share of youth humor at the
start, which is warm and lucky. The genitories add dice to every blow you strike.

### The fighting body

*Footage: backbone, right hand raised; arms, legs raised, feet.*

E: The backbone sets what you can carry, and your movement points in a fight: double its score. The
hands decide which tools you can use, and how well. The legs set how far you move in a fight; the
whole lower body sets how far you can travel in one journey, and your natural defence. The feet keep
you standing on bad ground and get you out of a fight.

E: The trunk, as a region, sets your health: a third of its score, plus one. A wound takes one point of
health and does not come back until it heals.

## 4. Childhood

*Footage: five memories. Each one: a fragment appears, a keyword is clicked, a way of thinking chosen,
the act, the outcome.*

E: The body has no past yet. The game gives it one through five childhood memories, played the way the
whole game is played. This first one is the clearest example of a narration turn, so we'll take it
slowly.

R: the opening observation. ("I half-remember sobbing, a muffled weeping from a woman's voice,
somewhere in the dark…")

E: That passage was written by a modus mentis, one way of thinking, which the model plays as a
character in its own voice. The lit word is a keyword. Clicking it means thinking about what it names.

**[D] A narration turn.** Observe → keyword → think with a modus mentis → a goal → the acting mind says
how willing it is → the dice → the outcome, written in the acting mind's voice. A counter of noetic
points beside it: one spent on each thought and on each extra observation; refilled when the scene
moves on, except after a failed roll.

E: Each step is a different mind. The observing mind chose what to notice. The thinking mind now
reasons, step by step, in its own voice, and picks a goal from what the code says this thing allows.
The acting mind says how willingly it will do it, and that changes the difficulty. The model makes
the choices; the code decides what is possible, how hard it is, and what happens.

R: the thinking mind's reasoning. ("I remember half-echoes of weeping…")
R: the acting mind's intent. ("I will try to remember the sobs…")
E: In childhood nothing is rolled: remembering always succeeds.
R: the outcome. ("I tried to dredge up the whispers of my childhood…")

E: Each memory gives something real. This one gives the city we grew up in.

*Memories two to five, shorter: each R passage is read; one line of E each.*

E (2): The second memory gives work on the docks, and with it what that work taught.
E (3): Something sharp: a short sword, bought with a first wage. It will be in our hand.
E (4): A dream of a golden arch: a traveller's backpack, with bread and cheese inside.
E (5): Shelter gives nothing to carry. Not every memory does.

## 5. Getting up

*Footage: the waking memories, the keyword, the act, the dice, continue.*

E: Childhood ends and the body wakes on open ground. Getting up is the first real roll.

R: each passage in turn (the memory, the reasoning, the intent).

**[D] Dice and humors.** One die per level of every mind in the chain: the one that observed, the one
that thought, the one that acts. The difficulty is how many sixes you need. Then the humors: only the
humor at the back of each queue can be spent, one per die, as many as the viscera allow. Blood adds
one, yellow bile takes one away, phlegm does nothing but clears the way. Black bile does nothing and
cannot leave. A queue of nothing but black bile is spent; all four, and the body dies.

E: [take-specific] Here: four dice, one six needed. [Two sixes: it succeeds without help. If the
roll fails in the final take: the humor spent, die by die.]

R: the outcome.

## 6. The run begins: the protagonist screen

*Footage: Escape, Protagonist; anatomy tab, memory tab.*

E: Escape opens the protagonist screen. The anatomy, with any wounds. Memory: the modi mentis learned
in childhood, filed in the memory of their kind.

### Inventory

*Footage: the sword in hand, the backpack opened, the bread eaten.*

E: The inventory. The sword is held. The backpack is worn, and things are stored in it. We eat the
bread.

### Humors

*Footage: the humors tab; the newest humors in the paunch pointed at.*

E: Eating pushes the food's humors into the paunch, at the front of its queue. Each humor shows its
warmth, which pays for effort, and what it does to a die. They reach the back, where they can be
spent, as the older ones are used up.

## 7. Travel to the farm

*Footage: the world map, a destination, the route, go.*

E: The map. You can set a destination as far as your legs allow, and the route goes cell by cell. Each
cell costs days and warmth, drawn from the humors as you walk, and each land has its own dangers: a
plain has wolves and bandits, a forest more of both.

## 8. The first fight

*Footage: the wolf, engage, two turns ended, five snap thrusts, the wolf dies.*

E: A wolf on the road. A fight is played on a grid, in turns. Your movement points come from the
backbone. The wolf is far off, so we wait for it to come.

E: Fighting skills come from the modi mentis that unlock them and from what you strike with. The sword
gives Snap Thrust.

**[D, short] Attack against defence.** The attack: the skill's dice, plus the genitories. The defence:
the lower limbs, guards, and the armour on the part struck. More sixes than the defender wounds; a tie
goes to the defender.

E: [take-specific, per blow: a hit, a miss, a humor spent on a failing roll.] The wolf falls on the
fifth thrust. The journey goes on.

## 9. The farm: asking for work

*Footage: arrival in the longhouse hall, the farmer, diligence, the roll, the conversation, the
resolution.*

R: the arrival observation, and the one after.

E: We need coin for boots. The farmer, Helen, gives work. We think about her with Diligence. Diligence
is a will: a mind that wants one thing, here asking for work. It looks for the shortest way to get
there, and carries the act out itself, eagerly, which lowers the difficulty by one.

R: diligence's reasoning, intent, outcome.

E: Asking succeeded, so a conversation opens. Helen speaks; each reply on offer was written by a
different speaking mind we hold, each in its own voice. The tongue decides how many are offered.

R: Helen's line; each reply offered; the one chosen. (Repeated per exchange.)

E: The conversation ends in one roll. The pool: Helen's opinion of us, the levels of every mind behind
the chosen replies, garments that suit her standing, and the visage. Asking for work is a matter of
consequence: four or five sixes, by how long the branch ran. [take-specific: the count here.]

E: [take-specific: the dice, any humor spent.]

## 10. The job

*Footage: the work screen, two hundred days, confirm.*

E: Work is not a quest. You trade days for coin and experience. A farmhand earns one copper every ten
days, so two hundred days earns twenty copper: the price of boots. The job trains three modi mentis,
the first the most. The calendar moves on by the days worked.

## 11. To the city, and the second wolf

*Footage: the map, the route; the fight.*

E: To the city. Another wolf, this one already beside us. [take-specific: the blows.]

## 12. The city: buying boots

*Footage: the cobbler's shop, Michael the cobbler, provisioning, the roll, the conversation.*

R: the arrival observation, and the one after.

E: Provisioning is another will: it wants to buy. Its course is a proposal to buy from the cobbler.

R: the reasoning, the intent.

E: [In take A the roll failed, and the humors turned it: the humors explained over this roll if it
is the first one that needs them.]

R: the outcome.

R: the conversation, as at the farm.

E: Proposing a trade is a formality: one or two sixes, by the branch's length. [take-specific.]

## 13. Trading

*Footage: the trade screen, the boots, confirm.*

E: The cobbler's shelf. Prices are in one coin each, copper, silver or gold, with no exchange between
them. The boots cost twenty copper.

## 14. Wearing the boots

*Footage: the inventory, the boots on the feet.*

E: A garment goes straight to its place when that place is free. Garments can protect, carry, and
please people of a given standing, which adds a die in conversation with them.

## 15. Routines

*Footage: the chorography tab, the farm's map, its routines; then the world map's routines box.*

E: The purse is empty again. Each place you visit is kept as a mental map, and each map holds
routines: ways back in. Asking for work taught us one. From the world map, the farm's routines are one
click from the route: the next visit can go straight to work, with no conversation first.

## 16. Closure

*Footage: the world map, held.*

E: That is the first hour: a body built, a past remembered, two fights, a job, a trade, and a way
back. Proscribed Palimpsest, at edmond00 dot itch dot io slash proscribed.

---

## Open questions

1. **Reply options**: read every reply offered (the developer's "every passage"; about three per
   exchange, roughly six exchanges in all), or only the speaker's line and the reply chosen?
2. **The GPU claim**: "on the processor alone, or on any graphics card." Is that true of the shipped
   build (the Vulkan backend), or only the CPU and certain cards?
3. **The cobbler roll**: in take A the model wrote a failure over a success (1 rewrite in 253
   across all takes). Take B is recording; the script above is written so either take works.
