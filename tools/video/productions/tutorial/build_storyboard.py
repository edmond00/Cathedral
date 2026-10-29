"""Writes storyboard.json for the tutorial from the narration (here, as code) and the take.

    python build_storyboard.py            # -> storyboard.json, and SCRIPT.md (the narration, readable)

The take is build/rec_tutorial_b. Its passages (passages_take_b.json, made by passages.py with
names_take_b.json) are read aloud in the "reading" voice. A segment runs from the moment before a
passage starts being written to the moment it sits whole on screen, and its reading waits for the end
of the picture ({"wait": true}): the explanation, if any, speaks while the model writes, and the
reading starts once the text can be read. Segments end before a press that changes the screen, so a
held frame never shows the next screen.

How each feature is explained: what it is for, how it works, what it did in this run, and, where a
player would want it, advice. Every rule stated is the manual's (docs/manual, derived from the code);
every number of the run is read from the take (its game.log, timeline.jsonl and frames).

Times below are moments of take B's timeline: clicks (timeline.jsonl) and passages (passages_take_b.json).
Another take means new times and new numbers: the lines that describe this run are marked TAKE.
"""
import json
from pathlib import Path

HERE = Path(__file__).parent
P = [p for p in json.loads((HERE / "passages_take_b.json").read_text())
     if "CHOOSER" not in p["voice"] and "MATCHER" not in p["voice"]]
EVENTS = [json.loads(l) for l in (HERE / "build/rec_tutorial_b/timeline.jsonl").read_text().splitlines() if l.strip()]
CLICKS = sorted(e["t"] for e in EVENTS if e["type"] == "click")

segments = []


SHOWS = 1.5    # seconds between the model's first word and the first word on screen


def R(i, anchored=True):
    """Passage i, read in the reading voice from the moment it starts appearing on screen."""
    part = {"text": P[i]["text"], "style": "reading"}
    if anchored:
        part["at"] = round(P[i]["ts"] + SHOWS, 2)
    return part


def W(text):
    """An explanation that waits for the picture too (said after a passage, or before it, once shown)."""
    return {"text": text, "wait": True}


def end(i):
    """When passage i sits whole on screen: just after the press that sets it into the page, when one
    follows within a few seconds (the text stops typing then), else a moment after it was written."""
    t1 = P[i]["t1"]
    nxt = next((c for c in CLICKS if t1 - 0.2 <= c <= t1 + 4.0), None)
    if nxt is not None:
        return nxt + 0.8
    following = [p["t0"] for p in P if p["t0"] >= t1 - 0.05 and p is not P[i]]
    return min([t1 + 2.5] + following)


def game(t0, t1, *vo, fade=False, rec="tutorial", **kw):
    seg = {"type": "game", "recording": rec, "from": round(t0, 2), "to": round(t1, 2), "fade": fade}
    if vo:
        seg["vo"] = list(vo)
    seg.update(kw)
    segments.append(seg)


def manim(scene, *vo, **kw):
    segments.append({"type": "manim", "scene": scene, "vo": list(vo), **kw})


def reply(t0, i, mind, *before):
    """One reply on offer, introduced by the mind that wrote it as it starts appearing. Returns where it ends."""
    game(t0, end(i), *before, {"text": mind + ".", "at": round(P[i]["ts"] + SHOWS, 2)}, R(i, anchored=False))
    return end(i)


FIGHT = 2.0    # a fight's own moves play at twice their speed; the model's waits at eight

# ── 1-3. the menu, the world, the body: from the creation take ─────────────────
# rec_creation plays tutorial.cli's opening alone (creation.cli). Nothing in it is written by the model, so
# it was re-shot when the CLI's element locator was fixed (it had put the cerebellum's clicks on empty
# screen), and its moments are looked up by what happened rather than typed in.
C_EVENTS = [json.loads(l) for l in (HERE / "build/rec_creation/timeline.jsonl").read_text().splitlines() if l.strip()]


