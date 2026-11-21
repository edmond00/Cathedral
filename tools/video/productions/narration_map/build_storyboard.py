"""Writes storyboard.json for the changelog "narration on the map", and SCRIPT.md (the narration, readable).

    python build_storyboard.py

Five takes, all on the real model, all one character (the run opening_map.cli starts):
  opening  build/rec_opening   opening_map.cli: childhood and getting up, on the get-up map
  farm     build/rec_farm      live: the farm, from the opening's save
  city     build/rec_city      live: the cobbler's shop, a theft that fails, caught, the confrontation lost
  shop     build/rec_shop      live: the same shop again, the theft succeeds; speech; a rule of action; Reconsider
  flee     build/rec_flee      live: --npc-hostile, an enemy in the room, RUNAWAY
and two takes of the tutorial, for the "before" (the keyword screen):
  old_opening, old_farm         ../tutorial/build/rec_opening3, rec_rest3

Times are moments of those timelines (marks, clicks) and of their passages (passages_<take>.json,
made by passages.py). Every rule stated is the manual's (docs/manual on narration-map) or the design
note's (design/NARRATION_MAP.md); every number of the run is read from the takes.
"""
import json
import re
from pathlib import Path

from narration import MANIM, WHY

HERE = Path(__file__).parent
SHOWS = 1.5          # seconds between the model's first word and the first word on screen
MAP = {"factor": 1.33, "center": [720, 500]}      # the map with its header (noetic points) in frame
TREE = {"factor": 2.0, "center": [720, 540]}      # the get-up map: one card, in the middle

segments = []


def is_passage(p):
    return not any(k in p["voice"] for k in ("CHOOSER", "MATCHER", "JUDGE")) and "CHRONICLE" not in p["voice"].upper()


def words(s):
    return set(re.findall(r"[a-z']+", s.lower()))


class Take:
    def __init__(self, name, folder, passages=None):
        self.name = name
        self.dir = HERE / folder
        self.events = [json.loads(l) for l in (self.dir / "timeline.jsonl").read_text().splitlines() if l.strip()]
        self.P = json.loads((HERE / passages).read_text()) if passages else []
        # What the screen showed: the game's rewrite of the model's text (the register filter changes a
        # word here and there), read off `inspect texts`; the reading voice reads what is on screen.
        self.screen = [m.group(1) for l in (self.dir / "game.log").read_text(errors="replace").splitlines()
                       if (m := re.match(r'\[cli\] text \S+ mind=\S+ "(.*)"$', l))]

    def mark(self, label):
        return next(e["t"] for e in self.events if e["type"] == "mark" and e["label"] == label)

    def click(self, label, after=0.0):
        return next(e["t"] for e in self.events if e["type"] == "click" and e.get("label") == label and e["t"] >= after)

    def events_t(self, kind, after=0.0):
        return next(e["t"] for e in self.events if e["type"] == kind and e["t"] >= after)

    def text(self, i):
        raw = self.P[i]["text"].strip()
        best, score = raw, 0.0
        for s in self.screen:
            a, b = words(raw), words(s)
            j = len(a & b) / max(1, len(a | b))
            if j > score:
                best, score = s, j
        out = best if score > 0.6 else raw
        return out.replace("[SUCCESS] ", "").replace('""', '"').strip('"')

    def R(self, i, text=None, anchored=True):
        """Passage i, read in the reading voice from when it starts appearing on screen."""
        part = {"text": text or self.text(i), "style": "reading"}
        if anchored:
            part["at"] = round(self.P[i]["ts"] + SHOWS, 2)
        return part


O = Take("opening", "build/rec_opening", "passages_opening.json")
F = Take("farm", "build/rec_farm", "passages_farm.json")
C = Take("city", "build/rec_city", "passages_city.json")
S = Take("shop", "build/rec_shop", "passages_shop.json")
X = Take("flee", "build/rec_flee", "passages_flee.json")


def W(text):
    """Said once the picture has had its say (after a reading, or over a held frame)."""
    return {"text": text, "wait": True}


