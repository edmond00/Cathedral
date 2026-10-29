"""Writes storyboard.json for the tutorial from the narration (SCRIPT.md, here as code) and the take.

    python build_storyboard.py            # -> storyboard.json

The take is build/rec_tutorial_b. Its passages (passages_take_b.json, made by passages.py with
names_take_b.json) are read aloud in the "reading" voice, each in a segment that ends just after the
passage has finished appearing, so the reading starts as the text streams in and the last frame holds
the whole passage while it is read. Every time below is a moment of that take's timeline (clip
boundaries, the clicks between steps), listed by passages.py and the take's timeline.jsonl.

Choosing another take means new times, new passages and new numbers in the explanations (dice,
humors spent): the take-specific lines are marked TAKE below.
"""
import json
from pathlib import Path

HERE = Path(__file__).parent
P = [p for p in json.loads((HERE / "passages_take_b.json").read_text())
     if "CHOOSER" not in p["voice"] and "MATCHER" not in p["voice"]]

segments = []


def R(i):
    """Passage i, read in the reading voice."""
    return {"text": P[i]["text"], "style": "reading"}


def end(i, pad=1.0):
    return P[i]["t1"] + pad


def game(t0, t1, *vo, **kw):
    seg = {"type": "game", "recording": "tutorial", "from": round(t0, 2), "to": round(t1, 2)}
    if vo:
        seg["vo"] = list(vo)
    seg.update(kw)
    segments.append(seg)


def clip(name, *vo, **kw):
    seg = {"type": "game", "recording": "tutorial", "clip": name}
    if vo:
        seg["vo"] = list(vo)
    seg.update(kw)
    segments.append(seg)


def manim(scene, *vo, **kw):
    seg = {"type": "manim", "scene": scene, "vo": list(vo)}
    seg.update(kw)
    segments.append(seg)


def memory(t_start, t_end, obs, reason, intent, outcome, after):
    """One childhood memory: the fragment, the reasoning, the intent, the outcome, one line on what it gave."""
    game(t_start, end(obs), R(obs))
    game(end(obs), end(reason), R(reason))
    game(end(reason), end(intent), R(intent))
    game(end(intent), t_end, R(outcome), after)


def replies(t_from, *pairs):
    """Each reply offered, introduced by the mind that wrote it. Returns where the last one ends."""
    t = t_from
    for mind, i in pairs:
        game(t, end(i), mind + ":", R(i))
        t = end(i)
    return t


# ── 1. a new run ──────────────────────────────────────────────────────────────
clip("new_run",
     "This is Proscribed Palimpsest, a narrative role-playing game. This video plays the first hour of a run, "
     "from the main menu to a pair of new boots, and explains each system as it comes up.")

# ── 2. a world ────────────────────────────────────────────────────────────────
clip("world_selection",
     "A run begins in the sky. Each of these moons is a world: three hundred and eighty-three of them, each with "
     "its own name. The sky is the same on every machine, so a moon always names the same world, and two players "
     "who pick the same moon walk the same land, with the same towns and the same people.",
     "The world is generated when you choose it, history included: realms, castles and temples are laid down "
     "first, and the farmland, villages and cities spread out from them. We take Fenumoth, a wooded world.")

# ── 3. the body ───────────────────────────────────────────────────────────────
clip("creation_overview",
     "Characters have no strength or charisma scores. A character is a body: five regions, twenty-two organs, "
     "twenty-eight organ parts. Every part starts at one, and you have twenty-five points to raise them. That is "
     "less than half of what the body could reach, so creating a character means choosing what to give up.")
manim("Organs",
      "Every measure the game uses comes from one organ or one region. Here they are, by the system each one "
      "serves. Hovering an organ shows the same thing on the right of the screen. We'll go through them in this "
      "order, raising the ones we want.")
clip("organs_thinking",
     "Thinking and memory. The encephalon, the whole head, sets your noetic points: how many times you can look "
     "and think in one scene. Its score halved, plus one.",
     "What you learn are called modi mentis: ways of thinking. New ones arrive in working memory, which is sized by "
     "the whole head and works as a queue: when it is full, the oldest is lost for good. To keep one, you file it "
     "into the long-term memory of its kind: the cerebellum holds practised skills, the cerebrum knowledge, the "
     "hippocampus the senses. The anamnesis holds what you set aside, and how many places you remember: four per "
     "point.",
     "The pineal gland sets how much experience a level costs, from twelve points down to six. From two points "
     "on, it also lets a mind doubt its own plan before it acts.")
