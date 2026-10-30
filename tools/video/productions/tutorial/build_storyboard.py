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
     if "CHOOSER" not in p["voice"] and "MATCHER" not in p["voice"]]   # take B: its indices are used below
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


def is_dialogue(p):
    """A line said in a conversation (the speaker's or a reply), not a narration passage."""
    return "I say to" in p.get("prompt", "") or p["voice"].startswith("You are ")


def is_passage(p):
    """What the model wrote for the screen: not a judge's letter, and not a background job's notes."""
    return ("CHOOSER" not in p["voice"] and "MATCHER" not in p["voice"] and "JUDGE" not in p["voice"]
            and "CHRONICLE" not in p["voice"].upper())


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

# ── the opening take: menu, world, body, childhood, getting up ────────────────
# rec_opening plays opening.cli: tutorial.cli from the main menu to the start of the run. It was re-shot
# apart from take B (the CLI's element locator fixed; the merged prompts) and every moment of it is looked
# up from what happened (its timeline, its passages, the words the CLI clicked), not typed in, so a
# retake only needs the few remarks about what the model wrote (OPENING_NOTES) read again.


class Take:
    """One recording and its passages: when things happened, and when each passage could be read."""

    def __init__(self, name, folder, passages):
        self.name = name
        self.dir = HERE / folder
        self.events = [json.loads(l) for l in (self.dir / "timeline.jsonl").read_text().splitlines() if l.strip()]
        self.clicks = [e for e in self.events if e["type"] == "click"]
        self.P = [p for p in json.loads((HERE / passages).read_text()) if is_passage(p)]
        words = [l.split("keyword word:", 1)[1].strip()
                 for l in (self.dir / "game.log").read_text(errors="replace").splitlines()
                 if l.startswith("[cli]   keyword word:")]
        kw = [c for c in self.clicks if c.get("cmd", "").startswith("click keyword")]
        self.words = {c["t"]: w for c, w in zip(kw, words)}
        self.log = (self.dir / "game.log").read_text(errors="replace").splitlines()

    def at(self, kind, label, nth=0):
        return [e["t"] for e in self.events if e["type"] == kind and (e.get("label") or e.get("name")) == label][nth]

    def clip(self, name):
        return self.at("clip-begin", name), self.at("clip-end", name)

    def click(self, t0, t1, label=None, cmd=None, last=False):
        hits = [c["t"] for c in self.clicks if t0 <= c["t"] <= t1
                and (label is None or c.get("label") == label)
                and (cmd is None or c.get("cmd", "").startswith(cmd))]
        return (hits[-1] if last else hits[0]) if hits else None

    def word(self, t):
        """The word the keyword click at t landed on: the timeline's label (the word itself, in takes
        recorded since the recorder labelled it so), else the logged word, else the notes' table."""
        label = next(c.get("label") for c in self.clicks if c["t"] == t)
        if label and not label.startswith("@"):
            return label
        return self.words.get(t) or OPENING_NOTES.get("words", {}).get(label, label.lstrip("@"))

    def between(self, t0, t1):
        return [i for i, p in enumerate(self.P) if t0 < p["t1"] <= t1]

    def end(self, i):
        """When passage i sits whole on screen (see `end` above)."""
        t1 = self.P[i]["t1"]
        nxt = next((c["t"] for c in self.clicks if t1 - 0.2 <= c["t"] <= t1 + 4.0), None)
        if nxt is not None:
            return nxt + 0.8
        following = [p["t0"] for p in self.P if p["t0"] >= t1 - 0.05 and p is not self.P[i]]
        return min([t1 + 2.5] + following)

    def shows(self, i):
        """When passage i starts appearing on screen. Its first word comes SHOWS after the model's; but
        a part chained straight after another (the acting mind's line after the thinking mind's) is
        written while the box still shows the one before, and appears only when that one is pressed on."""
        t = self.P[i]["ts"] + SHOWS
        if i and not is_dialogue(self.P[i]) and self.P[i]["t0"] - self.P[i - 1]["t1"] < 1.0:
            press = self.click(self.P[i - 1]["t1"] - 0.2, 1e9, cmd="click continue") or \
                    self.click(self.P[i - 1]["t1"] - 0.2, 1e9)
            if press is not None:
                t = max(t, press + 0.4)
        return round(t, 2)

    def R(self, i):
        return {"text": self.P[i]["text"], "style": "reading", "at": self.shows(i)}

    def game(self, t0, t1, *vo, **kw):
        game(t0, t1, *vo, rec=self.name, **kw)