def A(text, t):
    """Said from recording moment t: what the picture shows next is introduced as it happens."""
    return {"text": text, "at": round(t, 2)}


def game(rec, t0, t1, *vo, fade=False, zoom=MAP, **kw):
    seg = {"type": "game", "recording": rec, "from": round(t0, 2), "to": round(t1, 2), "fade": fade}
    if zoom:
        seg["zoom"] = zoom
    if vo:
        seg["vo"] = list(vo)
    seg.update(kw)
    segments.append(seg)


def manim(scene, step=None, **kw):
    core = list(MANIM[(scene, step)])
    why = [WHY[(scene, step)]] if (scene, step) in WHY else []
    seg = {"type": "manim", "scene": scene, "vo": core + why, **kw}
    if why:
        seg["core"] = [0, len(core)]                  # the scene's beats follow these parts; the reason, after
    if step:
        seg["params"] = {"step": step}
    segments.append(seg)


def shot(rec, t0, t1, speed=1.0, zoom=None):
    s = {"recording": rec, "from": round(t0, 2), "to": round(t1, 2), "speed": speed}
    if zoom:
        s["zoom"] = zoom
    return s


# ── 0. the hook ───────────────────────────────────────────────────────────────

segments.append({"type": "montage", "fade": True, "min": 22.0, "shots": [
    shot("old_farm", 470.0, 474.0),
    shot("farm", 60.0, 64.0, zoom=MAP),
    shot("farm", 1110.0, 1114.0, zoom=MAP),
    shot("shop", 1415.5, 1419.5, zoom=MAP),
    shot("flee", 312.5, 316.5, zoom=MAP),
], "vo": [
    "Proscribed Palimpsest has a new narration screen. The page of text with clickable words is gone. "
    "Instead, you see the place itself: a map you read, click, and fill as you play.",
    "This video shows why it changed, and how it plays. The rules are now easier to see, so it also looks "
    "at what each rule is for: what the minds do, how a will finds its way, and what a crime costs.",
]})

# ── 1. before, and why ────────────────────────────────────────────────────────

game("old_farm", 457.39, 520.0, MANIM[("Principles", None)][0], zoom=None, fade=True, speed=2.0)
game("old_farm", 811.89, 828.81,
     "To act on the farmer, you clicked the word farmer. Then you chose what to do. Then you chose which "
     "mind would do it. Three menus, but only one was a real choice: which mind. The new screen keeps only "
     "that one.",
     zoom=None)
segments.append({"type": "manim", "scene": "Principles", "vo": [MANIM[("Principles", None)][1], WHY[("Principles", None)]],
                 "core": [0, 1]})
game("farm", F.mark("arrival") - 10.0, F.mark("observe_person"),
     "Here is the same farm on the new screen. Each card is an area. The lit card is where we stand. The "
     "lines are the ways between areas. At the top, the header shows our name and our six noetic points. "
     "And one symbol up there is the way out.", fade=True)

# ── 2. reading the map ────────────────────────────────────────────────────────

manim("Legend", "kinds")
manim("Legend", "states")
manim("Legend", "tree")

# ── 3. a visit, as a player ───────────────────────────────────────────────────

game("farm", F.mark("observe_person"), F.click("CONTINUE", F.mark("observe_person")) + 1.5,
     "Let's play. In the Courtyard there is one person we don't know yet. We click it. The popup asks only "
     "one thing: which mind will look. We pick Lullaby.",
     F.R(0),
     W("Lullaby says no. Minds have tastes, and a mind can say this kind of thing does not interest it. That "
       "still costs the point, and the refusal stays on the map as a crossed circle. But each thing can only "
       "be refused once. A mind's taste can shape a visit, but it should not block you."),
     fade=True)
game("farm", F.mark("observe_again"), F.click("CONTINUE", F.mark("observe_again")) + 2.0,
     "We click again. Lullaby is now greyed out in the popup. Whichever mind we ask now has to look. We "
     "pick Social Interaction.",
     F.R(1),
     W("It's a butterfly. The symbol moves under the card and becomes the root of a tree. The text is under "
       "it, with a question mark: something to think about."))