clip("organs_senses",
     "The senses. The eyes set the reach of a ranged blow. The ears add to your initiative in a fight. The nose "
     "lowers the chance of meeting danger on the road, ten per cent per point, and tells you what is in a food "
     "before you eat it.")
clip("organs_dialogue",
     "Speech. The tongue sets how many replies you are offered in a conversation, up to five; at nought, you "
     "cannot hold a conversation at all. The teeth only decide whether you can eat hard food.")
clip("organs_humors",
     "The humors. Four organs make them: the paunch, the hepar, the spleen and the pulmones. Each keeps a queue of "
     "forty-nine. Humors pay for effort, like each step of a journey, and they can change a die after it is "
     "rolled. We'll see that at the first roll.",
     "The viscera set how many humors you can spend on a single roll, and how fast wounds close. We raise them to "
     "five.")
clip("organs_life",
     "Life. The heart sets your lifespan, how many companions you can lead, and a share of youthful humors at the "
     "start, which are warm and lucky. The genitories add dice to every blow you strike.")
clip("organs_fighting",
     "The body in a fight. The backbone sets what you can carry, and your movement points in a fight: double its "
     "score. The hands decide which tools you can use, and how well. The legs set how far one movement point "
     "takes you; the lower body as a whole sets how far a journey can reach, and your natural defence. The feet "
     "keep you standing on bad ground, and get you out of a fight.",
     "And the trunk, as a region, sets your health: a third of its score, plus one. A wound takes one point of "
     "health, and it does not come back until the wound heals.")

# ── 4. childhood ──────────────────────────────────────────────────────────────
# The first memory, slowly: the model is introduced here, at the first passage it writes.
game(304.3, 312.0,
     "The body has no past yet. The game gives it one through five childhood memories, played the way the whole "
     "game is played. The first is the clearest example of a turn, so we'll take it slowly.",
     R(0))
game(312.0, 316.0,
     "That passage was not written in advance. It was written just now, by a language model running on this "
     "computer. It runs entirely on your machine: no account, no server, nothing sent anywhere, and it works on "
     "the processor alone. It does not run the game. Rules, dice, wounds, coin and time are ordinary code. The "
     "model is asked narrow questions inside that code: what this mind notices, what it wants, how it tells what "
     "happened.",
     "Every passage is written by a modus mentis, a way of thinking, which the model plays as a character, in its "
     "own voice. The lit word is a keyword. We click sobs, choose Think, and choose which mind will think about "
     "it: for now, the only one we have.")
manim("NarrationTurn",
      "This is a turn. A mind observes. You click a word in what it wrote. A thinking mind reasons about it and "
      "settles on a goal, chosen from what the code says that thing allows. An acting mind says how willingly it "
      "will do it, and that changes the difficulty. Then the dice, and the outcome, told by the acting mind.",
      "Each thought costs a noetic point, and so does each look beyond the first. The points come back when the "
      "scene moves on, but not after a failed roll.")
game(316.0, end(1),
     "The thinking mind now reasons, step by step, in its own voice.", R(1))
game(end(1), end(2), "Then the acting mind says what it will do.", R(2))
game(end(2), end(3), "In childhood nothing is rolled: remembering always succeeds.", R(3))
game(end(3), 658.0, "Each memory gives something real. This one gives the city we grew up in: a port.")

memory(658.0, 1243.7, 4, 5, 6, 7,
       "The second memory gives work on the docks, and what that work taught.")
memory(1243.7, 1658.0, 8, 9, 10, 11,
       "Something sharp: a short sword, bought with a first wage. It will be in our hand.")
memory(1658.0, 2187.3, 12, 13, 14, 15,
       "A dream of a golden arch: a traveller's backpack, with bread and cheese inside.")
memory(2187.3, 2673.6, 16, 17, 18, 19,
       "Shelter gives nothing to carry. Not every memory does.")

# ── 5. getting up ─────────────────────────────────────────────────────────────
game(2700.0, 2909.0,
     "Childhood ends, and the body wakes on open ground, spent. Getting up is the first real roll.",
     R(20), R(21))
game(2909.0, 2918.0,
     "We think about the legs, with Nautical Jargon: a way of thinking the docks taught.")
game(2918.0, end(22), R(22))
game(end(22), end(23), "Knotwork, another lesson of the docks, will act.", R(23))
game(end(23), 3121.0,                                                    # TAKE: four dice, two sixes
     "The roll. Four dice come up, and two of them are sixes.")
