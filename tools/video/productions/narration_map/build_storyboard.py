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
    "Proscribed Palimpsest has a new narration screen. The page of text with its clickable words is gone. "
    "In its place, the place itself: a map you read, click and fill as you play.",
    "What follows: why it changed, how it plays, and, since the rules now sit in plain sight, what each of "
    "them is trying to do: what the minds are for, how a will finds its way, and what a crime should cost.",
]})

# ── 1. before, and why ────────────────────────────────────────────────────────

game("old_farm", 457.39, 520.0, MANIM[("Principles", None)][0], zoom=None, fade=True, speed=2.0)
game("old_farm", 811.89, 828.81,
     "To act on the farmer, you clicked the word farmer, then chose what to do with it, then which mind "
     "would do it. Three menus, though perhaps only one of them was a real choice: which mind. The new "
     "screen keeps that one, and only that one.",
     zoom=None)
segments.append({"type": "manim", "scene": "Principles", "vo": [MANIM[("Principles", None)][1], WHY[("Principles", None)]],
                 "core": [0, 1]})
game("farm", F.mark("arrival") - 10.0, F.mark("observe_person"),
     "Here is the same farm, under the new screen. Every card is an area. The one lit is where we stand. "
     "Lines are the ways between them. The header keeps our name and our noetic points, six of them, and "
     "at the top, a single symbol: the way out.", fade=True)

# ── 2. reading the map ────────────────────────────────────────────────────────

manim("Legend", "kinds")
manim("Legend", "states")
manim("Legend", "tree")

# ── 3. a visit, as a player ───────────────────────────────────────────────────

game("farm", F.mark("observe_person"), F.click("CONTINUE", F.mark("observe_person")) + 1.5,
     "Let's play. Under the Courtyard stands one person, still unknown. We click it, and the popup asks the "
     "one question left in it: which mind will look. We pick Lullaby.",
     F.R(0),
     W("Lullaby declined. Minds are meant to have tastes: a mind may say that nothing of that kind draws it. "
       "That costs the point, and it hangs on the map as a crossed circle, with its words. But a thing can be "
       "declined only once. A mind's taste may colour a visit, but it should not lock you out of it."),
     fade=True)
game("farm", F.mark("observe_again"), F.click("CONTINUE", F.mark("observe_again")) + 2.0,
     "We click again. Lullaby is greyed in the popup: it has had its say. And whoever we ask now must look. "
     "Social Interaction.",
     F.R(1),
     W("A butterfly. The symbol drops under the card and becomes the root of a tree, with the text beneath "
       "it and a question mark: something to think about."))
manim("Chain", "steps")
manim("Functions")
manim("Properties")
game("farm", F.mark("will_ratcatcher"), F.click("CONTINUE", F.mark("will_ratcatcher") + 200) + 2.0,
     "Now a will. Ratcatcher wants one thing: to catch vermin. We ask it to think about the butterfly.",
     F.R(4),
     F.R(5),
     W("To Ratcatcher, a butterfly is small game. That is rather the charm of wills: they see the whole world through "
       "one want. Its course here is a single act, catching it, which now waits under the tree. Hunt was "
       "chosen to carry it out."))
manim("Chain", "dice")
game("farm", F.mark("observe_way"), F.click("CONTINUE", F.mark("observe_way")) + 1.5,
     "Hover that act and the map tells you its dice and the sixes it needs. This one needs four sixes, and "
     "the chain that made it casts three dice. It cannot succeed, so we leave it waiting. Instead, a way. "
     "Each way has its own symbol, on its line: exploring is perhaps the one thing worth aiming exactly, so no "
     "mind picks the way for you. We ask Heraldry to look at the way to the Chicken Coop.",
     F.R(6),
     W("Declined again: one way, one decline."))
game("farm", F.mark("observe_way2"), F.click("CONTINUE", F.mark("observe_way2")) + 1.5,
     "So the next mind must look. Childhood Memory.",
     F.R(7))
game("farm", F.mark("think_way"), F.mark("act_move"),
     "Ratcatcher again, on the way. Will it go in?",
     F.R(10),
     F.R(11),
     W("It will. The coop is on its course, and an act a will proposes for its own course is something it "
       "is eager for. A plain way is difficulty one; eager, nought. Following a will is meant to be easy: that is "
       "what wills are for. And notice the header: that thought spent our last point."))