def c_at(kind, label, nth=0):
    """When the creation take's nth event of this kind and label happened."""
    return [e["t"] for e in C_EVENTS if e["type"] == kind and (e.get("label") or e.get("name")) == label][nth]


def c_game(t0, t1, *vo, fade=False):
    game(t0, t1, *vo, fade=fade, rec="creation")


c_game(c_at("clip-begin", "new_run"), c_at("click", "New") - 0.2,
       "Your character in this game is not a sheet of numbers. It is a body, built organ by organ, and a crowd of "
       "minds living in it: some notice, some think, some act, and some only feel. Each one speaks in its own "
       "voice, and nothing they say is written in advance.",
       "This is Proscribed Palimpsest. In the next hour of play, from the main menu to a pair of new boots, we'll "
       "see how all of it works. And along the way, I'll tell you what I would do as a player.", fade=True)
c_game(c_at("click", "New") - 0.2, c_at("move", "Fenumoth") - 0.2,
       "A run starts by choosing a world. Each moon in this sky is one: three hundred and eighty-three of them, "
       "each with a name. The sky is fixed, the same on every machine, so a moon always stands for the same world. "
       "Two players who pick the same moon walk the same land and meet the same people, so they can compare notes.")
c_game(c_at("move", "Fenumoth") - 0.2, c_at("click", "world-confirm") - 0.4,
       "Choosing a moon generates its world, and its history comes first. Realms rise and fall; castles, temples "
       "and cities are founded; then farmland, villages and pastures spread out from them. The card says what kind "
       "of world it is. We take Fenumoth, a wooded world. For a first run, any moon will do: every one of them is "
       "built to be livable.")
c_game(c_at("click", "world-confirm") - 0.4, c_at("clip-begin", "creation_overview"))
c_game(c_at("clip-begin", "creation_overview"), c_at("clip-end", "creation_overview"),
       "Now the protagonist. There are no strength or charisma scores in this game. A character is a body: five "
       "regions, which are the head, the face, the trunk, the arms and the legs; twenty-two organs; twenty-eight "
       "organ parts. Every part starts at one point, and you get twenty-five more to spend. That buys less than "
       "half of what the body could reach, so creating a character is really choosing what to give up.", fade=True)
manim("Organs",
      "Every number the game uses comes from one organ or one region. This map groups them by the system they "
      "feed: thinking and memory, the senses, speech, the humors, life, and the body in a fight. In the game, "
      "hovering an organ shows the same information on the right of the screen.",
      "We'll go through them in that order. Watch where the points go: this character is built to think, to "
      "talk, and to bend the dice.")