manim("Chain", "steps")
manim("Functions")
manim("Properties")
game("farm", F.mark("will_ratcatcher"), F.click("CONTINUE", F.mark("will_ratcatcher") + 200) + 2.0,
     "Now a will. Ratcatcher wants one thing: to catch vermin. We ask it to think about the butterfly.",
     F.R(4),
     F.R(5),
     W("To Ratcatcher, a butterfly is small game. That is the charm of a will: it sees the whole world "
       "through one want. Here, its course is a single act: catch it. That act now waits under the tree. "
       "Hunt was chosen to do it."))
manim("Chain", "dice")
game("farm", F.mark("observe_way"), F.click("CONTINUE", F.mark("observe_way")) + 1.5,
     "Hover over that act, and the map shows its dice and the sixes it needs. It needs four sixes, but the "
     "chain only gives three dice. It cannot succeed, so we leave it. Let's try a way instead. Each way has "
     "its own symbol, on its line. You choose exactly where to explore. No mind picks the way for you. We "
     "ask Heraldry to look at the way to the Chicken Coop.",
     F.R(6),
     W("Another refusal. Each way can be refused once."))
game("farm", F.mark("observe_way2"), F.click("CONTINUE", F.mark("observe_way2")) + 1.5,
     "So the next mind has to look. Childhood Memory.",
     F.R(7))
game("farm", F.mark("think_way"), F.mark("act_move"),
     "Ratcatcher again, about the way. Will it go in?",
     F.R(10),
     F.R(11),
     W("Yes. The coop is on its course, and a will is eager to follow its own course. A plain way normally "
       "needs one six. Eager, it needs none. Following a will should be easy: that is what wills are for. "
       "And look at the header: that thought used our last point."))
game("farm", F.mark("act_move"), F.mark("coop"),
     "With no points left, we can still do an act that is already waiting. An empty pool should not be a "
     "dead end. We click the act. It needs no sixes, so there is no roll.",
     F.R(12),
     W("Success. The result box says so, and lists what happened, one line each. Look at the header: a "
       "success refills all our points. Six again. And taking a way for the first time taught us Wayfaring. "
       "From now on, this way is known, and walking it is free."))
game("farm", F.mark("coop"), F.mark("coop_sloth"),
     "In the coop, we ask Ratcatcher to look at the people. A will only looks at the first step of its "
     "course, and nothing here is one.",
     F.R(13),
     A("Then we ask Hunt. A will's refusal does not count as the one refusal a thing allows. So Hunt can "
       "still refuse, and it does.", F.mark("coop_hunt")),
     F.R(14))
game("farm", F.mark("coop_sloth"), F.mark("think_hen"),
     "Third try: now the mind has to look. Sloth is a low-moral mind that observes and thinks.",
     F.R(15))
game("farm", F.mark("think_hen"), F.mark("coop_thing"),
     "This may be the most telling moment of the run. Watch what low morals do. We ask Sloth to think about "
     "the hen.",
     F.R(20),
     A("The game log explains it. Sloth is unscrupulous, so of the seven possible goals, it is only offered "
       "the two illegal ones. Hitting a hen that is not wild is a crime: the bird belongs to someone. Sloth "
       "chose to attack it. Then comes the choice of who does it. Diligence is a principled mind, so it "
       "refuses the crime before it is even offered.",
       F.click("CONTINUE", F.mark("think_hen"))),
     F.R(21))
manim("Morality")
manim("Rules")
game("farm", F.mark("coop_thing"), F.mark("party"),
     "Two more looks. Gourmandise refuses the things in the coop. Heraldry, asked next, has to look, and "
     "finds the nest box.",
     F.R(22), F.R(23),
     W("Our points are gone again. This member can do nothing more until an act succeeds."))