O = Take("opening", "build/rec_opening", "passages_opening.json")

# what the model happened to write, said where it is read; the rest of the opening is generic
OPENING_NOTES = json.loads((HERE / "opening_notes.json").read_text()) if (HERE / "opening_notes.json").exists() else {}


def note(key):
    return [W(OPENING_NOTES[key])] if key in OPENING_NOTES else []


# 1. the hook, the world
O.game(O.at("clip-begin", "new_run"), O.at("click", "New") - 0.2,
       "Your character in this game is not a sheet of numbers. It is a body, built organ by organ, and a crowd of "
       "minds living in it: some notice, some think, some act, and some only feel. Each one speaks in its own "
       "voice, and nothing they say is written in advance.",
       "This is Proscribed Palimpsest. In the next hour of play, from the main menu to a pair of new boots, we'll "
       "see how all of it works. And along the way, I'll tell you what I would do as a player.", fade=True)
O.game(O.at("click", "New") - 0.2, O.at("move", "Fenumoth") - 0.2,
       "A run starts by choosing a world. Each moon in this sky is one: three hundred and eighty-three of them, "
       "each with a name. The sky is fixed, the same on every machine, so a moon always stands for the same world. "
       "Two players who pick the same moon walk the same land and meet the same people, so they can compare notes.")
O.game(O.at("move", "Fenumoth") - 0.2, O.at("click", "world-confirm") - 0.4,
       "Choosing a moon generates its world, and its history comes first. Realms rise and fall; castles, temples "
       "and cities are founded; then farmland, villages and pastures spread out from them. The card says what kind "
       "of world it is. We take Fenumoth, a wooded world. For a first run, any moon will do: every one of them is "
       "built to be livable.")
O.game(O.at("click", "world-confirm") - 0.4, O.at("clip-begin", "creation_overview"))

# 2. the body: the system first, then the organs one by one
O.game(*O.clip("creation_overview"),
       "Now the protagonist. There are no strength or charisma scores in this game. A character is a body: five "
       "regions, which are the head, the face, the trunk, the arms and the legs; twenty-two organs; twenty-eight "
       "organ parts. Every part starts at one point, and you get twenty-five more to spend. That buys less than "
       "half of what the body could reach, so creating a character is really choosing what to give up.", fade=True)
SYSTEM_STEPS = [
    ("Before we spend them, here is how a character fits together, in four ideas. First, the body: five regions, "
     "the head, the face, the trunk, the arms and the legs, each scoring the sum of its organs. Here they are for "
     "the character we are about to build. Everything else in the game is read from these numbers."),
    ("Second, the measures. Each one is computed from a single organ or region, by a rule of its own. The "
     "backbone, doubled, gives the movement points you spend in a fight: four makes eight. The head, halved and "
     "plus one, gives your noetic points, the thoughts you can have in one scene: ten makes six."),
    ("Third, the ways of thinking you learn as you play, called modi mentis. Each draws on two organs, or on one "
     "region, and those set its ceiling, how far it can ever grow: one, plus up to three for each organ, or up to "
     "six for a region, the stronger they are. Swordsmanship draws on the arms and the hands. The arms score two "
     "out of four: plus two. The hands, four out of six: plus two. So its ceiling is five."),
    ("Fourth, the dice. Almost everything you attempt, a blow, a question, a climb, is settled by a roll. You roll "
     "one die for each level of the ways of thinking involved, and only sixes count: the task says how many it "
     "needs. Swordsmanship is level two today, so it brings two dice. Raise it, and it brings more."),
    ("Around these: what you carry can lend dice to an act, what you wear protects you in a fight and charms "
     "people in a conversation, and four organs make the humors, which can turn a die after it is rolled. We will "
     "meet each of them in this run."),
]
for k, vo in enumerate(SYSTEM_STEPS, start=1):
    manim("System", vo, params={"step": k})