manim("DiceHumors",
      "Here is how a roll works. You roll one die for each level of every mind in the chain: the one that "
      "observed, the one that thought, and the one that acts. The difficulty is how many sixes you need.",
      "If the roll falls short, the humors can change it. Only the humor at the back of each queue can be spent, "
      "one per die, and as many in one roll as the viscera allow. Blood adds one to a die. Yellow bile takes one "
      "away. Phlegm does nothing, but spending it clears the way to the humor behind it. Black bile does nothing "
      "and never leaves: a queue of nothing but black bile is spent, and when all four are, the body dies.")
game(3121.0, 3210.7, "Here no humor was needed.", R(24))

# ── 6. the protagonist screen ─────────────────────────────────────────────────
clip("run_start", "The run has begun.")
clip("anatomy", "Escape opens the protagonist screen. The anatomy, where wounds will show.")
clip("memory", "The memory: the ways of thinking learned in childhood, each filed in the memory of its kind.")
clip("inventory",
     "The inventory. The sword is held. The backpack is worn, and things are stored in it. We eat the bread.")
clip("humors",
     "Eating pushes the food's humors into the paunch, at the front of its queue. Hovering a humor shows its "
     "warmth, which pays for effort, and what it does to a die. It moves toward the back, where it can be spent, "
     "as the older ones are used up.")

# ── 7. the road ───────────────────────────────────────────────────────────────
clip("travel_farm",
     "The map. You can set a destination as far as your legs allow, and the route goes cell by cell. Each cell "
     "costs days, and warmth drawn from the humors as you walk, and each land has its dangers: a plain has wolves "
     "and bandits, a forest more of both. We head for a farm.")

# ── 8. the first fight ────────────────────────────────────────────────────────
game(3318.1, 3330.0, "A wolf on the road.")
game(3330.0, 3356.0,
     "A fight is played on a grid, in turns. Your movement points come from the backbone. The wolf is far off, "
     "so we end our turns and let it come.")
game(3356.0, 3373.6,                                                     # TAKE: five dice
     "Fighting skills come from the ways of thinking that unlock them, and from what you strike with. The sword "
     "gives Snap Thrust: five dice.")
manim("AttackDefence",
      "A blow is settled by sixes. The attack rolls the skill's dice, plus the dice of the genitories. The "
      "defence rolls dice from the lower body, from any guard, and from the armour on the part struck. More sixes "
      "than the defender, and the blow wounds. A tie goes to the defender.")
game(3373.6, 3442.0,                                                     # TAKE
     "The first three thrusts land, and the log names the wounds: a broken backbone, broken ribs. The wolf tries "
     "to learn a new attack in "
     "the middle of the fight, and fails. Beasts learn the same way you do.")
game(3442.0, 3474.0,                                                     # TAKE
     "The fourth thrust falls short. At the back of the hepar is voluptas, the humor of pleasure: it turns any "
     "die into a six. Spent, the thrust lands.")
game(3474.0, 3505.0, "The fifth needs two humors. The wolf falls.")      # TAKE
game(3505.0, end(25), "The journey goes on, to the farm.", R(25))
game(end(25), 3766.6, R(26))

# ── 9. asking for work ────────────────────────────────────────────────────────
game(3766.6, 3773.0,
     "We need coin for boots. The farmer, Zedayne Hartmead, gives work. We think about her with Diligence.")
game(3773.0, end(27),
     "Diligence is a will: a mind that wants one thing, here to ask for work. It looks for the shortest way to "
     "get there.", R(27))
game(end(27), end(28), R(28))
game(end(28), 4029.5,                                                    # TAKE
     "A will carries its own act out, eagerly, and that lowers the difficulty by one: one six needed. Two come "
     "up.")
game(4029.5, end(29), R(29))
game(end(29), end(30),
     "Asking succeeded, so a conversation opens. She speaks first.", R(30))
t = replies(end(30),
            ("Each reply on offer is written by a different speaking mind, in its own voice, and the tongue decides "
             "how many are offered. Diligence", 31),
            ("Nautical Jargon", 32), ("Provisioning", 33))
game(t, end(34), "We take Diligence's.", R(34))
t = replies(end(34), ("Social Interaction", 35), ("Diligence", 36), ("Bargaining", 37))
game(t, end(38), "We take Social Interaction's question.", R(38))
t = replies(end(38), ("Diligence", 39), ("Nautical Jargon", 40), ("Social Interaction", 41))
game(t, 5004.8,                                                          # TAKE
     "We take Diligence's. That ends the branch, and the whole conversation is settled by one roll. Nine dice: "
     "two for what she thinks of us, five for the levels of the minds behind our replies, and two for our face. "
     "Asking for work is a matter of consequence: after three replies, it needs four sixes. One comes up. Three "
     "humors turn three more dice.")