ORGAN_WALK = [  # (the organ hovered first, what is said while it is shown)
    ("anamnesis", ["First, thinking and memory: the head. As a whole, the head sets your noetic points, how many times you can "
     "look and think in one scene: its score halved, plus one.",
     "What you learn in this game are called modi mentis: ways of thinking. A new one always arrives in working "
     "memory, a short queue, also sized by the whole head. When that queue is full, the oldest way of thinking "
     "falls out, and it is lost for good.",
     "The anamnesis keeps a second queue, for what you set aside, and it decides how many places you remember: "
     "four per point. We raise it by one."]),
    ("cerebrum", ["To keep a way of thinking for good, you file it into a long-term memory of its kind. The cerebrum holds "
     "knowledge, like bargaining or archaeology. We give it two points."]),
    ("cerebellum", ["The cerebellum holds practised skills, like swordsmanship or knotwork, and it is what lets you learn a "
     "new move in the middle of a fight. Two points as well."]),
    ("hippocampus", ["The hippocampus holds the ways of thinking that use the senses."]),
    ("pineal_gland", ["And the pineal gland sets how much experience a level costs, from twelve points down to six. With two "
     "points or more, it also lets a mind doubt its own plan and think again.",
     "My advice: don't neglect memory. A character who learns a lot but has nowhere to file it keeps "
     "forgetting."]),
    ("left_eye", ["The senses. The eyes set the reach of a ranged blow, in cells."]),
    ("left_ear", ["The ears add their score to your initiative, which decides who acts first in a fight. We raise both."]),
    ("nose", ["The nose is underrated. Each point lowers the chance of meeting danger on the road by ten per cent, and it "
     "tells you which humors are in a food before you eat it."]),
    ("tongue", ["Speech. The tongue sets how many replies you are offered in a conversation, up to five. At nought, you "
     "cannot hold a conversation at all. Two points here: conversation is how you get work, trade and friends."]),
    ("teeths", ["The teeth only decide whether you can eat hard food. We leave them."]),
    ("paunch", ["The humors are this game's most unusual system. Four organs make them: the paunch, the hepar, the spleen "
     "and the pulmones. Each keeps a queue of forty-nine humors. A sound organ makes mostly blood; a failing one, "
     "mostly black bile.",
     "Humors are your fuel: every step of a journey burns some of their warmth. And they are your luck: a humor "
     "can change a die after it has been rolled."]),
    ("viscera", ["The viscera decide how many humors you may spend on a single roll, and how fast wounds close. Four points "
     "here, for five in all. I would always do this: it is the difference between a failed roll and a saved one."]),
    ("heart", ["Life. The heart sets how long you live, how many companions you can lead, one per point, and how many "
     "youthful humors you start with, which are warm and lucky. Four points: a long life, and room for a party "
     "later."]),
    ("genitories", ["The genitories add dice to every blow you strike. One point."]),
    ("backbone", ["The body in a fight. The backbone sets how much you can carry, and your movement points in a fight: twice "
     "its score. Three more points."]),
    ("right_hand", ["The hands decide which tools you can use. A practised hand does not get more out of a tool; it can make "
     "more things serve as tools. Two points for the right hand."]),
    ("left_hand", ["In a fight, each hand and each foot strikes on its own, and the arms are the organs of brute force."]),
    ("left_leg", ["The legs set how far one movement point takes you. With the feet, the lower body as a whole sets how far "
     "one journey can reach, and your natural defence."]),
    ("left_foot", ["The feet keep you standing on bad ground, and roll the dice to run away. One last number: health. It "
     "comes from the trunk as a whole, a third of its score plus one, and each wound costs a point of it until "
     "it heals. Twenty-five points spent. That is the body."]),
]
for k, (organ, vo) in enumerate(ORGAN_WALK):
    t0 = c_at("move", "organ:" + organ)
    t1 = (c_at("move", "organ:" + ORGAN_WALK[k + 1][0]) if k + 1 < len(ORGAN_WALK)
          else c_at("clip-end", "organs_fighting"))
    c_game(t0, t1, *vo, fade=(k == 0))

# ── 4. childhood ──────────────────────────────────────────────────────────────
game(202.6, end(0),
     "The body has no past yet. The game gives it one through five childhood memories, and they are played "
     "exactly the way the whole game is played. So the first memory is our first lesson. Let's take it slowly.",
     R(0), fade=True)
game(end(0), 312.2,
     "That text was not written in advance. It was written a moment ago, by a language model running on this "
     "computer. It runs entirely on your machine: no account, no server, nothing sent anywhere, and it works on "
     "the processor alone.",
     "But the model does not run the game. The rules, the dice, wounds, coin and time are ordinary code. The "
     "model is asked narrow questions inside that code: what does this mind notice, what does it want, how does "
     "it tell what happened.",
     "Each question is put to a modus mentis, a way of thinking, and the model plays that way of thinking as a "
     "character, in its own voice. That is why every passage is headed by a name. This one is Childhood "
     "Reminiscence.")
manim("NarrationTurn",
      "Here is a turn: the loop you repeat all game. A mind observes, and writes what it notices. Some words in "
      "what it wrote light up: keywords. You click one. A thinking mind reasons about it and settles on a goal, "
      "chosen from the actions the code says that thing allows. Then an acting mind says how willing it is, and "
      "its willingness makes the roll easier or harder. The dice decide, and the acting mind tells how it went.",
      "Thinking costs a noetic point, and so does every extra look. The points come back when the scene moves "
      "on, but not after a failed roll: a miss costs something.")