game("farm", F.mark("act_move"), F.mark("coop"),
     "With no points left we can still do what is already waiting: an empty pool should not be "
     "a dead end. We click the act, and Execute. "
     "Difficulty nought is not rolled.",
     F.R(12),
     W("Success, and the result box says so on its border, with what it came to, one line each. Look at the "
       "header: a success refills the whole pool. Six points again. And going by a way for the first time "
       "taught Wayfaring: from now on that way is known, and walking it is free."))
game("farm", F.mark("coop"), F.mark("coop_sloth"),
     "In the coop, Ratcatcher is asked to look at the people. As a will, it only looks at what begins its "
     "course, and nothing here does.",
     F.R(13),
     A("Then Hunt. A will turning away is not the one decline a thing allows, so Hunt may still decline, "
       "and does.", F.mark("coop_hunt")),
     F.R(14))
game("farm", F.mark("coop_sloth"), F.mark("think_hen"),
     "Third asking: now the mind must look. Sloth, a low-moral mind that observes and thinks.",
     F.R(15))
game("farm", F.mark("think_hen"), F.mark("coop_thing"),
     "This may be the most telling moment of the run. Watch what a low moral level does. We ask Sloth to "
     "think about the hen.",
     F.R(20),
     A("Behind that thought, the game log says: Sloth is unscrupulous, offering only the two illegal goals "
       "of seven. Striking a hen that is not wild is a crime: the bird is somebody's. Sloth chose to attack "
       "it. Then the means: Diligence, a principled mind, refuses a crime before it is even offered.",
       F.click("CONTINUE", F.mark("think_hen"))),
     F.R(21))
manim("Morality")
manim("Rules")
game("farm", F.mark("coop_thing"), F.mark("party"),
     "Two more observations. Gourmandise declines the things in the coop; Heraldry, asked next, must look, "
     "and finds the nest box.",
     F.R(22), F.R(23),
     W("That is the pool spent again. Nothing more for this member, until an act succeeds."))
manim("Party")
game("farm", F.mark("party"), F.mark("rag_think"),
     "But a member is only one body. We click the other token: Raggedbrow, still in the Courtyard. "
     "His own three points, his own minds, his own map, empty. The butterfly is known to him by name, "
     "since the party has seen it, but he has not observed it. Caution declines; Curiosity looks.",
     F.R(24), F.R(25))
game("farm", F.mark("rag_think"), F.mark("leave"),
     "Curiosity wants to examine it closely.",
     F.R(30),
     A("And Digging, chosen to do it, refuses at once. A mind that does not observe will not stand and look: "
       "a rule of choice, the game tells you why.", F.click("CONTINUE", F.mark("rag_think"))),
     F.R(31))
game("farm", F.mark("leave"), F.mark("end"),
     "Everybody is out of points, so we leave. Leave walks every member out, each from where it stands, by "
     "the ways they know. Go Back is gone for the same reason: walking where you have already been should "
     "hardly cost a thought.")

# ── 4. wills, more closely ────────────────────────────────────────────────────

manim("Will", "course")
manim("Will", "candidates")
game("city", C.mark("arrival") - 8.0, C.mark("diligence_think"),
     "A cobbler's shop, in the city. Five things, two people. We ask Petty Thief to look at a thing: as a "
     "will, it goes straight to what begins its course.",
     C.R(0), fade=True)
game("city", C.mark("diligence_think"), C.mark("thief_think"),
     "Diligence wants work: a job, a bench, stores to lay in. We ask it to think about the same rack.",
     C.R(1),
     W("No course to any of its wants exists here, so it declines, saying it would have to leave. "
       "Diligence is also principled, so it would never have been offered the theft. Such a refusal is not "
       "quite a dead end, it tells you what this mind is looking for."))
manim("Will", "leave")

# ── 5. crime, discretion, getting out ─────────────────────────────────────────

game("city", C.mark("thief_think"), C.mark("steal"),
     "Petty Thief thinks about the rack.",
     C.R(4), C.R(5),
     W("A theft, difficulty two. A will cannot refuse an act it wants, and an unscrupulous one cannot refuse "
       "a crime. The cobbler is in the room, but Petty Thief is discreet, and a discreet mind may try."))
manim("Proximity", "raw")
game("city", C.mark("steal"), C.mark("caught"),
     "Execute. Three dice, one six. It fails.",
     C.R(6),
     W("And in the same room, a failure is seen. Caught red-handed. This is where a crime is meant to bite."))