manim("Party")
game("farm", F.mark("party"), F.mark("rag_think"),
     "But a member is only one body. We click the other token: Raggedbrow, still in the Courtyard. He has "
     "his own three points, his own minds, and his own map, which is empty. He knows the butterfly's name, "
     "because the party has seen it. But he has not observed it himself. Caution refuses. Curiosity looks.",
     F.R(24), F.R(25))
game("farm", F.mark("rag_think"), F.mark("leave"),
     "Curiosity wants to examine it closely.",
     F.R(30),
     A("We pick Digging to do it, and it refuses at once. Digging does not observe, so it will not stand and "
       "look. That is a rule of choice, and the game tells you why.", F.click("CONTINUE", F.mark("rag_think"))),
     F.R(31))
game("farm", F.mark("leave"), F.mark("end"),
     "Everybody is out of points, so we leave. Leave walks every member out, from where they stand, by the "
     "ways they know. That is also why Go Back is gone: walking back where you have been should cost "
     "nothing.")

# ── 4. wills, more closely ────────────────────────────────────────────────────

manim("Will", "course")
manim("Will", "candidates")
game("city", C.mark("arrival") - 8.0, C.mark("diligence_think"),
     "A cobbler's shop, in the city. Five things, two people. We ask Petty Thief to look at a thing. As a "
     "will, it goes straight to the first step of its course.",
     C.R(0), fade=True)
game("city", C.mark("diligence_think"), C.mark("thief_think"),
     "Diligence wants work: a job, a bench, supplies to stock. We ask it to think about the same rack.",
     C.R(1),
     W("Nothing here leads to what it wants, so it refuses, and says it would have to leave. Diligence is "
       "also principled, so it would never be offered the theft anyway. This kind of refusal is not really a "
       "dead end. It tells you what this mind is looking for."))
manim("Will", "leave")

# ── 5. crime, discretion, getting out ─────────────────────────────────────────

game("city", C.mark("thief_think"), C.mark("steal"),
     "Petty Thief thinks about the rack.",
     C.R(4), C.R(5),
     W("A theft, needing two sixes. A will cannot refuse what it wants, and a low-moral mind cannot refuse a "
       "crime. The cobbler is in the room. But Petty Thief is discreet, and a discreet mind may try."))
manim("Proximity", "raw")
game("city", C.mark("steal"), C.mark("caught"),
     "We do it. Three dice, one six. It fails.",
     C.R(6),
     W("In the same room, a failure is seen. We are caught red-handed. This is where a crime should hurt."))
manim("Proximity", "effective")
game("city", C.mark("caught"), C.click("footer button") + 25.0,
     "Being caught starts a conversation. Losing it starts a fight.",
     C.R(7),
     A("We answer. Each reply on offer is written by one of our minds, with its name in front. Diligence "
       "denies it.", C.events_t("model-busy", 674.2)),
     C.R(10),
     C.R(11),
     A("Then Social Interaction answers.", C.click("CONTINUE", 830.0)),
     C.R(14),
     C.R(15),
     C.R(18),
     A("That reply is a check. We roll five dice, from the minds that spoke, our looks, and our coat. We "
       "need four sixes, and get one. We spend a humor to turn a die into a six. That makes two. Not "
       "enough.",
       C.click("humor hepar") - 12.0),
     C.R(19),
     W("We lose the conversation, and the cobbler attacks. On the map, the exit now says Runaway."), speed=1.5)
manim("Wages")
game("shop", S.mark("steal") - 3.0, S.mark("speak"),
     "Same shop, another try. This time Sloth thought of the theft, and Petty Thief was chosen to do it. "
     "The dice are the same, five, six, four. But this time we use a humor to turn one into a six. Success: "
     "a shoe last goes in the pack, and our points refill.", S.R(7), fade=True)
game("shop", S.mark("speak"), S.mark("rag_think"),
     "Under the rack there is a quotation mark: tell someone about it. Social Interaction tells Raggedbrow, "
     "who is standing nearby.",
     S.R(8, text=S.text(8).replace("Helen", "Raggedbrow")),
     W("That cost us a point. Switch to Raggedbrow, and the rack is on his map, already observed, with our "
       "words as his description."))