game(312.2, end(1),
     "We click sobs, choose Think, and pick the mind that will think. For now, there is only one.", R(1))
game(end(1), end(2), "The thinking mind has its goal. Now the acting mind puts it into words.", R(2))
game(end(2), 533.0, "The act is offered as a line. We click it, then Execute.")
game(533.0, end(3),
     "In childhood nothing is rolled: remembering always succeeds.", R(3),
     W("Look under the passage, at what this memory gave: three ways of thinking, Empathy, Nautical Jargon and "
       "Knotwork, and a townsman's clothes: cap, cloak, tunic and breeches. The text said a port city. The code "
       "decided what that means for your character. Every passage works like this: the words are the model's; "
       "what they give you is the game's."))
game(end(3), 657.3)

# memory two
game(657.3, end(4),
     "The second memory. Look at the heading: the observer is now Empathy, one of the minds we just gained. A "
     "new mind notices different things.", R(4), fade=True)
game(end(4), end(5), "We click waves, and think with Childhood Reminiscence again.", R(5))
game(end(5), end(6), R(6))
game(end(6), end(7), R(7), W("This one gives Hard Labor. The docks taught us to work."))
game(end(7), 1242.9)

# memory three
game(1242.9, end(8), "The third memory: something sharp.", R(8), fade=True)
game(end(8), end(9), R(9))
game(end(9), end(10), R(10),
     W("Look at what the acting mind wrote: a sharp taste, like a sour apple. The model drifted from a blade to "
       "an apple. It does not matter: the act is still remembering something sharp, and the code knows what that "
       "memory holds. I find these little slips part of the charm."))
game(end(10), end(11), R(11), W("And there it is: a short sword, and Bargaining."))
game(end(11), 1657.4)

# memory four
game(1657.4, end(12), "The fourth: a dream of a golden arch.", R(12), fade=True)
game(end(12), end(13), R(13))
game(end(13), end(14), R(14))
game(end(14), end(15), R(15),
     W("A traveller's backpack, with bread and cheese inside, and Archaeology. Keep the bread in mind."))
game(end(15), 2186.7)

# memory five
game(2186.7, end(16), "The last memory: shelter.", R(16), fade=True)
game(end(16), end(17), R(17))
game(end(17), end(18), R(18))
game(end(18), end(19), R(19),
     W("This one gives no item, only two ways of thinking: Exploration and Endurance. My advice for your own "
       "childhood: read the fragments, and pick what you want your character to be. The memories you choose are "
       "your starting kit."))
game(end(19), 2673.0)

# ── 5. getting up ─────────────────────────────────────────────────────────────
game(2673.0, end(20),
     "Childhood is over. The body wakes on open ground, spent, and a new mind takes our bearings: Childhood "
     "Memory.", R(20), fade=True)
game(end(20), end(21), R(21), W("Getting up is the first real roll of the game."))
game(end(21), end(22),
     "We think about the legs, this time with Nautical Jargon, one of the minds from the docks. Listen to how "
     "differently it reasons.", R(22),
     W("Everything is rope, knots and tides. That is what a persona does: the same question, put to a different "
       "mind, comes back in a different voice."))
game(end(22), end(23), "Knotwork, another lesson of the docks, will act.", R(23))
game(end(23), 3121.0,                                                    # TAKE: four dice, two sixes
     "We click the act, and Execute. The dice: four of them, and two sixes.")
manim("DiceHumors",
      "Every roll works the same way. You roll one die for each level of every mind in the chain: the one that "
      "observed, the one that thought, and the one that acts. The difficulty is the number of sixes you need. So "
      "raising any of the three minds means more dice, and better odds.",
      "When a roll falls short, the humors can save it. Only the humor at the back of each queue can be spent, "
      "one per die, and only as many in one roll as the viscera allow. Blood adds one to a die. Yellow bile takes "
      "one away. Phlegm does nothing, but spending it clears the way to the humor behind it. Black bile does "
      "nothing, and never leaves. A queue full of black bile is spent, and when all four are, the body dies. So "
      "humors are your luck, and also your life.",
      "Here, two sixes were enough to stand up without help.")