game(5004.8, end(42), R(42))
game(end(42), 5088.1)

# ── 10. the job ───────────────────────────────────────────────────────────────
clip("job_menu",
     "Work is not a quest. You trade days for coin and experience. A farmhand earns one copper every ten days, so "
     "two hundred days earn twenty copper: the price of boots. The job trains three ways of thinking, the first "
     "the most, and the calendar moves on by the days worked.")

# ── 11. to the city ───────────────────────────────────────────────────────────
clip("travel_city", "Then to the city.")
game(5139.2, 5320.0,                                                     # TAKE
     "Another wolf, already beside us. This time every thrust falls short of the wolf's defence, and each time a "
     "humor turns it: one humor for each of the first three, three for the fourth, one for the fifth. The wolf "
     "falls on the fifth.")
game(5320.0, end(43), "We reach the cobbler's shop.", R(43))
game(end(43), 5664.2, R(44))

# ── 12. buying boots ──────────────────────────────────────────────────────────
game(5664.2, 5679.0,
     "Provisioning is another will: it wants to buy. Its way here is a proposal to buy from the cobbler, "
     "Publiosh Tommaezoth.")
game(5679.0, end(45), R(45))
game(end(45), end(46),
     "This time the act goes to Diligence, which is reluctant: the difficulty rises by one, to two sixes.", R(46))
game(end(46), 5942.5,                                                    # TAKE
     "Five dice, and no six. Two humors make two sixes.")
game(5942.5, end(47), R(47))
game(end(47), end(48), "The cobbler answers.", R(48))
t = replies(end(48), ("Bargaining", 49), ("Enterprise", 50), ("Social Interaction", 51))
game(t, end(52), "We take Bargaining's.", R(52))
t = replies(end(52), ("Social Interaction", 53), ("Diligence", 54), ("Enterprise", 55))
game(t, 6584.2,                                                          # TAKE
     "We take Social Interaction's, and the roll: ten dice. Two for regard, two for the replies, two for our "
     "face, and four for our clothes: the cap, cloak, tunic and breeches of a townsman, which please a "
     "craftsman. Proposing a trade is a formality: after two replies, one six. One comes up.")
game(6584.2, end(56), R(56))
game(end(56), end(57),
     "Then one more passage, from a mind that took no part in the act. Bargaining is also a feeling, moved by a "
     "good trade. It tells how the trade felt, and pushes a humor of joy into the spleen.", R(57))
game(end(57), 6721.0)

# ── 13. the trade ─────────────────────────────────────────────────────────────
game(6721.0, 6735.0,
     "The cobbler's shelf. Every price is in one coin, copper, silver or gold, and coins never convert. The boots "
     "cost twenty copper.")
game(6735.0, 6851.9, R(58))

# ── 14. the boots ─────────────────────────────────────────────────────────────
clip("wear_boots",
     "A garment goes straight to its place when that place is free. Garments can protect, carry things, and "
     "please people of a given standing, which adds a die in conversation with them.")

# ── 15. routines ──────────────────────────────────────────────────────────────
clip("chorography",
     "The purse is empty again. Each place you visit is kept as a mental map, and each map holds routines: ways "
     "back in. Asking for work taught us one.")
clip("routine_box",
     "From the world map, the farm's routines are one click from the route. The next visit can go straight to "
     "work, with no conversation first.")

# ── 16. closure ───────────────────────────────────────────────────────────────
clip("closure",
     "That is the first hour: a body built, a past remembered, two fights, a job, a trade, and a way back. "
     "Proscribed Palimpsest, at edmond zero zero dot itch dot io, slash proscribed.")

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
    "recordings": {"tutorial": {
        "script": "tutorial.cli", "dir": "build/rec_tutorial_b", "timeout": 14400,
        "flags": "--childhood-path \"sobs,waves,sharp,golden arch,shelter\" --force-encounter wolf,wolf "
                 "--no-encounters --npc-static --start-area \"Longhouse Hall,Cobbler's Shop\" --observe-only "
                 "farmer,cobbler --npc-affinity DistantAcquaintance --grant-mm diligence,provisioning,swordsmanship:2 "
                 "--job-only farmhand --humors all=voluptas"}},
    "manim_file": "scenes.py",
    "segments": segments,
}
(HERE / "storyboard.json").write_text(json.dumps(storyboard, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
print(f"{len(segments)} segments, {sum(1 for s in segments if s['type'] == 'manim')} diagrams")