game("shop", S.mark("observe_cobbler"), S.mark("attack_refused"),
     "Back to us. We look at the cobbler, and ask Sloth about him.",
     S.R(15), S.R(20), S.R(21),
     W("Only crimes again. This time it is an assault, and Vigor is chosen to do it. Vigor is not discreet."))
game("shop", S.mark("attack_refused"), S.mark("reconsider"),
     "We do it.",
     S.R(22),
     W("A rule of action stops it. The cobbler is in the room, and an ordinary mind is refused. We chose "
       "this act, so the refusal costs a point, and the act keeps waiting."))
game("shop", S.mark("reconsider"), S.mark("leave"),
     "Reconsider asks a thinking mind to look at the act again. It keeps the goal, but gives the act to a "
     "different mind, one that no rule refuses here: a discreet one.",
     S.R(27), S.R(28),
     W("The old act stays, crossed out, and the new one waits: Petty Thief, needing one six. Hitting him "
       "would start a fight, so we leave it there."))
manim("Confronted", "closed")
game("flee", X.mark("confronted") - 12.0, X.mark("think_enemy"),
     "To show the rest, a debug option makes everyone treat us as an enemy. We walk into the shop. The "
     "cobbler is a skull, already on our map. While he confronts us, he is the only thing we can look at or "
     "think about.",
     fade=True)
game("flee", X.mark("runaway"), X.mark("end"),
     "The exit says Runaway. We roll the feet's two dice and need one six. A five and a six. We are out. "
     "Raggedbrow is still in the shop, but the cobbler's problem is with us, not him. Nothing stops him, so "
     "his exit says Leave.")
manim("Confronted", "exit")

# ── closing ───────────────────────────────────────────────────────────────────

segments.append({"type": "montage", "fade": True, "min": 20.0, "shots": [
    shot("farm", 1160.0, 1164.0, zoom=MAP),
    shot("farm", 1650.0, 1654.0, zoom=MAP),
    shot("shop", 668.0, 672.0, zoom=MAP),
    shot("shop", 1715.0, 1719.0, zoom=MAP),
    shot("flee", 300.0, 304.0, zoom=MAP),
], "vo": [
    "That is narration on the map. The rules underneath did not change. The minds, the dice, the wills and "
    "the crimes are the same. What changed is that you can see them, and see what your minds did in a "
    "place. Hopefully, that makes their choices, and yours, a little easier to follow. Thank you for "
    "playing.",
]})

# ── the short cut ─────────────────────────────────────────────────────────────
# Under ten minutes, for a viewer rather than a player: why the screen changed, the mind map, what minds
# are, a will at work, a low mind and its crime, a theft caught and the confrontation lost. The how-to
# (the legend, the costs, the dice, the party, the second try at the shop) is left to the long video.
# Picked by the opening words of each segment's narration, so a reworded line fails loudly here.

def pick(start, vo=None):
    for seg in segments:
        first = (seg.get("vo") or [""])[0]
        if (first if isinstance(first, str) else first["text"]).startswith(start):
            seg = json.loads(json.dumps(seg))
            if vo:
                seg["vo"] = vo(seg["vo"])
            return seg
    raise KeyError(start)


def first_says(text):
    return lambda vo: [text] + vo[1:]


short = [
    pick("Proscribed Palimpsest has a new", lambda vo: [vo[0],
         "This video shows why it changed, and what the rules underneath are for: what a mind is, how a "
         "will finds its way, and what a crime costs."]),
    pick("The old screen had its charm"),
    pick("Now the place is the page"),
    pick("So which mind does what?"),
    pick("Now a will.", first_says(
         "Here is the farm, being played. You choose a mind for each step, and some minds are wills. "
         "Ratcatcher wants one thing: to catch vermin. We ask it to think about a butterfly.")),
    pick("And when the place offers no course"),
    pick("This may be the most telling moment", first_says(
         "Now a mind with low morals: Sloth. We ask it to think about a hen.")),
    pick("Petty Thief thinks about the rack.", first_says(
         "A cobbler's shop, in the city. Petty Thief, a will to steal, thinks about a rack of shoes.")),
    pick("We do it. Three dice, one six."),
    pick("Being caught starts a conversation"),
    pick("That is narration on the map."),
]