game(3121.0, end(24), R(24),
     W("Success. Look at the lines below: every mind in the chain gained a point of experience, and the act "
       "taught a new one, Vigor. Doing things is how you learn."))
game(end(24), 3223.0,
     "The run has truly begun, and we are on the world map. Before travelling, let's look at what we have. "
     "Escape opens the menu, and Protagonist opens the character screen.")

# ── 6. the protagonist screen ─────────────────────────────────────────────────
game(3223.0, 3236.0, "The anatomy tab: the body we built. Wounds will show here, on the organ they hit.",
     fade=True)
game(3236.0, 3246.0,
     "The memory tab: everything we learned, filed in the memory of its kind. There are also two wills here, "
     "Diligence and Provisioning. A will is a mind that wants one thing; we'll see both at work.",
     "My advice: check this tab often, and file new ways of thinking before working memory pushes them out.")
game(3246.0, 3270.5,
     "The inventory. The sword is in hand, the townsman's clothes are worn, and so is the backpack, which holds "
     "things. We open it, pick the bread, and eat it.")
game(3270.5, 3294.0,
     "Eating pushed the bread's humors into the paunch, at the front of its queue. Hover a humor to see what it "
     "is: its warmth, which pays for effort, and what it does to a die. New humors enter at the front and move "
     "back as older ones are used, and only the one at the very back can be spent on a roll.",
     "My advice: what you eat today is the luck you will have a few days from now.")
game(3294.0, 3297.7)

# ── 7. the road ───────────────────────────────────────────────────────────────
game(3297.7, 3318.1,
     "Back on the map, we pick the farm as our destination. A route goes cell by cell, and every cell costs days, "
     "and warmth drawn from the humors as you walk. Each land has its dangers: wolves and bandits on a plain, "
     "more of both in a forest, and a keen nose avoids some of them. Before a long journey, check that your "
     "humors will last it.", fade=True)

# ── 8. the first fight ────────────────────────────────────────────────────────
game(3318.1, 3330.0, "A wolf on the road. We choose to fight it.", speed=FIGHT, fade=True)
game(3330.0, 3357.0,
     "A fight is played on a grid, in turns. Your movement points come from the backbone, and the ears' "
     "initiative says who goes first: us. The wolf is far off. Rather than walk up to it, we end our turns and let "
     "it come to us. Fighting on your own terms is good advice in any game.", speed=FIGHT)
game(3357.0, 3373.6,                                                     # TAKE: five dice
     "Fighting skills come from two things: the ways of thinking you hold, and what you strike with. The short "
     "sword and Swordsmanship give Snap Thrust: five dice.", speed=FIGHT)
manim("AttackDefence",
      "Here is a blow. The attacker rolls the skill's dice, plus the dice of the genitories. The defender rolls "
      "dice for the lower body, for any guard or stance, and for the armour on the part struck. If the attacker "
      "shows more sixes, the blow lands, and it leaves a wound on the part it hit. A tie goes to the defender.",
      "A wolf wears no armour, and its defence is small.")
game(3373.6, 3442.0,                                                     # TAKE
     "The first three thrusts land. The log at the bottom names each wound: a broken backbone, then broken ribs. "
     "Watch the wolf's turns too: it tries to learn new attacks in the middle of the fight, and fails. Beasts "
     "learn the way you do.", speed=FIGHT)
game(3442.0, 3474.0,                                                     # TAKE
     "The fourth thrust falls short. Under the dice, the humors. At the back of the hepar is voluptas, the humor "
     "of pleasure, which turns any die into a six. We spend it on the first die, and the thrust lands.",
     speed=FIGHT)