MEMORY_STEPS = [
    ("Memory deserves a closer look, because this game makes you forget. Everything you learn arrives in working "
     "memory, at the front. Working memory is a queue: when it is full and something new arrives, the oldest way "
     "of thinking falls out of the back, and it is lost for good."),
    ("So you file what you want to keep. Consolidating moves a way of thinking into the long-term memory of its "
     "kind, practised skills, knowledge, or the senses, and there it is safe. Or you reject one on the spot, to "
     "make room before the queue decides for you."),
    ("Long-term memories fill up too. To make room, you archive a way of thinking into the residual memory, a last "
     "queue the anamnesis keeps, and each memory always keeps at least one. When the residual queue is full, its "
     "oldest is forgotten for good. My advice: don't neglect memory. A character who learns a lot but has nowhere "
     "to file it keeps forgetting."),
]
ORGAN_WALK = [  # (the organ hovered first, what is said while it is shown)
    ("anamnesis", ["First, thinking and memory: the head. As a whole, the head sets your noetic points, how many "
                   "times you can look and think in one scene, and it sizes working memory, where everything you "
                   "learn arrives. The anamnesis sizes the residual memory, and how many places you remember: four "
                   "per point. We raise it by one."]),
    ("cerebrum", ["The cerebrum sizes the memory for knowledge, like bargaining or archaeology. Two points."]),
    ("cerebellum", ["The cerebellum sizes the memory for practised skills, like swordsmanship or knotwork, and it "
                    "lets you learn a new move in the middle of a fight. Two points."]),
    ("hippocampus", ["The hippocampus sizes the memory for the ways of thinking that use the senses."]),
    ("pineal_gland", ["And the pineal gland sets how much experience a level costs, from twelve points down to six. "
                      "With two points or more, it also lets a mind doubt its own plan and think again."]),
    ("left_eye", ["The senses. The eyes set the reach of a ranged blow, in cells."]),
    ("left_ear", ["The ears add their score to your initiative, which decides who acts first in a fight. We raise both."]),
    ("nose", ["The nose is underrated. Each point lowers the chance of meeting danger on the road by ten per cent, and it "
     "tells you which humors are in a food before you eat it."]),
    ("tongue", ["Speech. The tongue sets how many replies you are offered in a conversation, up to five. At nought, you "
     "cannot hold a conversation at all. Two points here: conversation is how you get work, trade and friends.",
     "And the face as a whole gives countenance: in every conversation, one die for each five points. This face "
     "is worth two."]),
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
    t0 = O.at("move", "organ:" + organ)
    t1 = (O.at("move", "organ:" + ORGAN_WALK[k + 1][0]) if k + 1 < len(ORGAN_WALK)
          else O.at("clip-end", "organs_fighting"))
    O.game(t0, t1, *vo, fade=(k == 0))
    if organ == "pineal_gland":
        for j, mvo in enumerate(MEMORY_STEPS, start=1):
            manim("Memory", mvo, params={"step": j})

# 3. childhood: five memories, the first one slowly
MEMORY = {
    1: ("The body has no past yet. The game gives it one through five childhood memories, and they are played "
        "exactly the way the whole game is played. So the first memory is our first lesson. Let's take it slowly.",
        "Look under the passage, at what this memory gave: three ways of thinking, Empathy, Nautical Jargon and "
        "Knotwork, and a townsman's clothes: cap, cloak, tunic and breeches. The text said a port city. The code "
        "decided what that means for your character. Every passage works like this: the words are the model's; "
        "what they give you is the game's."),
    2: ("The second memory. Look at the heading: the observer is now Empathy, one of the minds we just gained. A "
        "new mind notices different things.",
        "This one gives two ways of thinking: Hard Labor and Tide Watching. The docks taught us to work, and to "
        "read the water."),
    3: ("The third memory: something sharp.", "A short sword, and Bargaining."),
    4: ("The fourth: a dream of a golden arch.",
        "A traveller's backpack, with bread and cheese inside, and Archaeology. Keep the bread in mind."),
    5: ("The last memory: shelter.",
        "This one gives no item, only two ways of thinking: Exploration and Endurance. My advice for your own "
        "childhood: read the fragments, and pick what you want your character to be. The memories you choose are "
        "your starting kit."),
}
cut = O.click(O.at("clip-end", "organs_fighting"), 1e9, label="Continue") + 0.3
prev_end = O.at("clip-end", "organs_fighting")
for m in range(1, 6):
    cb, ce = O.clip(f"childhood_{m}")
    ps = O.between(prev_end, ce)
    assert len(ps) == 4, f"memory {m}: expected 4 passages, found {[O.P[i]['text'][:40] for i in ps]}"
    obs, reason, intent, outcome = ps
    kw = O.click(cb, ce, cmd="click keyword")
    footer = O.click(cb, ce, label="footer button", last=True)
    intro, gave = MEMORY[m]
    O.game(cut, O.end(obs), intro, O.R(obs), *note(f"m{m}_obs"), fade=True)
    if m == 1:
        O.game(O.end(obs), kw - 0.3,
               "That text was not written in advance. It was written a moment ago, by a language model running on "
               "this computer. It runs entirely on your machine: no account, no server, nothing sent anywhere, and "
               "it works on the processor alone.",
               "But the model does not run the game. The rules, the dice, wounds, coin and time are ordinary code. "
               "The model is asked narrow questions inside that code: what does this mind notice, what does it "
               "want, how does it tell what happened.",
               "Each question is put to a modus mentis, a way of thinking, and the model plays that way of thinking "
               "as a character, in its own voice. That is why every passage is headed by a name. Read these texts "
               "as the character's own thoughts. When one sounds a little strange, take it as this character's "
               "strange way of seeing things.")
        manim("NarrationTurn",
              "Here is a turn: the loop you repeat all game. A mind observes, and writes what it notices. Some words "
              "in what it wrote light up: keywords. You click one. A thinking mind reasons about it and settles on a "
              "goal, chosen from the actions the code says that thing allows. Then an acting mind says how willing "
              "it is, and its willingness makes the roll easier or harder. The dice decide, and the acting mind "
              "tells how it went.",
              "Thinking costs a noetic point, and so does every extra look. The points come back when the scene "
              "moves on, but not after a failed roll: a miss costs something.")
        first = (f"We click the word {O.word(kw)}, choose Think, and pick the mind that will think. For now, "
                 "there is only one.")
        O.game(kw - 0.3, O.end(reason), first, O.R(reason), *note(f"m{m}_reason"))
        O.game(O.end(reason), O.end(intent), "Now the acting mind puts the goal into words.", O.R(intent),
               *note(f"m{m}_intent"))
        execute = O.click(cb, ce, label="Execute")
        O.game(O.end(intent), execute + 0.3, "The act is offered as a line. We click it, then Execute.")
        O.game(execute + 0.3, O.end(outcome), "In childhood nothing is rolled: remembering always succeeds.",
               O.R(outcome), W(gave), *note(f"m{m}_outcome"))
    else:
        O.game(O.end(obs), O.end(reason), f"We click the word {O.word(kw)}.", O.R(reason), *note(f"m{m}_reason"))
        O.game(O.end(reason), O.end(intent), O.R(intent), *note(f"m{m}_intent"))
        O.game(O.end(intent), O.end(outcome), O.R(outcome), W(gave), *note(f"m{m}_outcome"))
    O.game(O.end(outcome), footer - 0.3)
    cut, prev_end = footer - 0.3, ce

# 4. getting up: the first real roll
gb, ge = O.clip("get_up")
ps = O.between(prev_end, ge)
assert len(ps) == 5, f"getting up: expected 5 passages, found {[O.P[i]['text'][:40] for i in ps]}"
bearings, look, reason, intent, outcome = ps
kw = O.click(gb, ge, cmd="click keyword")
mind = next(c.get("label") for c in O.clicks if c["t"] > kw and c.get("cmd", "").startswith("choose")
            and c.get("label") not in ("Think",))
O.game(cut, O.end(bearings),
       "Childhood is over. The body wakes on open ground, spent, and a mind takes our bearings.", O.R(bearings),
       fade=True)
O.game(O.end(bearings), O.end(look), O.R(look), W("Getting up is the first real roll of the game."))
O.game(O.end(look), O.end(reason),
       f"We think about the {O.word(kw)}, this time with {mind}, one of the minds from the docks. Listen to how "
       "differently it reasons.", O.R(reason), *note("up_reason"))
O.game(O.end(reason), O.end(intent), O.R(intent), *note("up_intent"))
roll = next(l for l in O.log if l.startswith("[cli] dice ["))
faces = roll.split("[", 2)[2].split("]")[0].split()
sixes = faces.count("6")
spent = len([c for c in O.clicks if gb <= c["t"] <= ge and c.get("cmd", "").startswith("click humor")])
dice_end = O.click(O.end(intent), ge, label="Continue") - 0.3
NUM = "nought one two three four five six seven eight nine ten eleven twelve".split()
roll_line = (f"We click the act, and Execute. The dice: {NUM[len(faces)]} of them"
             + (f", and {NUM[sixes]} six{'es' if sixes != 1 else ''}." if sixes else ", and not a single six.")
             + (f" Not enough: we spend {NUM[spent]} humor{'s' if spent != 1 else ''}, and the roll succeeds."
                if spent else ""))
O.game(O.end(intent), dice_end, roll_line)
manim("DiceHumors",
      "Every roll works the same way. You roll one die for each level of every mind in the chain: the one that "
      "observed, the one that thought, and the one that acts. The difficulty is the number of sixes you need. So "
      "raising any of the three minds means more dice, and better odds.",
      "When a roll falls short, the humors can save it. Only the humor at the back of each queue can be spent, "
      "one per die, and only as many in one roll as the viscera allow. Blood adds one to a die. Yellow bile takes "
      "one away. Phlegm does nothing, but spending it clears the way to the humor behind it. Black bile does "
      "nothing, and never leaves. A queue full of black bile is spent, and when all four are, the body dies. So "
      "humors are your luck, and also your life.")
O.game(dice_end, O.end(outcome), O.R(outcome),
       W("Success. Look at the lines below: every mind in the chain gained a point of experience, and the act "
         "taught a new one, Vigor. Doing things is how you learn."), *note("up_outcome"))
leave = O.click(O.end(outcome), ge + 1, label="footer button")
O.game(O.end(outcome), leave - 0.2)
O.game(leave - 0.2, O.at("clip-end", "run_start"),
       "The run has truly begun. This is the world map, where every journey starts. Before travelling, let's look "
       "at what we have.")

# ── 6. the protagonist screen ─────────────────────────────────────────────────
game(3223.0, 3226.3, "Escape opens the menu, and Protagonist opens the character screen.", fade=True)
game(3226.3, 3236.0, "The anatomy tab: the body we built. Wounds will show here, on the organ they hit.")
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
segments[-1]["vo"].append(W("A sailor's mind asking for farm work: it offers itself like a crew to a captain, "
                            "bind me to your helm."))
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
     "into a six. A feeling now is luck for later.", R(57),
     W("A hawk skimming the breeze after a hunt: even a merchant's mind reaches for its own images when it is "
       "glad."))
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
    "fit": "slow",
    "idle": {"after": 2.0, "before": 0.8, "speed": 4.0, "min": 1.5},
    "voice": {"name": "en_GB-alan-medium", "speed": 1.0, "grit": 0.15},
    "voice_styles": {"reading": {"speed": 0.88, "grit": 0, "noise_scale": 0.9, "noise_w": 1.1,
                                 "pitch": 0.97, "room": 0.16}},
    "music": {"take_audio": "tutorial", "level_db": -34, "duck": 0.65},
    "clicks": {"gain": 0.5},
    "recordings": {"opening": {
        "script": "opening.cli", "dir": "build/rec_opening", "timeout": 7200,
        "flags": "--childhood-path \"sobs,waves,sharp,golden arch,shelter\" --force-encounter wolf,wolf "
                 "--no-encounters --npc-static --start-area \"Longhouse Hall,Cobbler's Shop\" --observe-only "
                 "farmer,cobbler --npc-affinity DistantAcquaintance --grant-mm diligence,provisioning,swordsmanship:2 "
                 "--job-only farmhand --humors all=voluptas --save-path build/rec_opening/save.json"},
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