manim("Proximity", "effective")
game("city", C.mark("caught"), C.click("footer button") + 25.0,
     "A confrontation is a conversation, and losing it is a fight.",
     C.R(7),
     A("We answer. Every reply on offer is written by one of our minds, its name before it. Diligence "
       "denies it.", C.events_t("model-busy", 674.2)),
     C.R(10),
     C.R(11),
     A("Social Interaction answers next.", C.click("CONTINUE", 830.0)),
     C.R(14),
     C.R(15),
     C.R(18),
     A("That reply is a check. Five dice: the levels of the minds that spoke, our looks, our coat. Four sixes "
       "needed, and one comes up. We spend a humor, and a die turns to six: two. Not enough.",
       C.click("humor hepar") - 12.0),
     C.R(19),
     W("The conversation is lost, and the cobbler fights. On the map, the exit now reads Runaway."), speed=1.5)
manim("Wages")
game("shop", S.mark("steal") - 3.0, S.mark("speak"),
     "Same shop, another try. This time Sloth thought of the theft, and Petty Thief was chosen to do it. "
     "Same dice, five, six, four, but this time we turn one with a humor: success, a shoe last in the pack, "
     "and the pool refilled.", S.R(7), fade=True)
game("shop", S.mark("speak"), S.mark("rag_think"),
     "Under the rack, a quotation mark: speak of it. Social Interaction tells Raggedbrow, standing nearby.",
     S.R(8, text=S.text(8).replace("Helen", "Raggedbrow")),
     W("It cost us a point. Switch to Raggedbrow, and the rack is on his map, observed, with our words as "
       "his observation."))
game("shop", S.mark("observe_cobbler"), S.mark("attack_refused"),
     "Back to us. We observe the cobbler, and ask Sloth about him.",
     S.R(15), S.R(20), S.R(21),
     W("Only crimes again, and this time an assault, with Vigor as the means. Vigor is not discreet."))
game("shop", S.mark("attack_refused"), S.mark("reconsider"),
     "Execute.",
     S.R(22),
     W("A rule of action. The cobbler is in the room, and an ordinary mind is refused. The act was chosen, "
       "so the refusal costs a point, and the act stays, waiting."))
game("shop", S.mark("reconsider"), S.mark("leave"),
     "Reconsider asks a thinking mind to look at the act again. Holding to it, it hands the act to another "
     "acting mind, one no rule would refuse here: a discreet one.",
     S.R(27), S.R(28),
     W("The old act is kept, struck through, and the new one waits: Petty Thief, difficulty one. A swing at "
       "him would start a fight, so we leave it there."))
manim("Confronted", "closed")
game("flee", X.mark("confronted") - 12.0, X.mark("think_enemy"),
     "To see the rest, a debug flag makes every person count us an enemy. We walk into the shop, and the "
     "cobbler is a skull, already on our map. Confronted, he is all we may look at or think about.",
     fade=True)
game("flee", X.mark("runaway"), X.mark("end"),
     "The exit reads Runaway. The feet's dice: two of them, one six needed, and a five and a six. We are "
     "out. Raggedbrow is not: he stands in the same shop, but the cobbler's quarrel is with us, not with "
     "him. Nothing confronts him, and his exit reads Leave.")
manim("Confronted", "exit")

# ── closing ───────────────────────────────────────────────────────────────────

segments.append({"type": "montage", "fade": True, "min": 20.0, "shots": [
    shot("farm", 1160.0, 1164.0, zoom=MAP),
    shot("farm", 1650.0, 1654.0, zoom=MAP),
    shot("shop", 668.0, 672.0, zoom=MAP),
    shot("shop", 1715.0, 1719.0, zoom=MAP),
    shot("flee", 300.0, 304.0, zoom=MAP),
], "vo": [
    "That is narration on the map. The rules underneath did not change: the minds, the dice, the wills and "
    "the crimes are as they were. What changed is that you can see them, and see what your minds made of "
    "a place. Hopefully it makes their choices, and yours, a little easier to read. Thank you for playing.",
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
         "What follows: why it changed, and what the rules underneath it are trying to do: what a mind is, "
         "how a will finds its way, and what a crime should cost."]),
    pick("The old screen had its charm"),
    pick("Now the place is the page"),
    pick("So which mind is asked what?"),
    pick("Now a will.", first_says(
         "Here is the farm, played. Every mind is chosen by hand, and some minds are wills. Ratcatcher wants "
         "one thing: to catch vermin. We ask it to think about a butterfly.")),
    pick("And when no course exists"),
    pick("This may be the most telling moment", first_says(
         "Now a mind with low morals: Sloth. We ask it to think about a hen.")),
    pick("Petty Thief thinks about the rack.", first_says(
         "A cobbler's shop, in the city. Petty Thief, a will to steal, thinks about a rack of shoes.")),
    pick("Execute. Three dice, one six."),
    pick("A confrontation is a conversation"),
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