game(3474.0, 3503.0,                                                     # TAKE
     "The fifth needs two humors, from the hepar and the spleen, and the wolf falls. Those were well spent: a "
     "fight that drags on costs warmth, and warmth is what carries you across the map.", speed=FIGHT)
game(3503.0, end(25),
     "The journey goes on to the farm. Arriving opens the place, and a mind takes our bearings.", R(25))
game(end(25), end(26), R(26), W("That is the farmer, Zedayne Hartmead. She can give us work."))

# ── 9. asking for work ────────────────────────────────────────────────────────
game(end(26), 3773.0,
     "We need twenty copper for boots, and the honest way to get coin is work. We click the farmer, choose "
     "Think, and pick Diligence.", fade=True)
game(3773.0, end(27),
     "Diligence is a will: a mind that wants one thing, here, to ask for work. It does not weigh options. It "
     "finds the shortest way from where we stand to what it wants, and proposes the first step.", R(27))
game(end(27), end(28), R(28))
game(end(28), 4029.5,                                                    # TAKE
     "A will that can act does its own act, and does it eagerly, which lowers the difficulty by one. One six "
     "needed; two come up.")
game(4029.5, end(29), R(29),
     W("Success, and a new way of thinking learned: Social Interaction. One honest remark: the passage says we "
       "talked under the stars, but it is the afternoon. The model's words are flavour; the facts are the "
       "game's."))
game(end(29), end(30),
     "Now a conversation. A conversation is a tree, written by hand. Each line has a fixed meaning, and the model "
     "rewrites it in the voice of whoever says it. Zedayne speaks first.", R(30))
t = reply(end(30), 31, "Diligence",
          "Then our replies. Each is one branch of the tree, voiced by one of the minds we hold that can speak. The "
          "tongue decides how many are offered: we gave it two points, so three.")
t = reply(t, 32, "Nautical Jargon")
game(t, end(33), {"text": "Provisioning.", "at": round(P[33]["ts"] + SHOWS, 2)}, R(33, anchored=False),
     W("Diligence and Provisioning were given the very same line: I am after work as a farmhand; what does it "
       "pay? Provisioning, which counts everything in days of supplies, turned it into grain, firewood and "
       "salt."))
game(end(33), end(34), "We choose Diligence's, the plainest.", R(34))
t = reply(end(34), 35, "Social Interaction")
t = reply(t, 36, "Diligence")
t = reply(t, 37, "Bargaining")
game(t, end(38), "We ask what the work is like, through Social Interaction.", R(38))
t = reply(end(38), 39, "Diligence")
t = reply(t, 40, "Nautical Jargon")
t = reply(t, 41, "Social Interaction")
game(t, 5004.4,                                                          # TAKE
     "We take Diligence's answer, and that ends the branch. Now the whole conversation is settled by one roll. "
     "Nine dice: two for what she thinks of us, five for the levels of the minds behind our three replies, and "
     "two for our face. Asking for work is a matter of consequence: after three replies, it needs four sixes. "
     "One comes up. We spend three humors, on three dice, and that makes four.")
game(5004.4, end(42),
     "My advice for conversations: say your piece in full. A longer branch adds more dice than difficulty.",
     R(42), W("She takes us on."))
game(end(42), 5088.1)

# ── 10. the job ───────────────────────────────────────────────────────────────
game(5088.1, 5118.3,
     "Work in this game is not a quest. You trade days of your life for coin and experience. A farmhand earns one "
     "copper every ten days, so two hundred days earn twenty copper, the price of boots. The work also trains "
     "three ways of thinking, the first one most.",
     "And the calendar moves on by every day worked. Your lifespan is counted in days, so time is the real price "
     "of work.", fade=True)
game(5118.3, 5119.3)

# ── 11. to the city ───────────────────────────────────────────────────────────
game(5119.3, 5139.2, "With twenty copper in the purse, to the city.", fade=True)
game(5139.2, 5184.0,                                                     # TAKE
     "Another wolf, and this one starts right beside us. The first thrust falls short of its defence, and a "
     "humor turns it.", speed=FIGHT, fade=True)