def write(segs, output, story, script, title):
    storyboard = {
        "title": title,
        "output": output,
        "size": [1920, 1080],
        "fps": 30,
        "dither": {"animation": {"levels": 4, "scale": 2}, "game": {"levels": 6, "scale": 1}},
        "fast_forward": {"speed": 8, "min": 1.5, "label": False},
        "fit": "slow",
        "idle": {"after": 2.0, "before": 0.8, "speed": 4.0, "min": 1.5},
        "voice": {"name": "en_GB-alan-medium", "speed": 1.0, "grit": 0.15},
        "voice_styles": {"reading": {"speed": 0.88, "grit": 0, "noise_scale": 0.9, "noise_w": 1.1,
                                     "pitch": 0.97, "room": 0.16}},
        "music": {"wav": "build/music_bed.wav", "level_db": -34, "duck": 0.65},
        "clicks": {"gain": 0.5},
        "recordings": {
            "opening": {"script": "opening_map.cli", "dir": "build/rec_opening", "timeout": 7200,
                        "flags": "--organs encephalon=2 --no-encounters --npc-static --grant-mm "
                                 "diligence,provisioning,petty_thief,social_interaction --fill-party --humors all=voluptas "
                                 "--no-generate-ahead --save-path build/rec_opening/save.json"},
            "farm": {"dir": "build/rec_farm", "note": "Live take (make_video.py live/send), from the opening's save: "
                     "--no-encounters --npc-static --no-generate-ahead --save-path build/rec_farm_save.json"},
            "city": {"dir": "build/rec_city", "note": "Live take, from the farm's save: --start-area \"Cobbler's Shop\" "
                     "--no-encounters --npc-static --no-generate-ahead --save-path build/rec_city_save.json"},
            "shop": {"dir": "build/rec_shop", "note": "Live take, from the farm's save, as city."},
            "flee": {"dir": "build/rec_flee", "note": "Live take, from the farm's save, as city, with --npc-hostile."},
            "old_opening": {"dir": "../tutorial/build/rec_opening3", "note": "The tutorial's take (video-tutorial branch)."},
            "old_farm": {"dir": "../tutorial/build/rec_rest3", "note": "The tutorial's take (video-tutorial branch)."},
        },
        "manim_file": "scenes.py",
        "segments": segs,
    }
    (HERE / story).write_text(json.dumps(storyboard, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")

    lines = ["# Narration on the map: narration", "",
             "Generated by build_storyboard.py. Explanation is plain; *the game's own text, read in the second "
             "voice, is in italics*; [Scene] marks a manim scene.", ""]
    for s in segs:
        vo = s.get("vo") or []
        if s["type"] == "manim":
            lines.append(f"**[{s['scene']}{' ' + s['params']['step'] if s.get('params') else ''}]**")
        for p in vo:
            text = p if isinstance(p, str) else p["text"]
            lines.append(f"> *{text}*" if isinstance(p, dict) and p.get("style") == "reading" else text)
        if vo:
            lines.append("")
    (HERE / script).write_text("\n".join(lines) + "\n", encoding="utf-8")


write(segments, "build/narration_map.mp4", "storyboard.json", "SCRIPT.md", "Proscribed Palimpsest: narration on the map")
write(short, "build/narration_map_short.mp4", "storyboard_short.json", "SCRIPT_short.md",
      "Proscribed Palimpsest: narration on the map (short)")
print(f"{len(segments)} segments, {sum(1 for s in segments if s['type'] == 'manim')} diagrams; short: {len(short)}")