game(5184.0, 5256.0, "Again, and again: each thrust needs one humor to land.", speed=FIGHT)    # TAKE
game(5256.0, 5293.0, "The fourth needs three.", speed=FIGHT)                                 # TAKE
game(5293.0, 5320.0,                                                     # TAKE
     "The fifth lands, and the wolf falls. That was an unlucky fight: seven humors spent. A run of bad rolls "
     "can drain them, and when a fight goes badly, running away is always possible: one six on the feet's dice.",
     speed=FIGHT)
game(5320.0, end(43), "We reach the city, and the cobbler's shop.", R(43))
game(end(43), end(44), R(44),
     W("This is one of the model's strengths: from a few traits the game gives it, it draws a person. A boot "
       "over one knee, an argument about the gods, an old scar. The cobbler, Publiosh Tommaezoth, has a "
       "character."))

# ── 12. buying boots ──────────────────────────────────────────────────────────
game(end(44), 5679.0,
     "We think about him with Provisioning, our second will. Provisioning wants to buy.", fade=True)
game(5679.0, end(45), R(45),
     W("Provisioning sees everything in days of supplies. Its way to a purchase is to go and propose a trade."))
game(end(45), end(46), R(46))
game(end(46), 5941.9,                                                    # TAKE
     "This time the act was given to Diligence, and Diligence is reluctant: buying is not its business. "
     "Reluctance raises the difficulty by one, to two sixes. Five dice, and not a single six. Two humors, two "
     "sixes.")
game(5941.9, end(47), R(47),
     W("This is the passage I like least in the take. It sounds like a failure, but the roll succeeded. A small "
       "model sometimes bends the tone, and the game holds the result: the conversation opens all the same."))
game(end(47), end(48), R(48))
t = reply(end(48), 49, "Bargaining", "Our replies.")
t = reply(t, 50, "Enterprise")
game(t, end(51), {"text": "Social Interaction.", "at": round(P[51]["ts"] + SHOWS, 2)}, R(51, anchored=False),
     W("Bargaining wants a rare gem, in a boot shop. The line it was given only said: I am after something "
       "particular. Bargaining made it a haggler's line, and got carried away."))
game(end(51), end(52), "We choose Bargaining's anyway, to see what happens.", R(52),
     W("The cobbler brings us straight back to footwear. He can: his side of the tree knows we came for shoes."))
t = reply(end(52), 53, "Social Interaction")
t = reply(t, 54, "Diligence")
t = reply(t, 55, "Enterprise")
game(t, 6583.8,                                                          # TAKE
     "We choose Social Interaction's, and roll. Ten dice: two for regard, two for the replies, two for our face, "
     "and four for our clothes, because the townsman's cap, cloak, tunic and breeches all please a craftsman. "
     "Proposing a trade is a formality: after two replies, one six. One comes up.",
     "My advice: dress for the person you are talking to. Those are the cheapest dice in the game.")
game(6583.8, end(56), R(56))
game(end(56), end(57),
     "Then one more passage, from a mind that took no part. Bargaining is also a feeling, moved by a good "
     "trade. It tells how the trade felt, and pushes a humor of joy into the spleen: laetitia, which turns a five "
     "into a six. A feeling now is luck for later.", R(57))
game(end(57), 6719.9)

# ── 13. the trade ─────────────────────────────────────────────────────────────
game(6719.9, 6735.0,
     "The cobbler's shelf. Every price is in one coin, copper, silver or gold, and coins never convert. The "
     "leather boots cost twenty copper: all we have. One click to add them, one to confirm.", fade=True)
game(6735.0, end(58), "Back in the shop, a new look at the cobbler.", R(58),
     W("A bleeding knee, this time: the model invented a detail. Read the game's text as a story told by a "
       "slightly unreliable narrator. The rules underneath it are exact."))

# ── 14. the boots ─────────────────────────────────────────────────────────────
game(end(58), 6870.4,
     "In the inventory, the boots are already on our feet: a garment goes straight to its place when that place "
     "is free. Garments can protect, carry things, and please people of a given standing, which adds dice in "
     "conversation with them.", fade=True)

# ── 15. routines ──────────────────────────────────────────────────────────────
game(6870.4, 6895.1,
     "The purse is empty again, and that is fine. Every place you visit is kept as a mental map, and each map "
     "holds routines: ways back in. Asking for work taught one at the farm.",
     "The anamnesis decides how many maps you keep, four per point, and the oldest is forgotten first. You can "
     "lock the ones you care about.", fade=True)
game(6895.1, 6939.5,
     "From the world map, a place's routines are one click away. The work routine takes us straight back to "
     "work at the farm, with no conversation at all. Earning more coin is now a single trip.")

# ── 16. closure ───────────────────────────────────────────────────────────────
game(6939.5, 6954.5,
     "That was the first hour: a body built, a past remembered, two wolves, a job, a trade, and a way back. "
     "Every word the game wrote, it wrote on this computer, while we played. Proscribed Palimpsest is at "
     "edmond zero zero dot itch dot io, slash proscribed.", fade=True)

storyboard = {
    "title": "Proscribed Palimpsest: the first hour",
    "output": "build/tutorial.mp4",
    "size": [1920, 1080], "fps": 30,
    "dither": {"animation": {"levels": 4, "scale": 2}, "game": {"levels": 6, "scale": 1}},
    "fast_forward": {"speed": 8, "min": 1.5, "label": False},
    "voice": {"name": "en_GB-alan-medium", "speed": 1.0, "grit": 0.15},
    "voice_styles": {"reading": {"speed": 0.88, "grit": 0, "noise_scale": 0.9, "noise_w": 1.1,
                                 "pitch": 0.97, "room": 0.16}},
    "music": {"take_audio": "tutorial", "level_db": -34, "duck": 0.65},
    "clicks": {"gain": 0.5},
    "recordings": {"creation": {
        "script": "creation.cli", "dir": "build/rec_creation", "timeout": 1800,
        "flags": "--childhood-path \"sobs,waves,sharp,golden arch,shelter\" --force-encounter wolf,wolf "
                 "--no-encounters --npc-static --start-area \"Longhouse Hall,Cobbler's Shop\" --observe-only "
                 "farmer,cobbler --npc-affinity DistantAcquaintance --grant-mm diligence,provisioning,swordsmanship:2 "
                 "--job-only farmhand --humors all=voluptas"},
                   "tutorial": {
        "script": "tutorial.cli", "dir": "build/rec_tutorial_b", "timeout": 14400,
        "flags": "--childhood-path \"sobs,waves,sharp,golden arch,shelter\" --force-encounter wolf,wolf "
                 "--no-encounters --npc-static --start-area \"Longhouse Hall,Cobbler's Shop\" --observe-only "
                 "farmer,cobbler --npc-affinity DistantAcquaintance --grant-mm diligence,provisioning,swordsmanship:2 "
                 "--job-only farmhand --humors all=voluptas"}},
    "manim_file": "scenes.py",
    "segments": segments,
}
(HERE / "storyboard.json").write_text(json.dumps(storyboard, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")

# The narration as a script, for reading: explanation plain, the game's text in italics.
lines = ["# The tutorial video: narration", "",
         "Generated by build_storyboard.py from the narration and take B. Explanation is plain; *the game's "
         "own text, read in the second voice, is in italics*; [diagram] marks a manim scene.", ""]
for s in segments:
    vo = s.get("vo") or []
    if s["type"] == "manim":
        lines.append(f"**[{s['scene']}]**")
    for p in vo:
        text = p if isinstance(p, str) else p["text"]
        lines.append(f"> *{text}*" if isinstance(p, dict) and p.get("style") == "reading" else text)
    if vo:
        lines.append("")
(HERE / "SCRIPT.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"{len(segments)} segments, {sum(1 for s in segments if s['type'] == 'manim')} diagrams")
