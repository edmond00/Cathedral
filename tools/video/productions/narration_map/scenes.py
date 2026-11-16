"""Animations for the changelog video "narration on the map".

Every rule drawn here is the manual's (docs/manual on narration-map, synced to the code) or the design
note's (design/NARRATION_MAP.md):
  Principles  - what the new screen stands on (NARRATION_MAP §1-§5)
  Legend      - the symbols, by kind; an element's three states; a tree (NARRATION_MAP §1, Config.Symbols)
  Chain       - observe -> think -> act, the pool, the difficulty, the budget (VI §3, §5)
  Functions   - the eight functions, and the exclusions (II §1)
  Properties  - what a modus mentis declares, on three real ones (II §1; the *ModusMentis.cs classes)
  Rules       - rules of action and rules of choice (VI §6)
  Will        - a will's course, its candidates, its eagerness, "I would have to leave" (II §7, VI §6)
  Morality    - the four behaviours (VI §7)
  Proximity   - raw and effective proximity, discretion, the three rungs (VI §7)
  Confronted  - what a confrontation closes, explaining oneself, LEAVE and RUNAWAY (VI §7)
  Wages       - what a failure costs (VI §8)
  Party       - each member its own body; speaking to a companion (VI §10)
"""

from vidkit.manimkit import *  # noqa: F401,F403


# The map's own glyphs (src/Config.cs, Config.Symbols)
WAY, PERSON, HOSTILE, THING, CONTENT = "∩", "☺", "☠", "◆", "◇"
THINK, SPEAK, TEXT, REFUSAL, FAIL, OK, FEEL = "?", "❝", "¶", "⊘", "✗", "✓", "♥"
DIFF = "①②③④⑤⑥⑦⑧⑨⑩"
SYMBOL_FONT = "DejaVu Sans Mono"          # Consolas has few of these; the game falls back the same way


def glyph(s: str, size: float = 40, color: str = GRAY85) -> Text:
    return Text(s, font=SYMBOL_FONT, font_size=size, color=color)


def die(scene, face, color=GRAY85, size=0.62):
    sq = RoundedRectangle(corner_radius=0.08, width=size, height=size, stroke_color=color, stroke_width=2,
                          fill_color=BLACK_, fill_opacity=1)
    return VGroup(sq, scene.text(str(face), 28 * size / 0.62, color).move_to(sq))


def marks(n: int, color=GOLD) -> VGroup:
    """A level shown as marks, one a level, as the popup shows it."""
    return VGroup(*[Rectangle(width=0.1, height=0.26, stroke_width=0, fill_color=color, fill_opacity=1)
                    for _ in range(n)]).arrange(RIGHT, buff=0.07)


def step(scene) -> str:
    return str(scene.params.get("step", ""))


# ── principles ────────────────────────────────────────────────────────────────

PRINCIPLES = [
    ("the place is the page", "no scrolling text, no keywords: the location map is the narration screen"),
    ("one click, one thing, one text", "you choose what is looked at; the mind chooses how"),
    ("a symbol says what it is", "a way, a person, a hostile, a thing: you know what a click will look at"),
    ("nothing scrolls away", "every text becomes a node on a tree, kept for the whole visit"),
    ("where you have been is free", "known ways are walked at no cost and on no roll"),
]


class Principles(BrandScene):
    """Five lines, one after the other, each a principle and what it means."""

    def construct(self):
        h = self.header("narration on the map", "what the new screen stands on")
        rows = VGroup()
        for i, (title, what) in enumerate(PRINCIPLES):
            n = self.text(f"{i + 1}", 30, LIGHT_PURPLE)
            t = self.text(title, 30, GOLD)
            w = self.text(what, 20, GRAY75)
            VGroup(t, w).arrange(DOWN, aligned_edge=LEFT, buff=0.1)
            rows.add(VGroup(n, VGroup(t, w)).arrange(RIGHT, buff=0.4, aligned_edge=UP))
        rows.arrange(DOWN, aligned_edge=LEFT, buff=0.32)
        rows.scale_to_fit_width(min(rows.width, config.frame_width - 1.2))
        rows.next_to(h, DOWN, buff=0.45)
        for i, r in enumerate(rows):
            self.wait_until(0.05 + 0.17 * i)
            self.play(FadeIn(r, shift=RIGHT * 0.2), run_time=0.7)
        self.fill_to_target()


# ── the legend ────────────────────────────────────────────────────────────────

KINDS = [
    (WAY, "way", "a neighbouring area, a door, a stair, a path, a climb", GOLD),
    (PERSON, "person", "anyone, or any creature, that is not your enemy", GRAY85),
    (HOSTILE, "hostile", "one that counts you an enemy", LIGHT_PURPLE),
    (THING, "thing", "every other point of interest: a cart, a bush, a chest", GRAY85),
    (CONTENT, "content", "what a container was seen to hold", GRAY75),
]

STATES = [
    ("unknown", GRAY50, "only its kind and where it stands",
     "click the kind: the mind picks one"),
    ("known", GRAY85, "remembered from an earlier visit, or seen inside a container",
     "named: a click observes that one"),
    ("observed", GOLD, "looked at on this visit, by this body",
     "the root of a tree: it can be thought about"),
]


class Legend(BrandScene):
    """params step: "kinds" (the four kinds), "states" (unknown, known, observed), "tree" (a whole tree)."""

    def construct(self):
        s = step(self) or "kinds"
        getattr(self, "do_" + s)()
        self.fill_to_target()

    def do_kinds(self):
        self.header("four kinds of symbol", "what a click will look at")
        rows = VGroup()
        for g, name, what, col in KINDS:
            rows.add(VGroup(glyph(g, 46, col), self.text(name, 28, col), self.text(what, 20, GRAY60)))
        gw = max(r[0].width for r in rows)
        nw = max(r[1].width for r in rows)
        for i, r in enumerate(rows):
            y = -i * 0.95
            r[0].move_to([-5.4, y, 0])
            r[1].move_to([-4.7 + nw / 2, y, 0]).align_to([-4.7, 0, 0], LEFT)
            r[2].next_to([-4.7 + nw + 0.5, y, 0], RIGHT, buff=0)
        rows.center().shift(DOWN * 0.7)
        rows[-1].set_opacity(0.0)
        for i, r in enumerate(rows[:-1]):
            self.wait_until(0.04 + 0.16 * i)
            self.play(FadeIn(r, shift=RIGHT * 0.2), run_time=0.6)
        self.wait_until(0.72)
        rows[-1].set_opacity(1)
        self.play(FadeIn(rows[-1], shift=RIGHT * 0.2), run_time=0.6)

    def do_states(self):
        self.header("three states", "of every element, for one body")
        cols = VGroup()
        for i, (name, col, what, click) in enumerate(STATES):
            sym = glyph(PERSON, 64, col)
            if name == "known":
                sym = VGroup(sym, self.text("the cobbler", 20, GRAY85)).arrange(DOWN, buff=0.12)
            elif name == "observed":
                sym = VGroup(sym, self.text("the cobbler", 20, GOLD),
                             glyph("│", 26, GRAY50), glyph(TEXT, 30, GRAY75)).arrange(DOWN, buff=0.06)
            t = self.text(name.upper(), 28, col)
            w = Paragraph(*self.wrap(what, 26), font=FONT, font_size=19, color=GRAY75, alignment="center", line_spacing=0.8)
            c = Paragraph(*self.wrap(click, 26), font=FONT, font_size=19, color=LIGHT_PURPLE, alignment="center", line_spacing=0.8)
            cols.add(VGroup(t, sym, w, c).arrange(DOWN, buff=0.3))
        cols.arrange(RIGHT, buff=0.7, aligned_edge=UP).shift(DOWN * 0.55)
        for i, c in enumerate(cols):
            c[1].align_to(cols[0][1], UP)
        arrows = VGroup(*[Arrow(cols[i][0].get_right(), cols[i + 1][0].get_left(), buff=0.2, stroke_width=2,
                                color=GRAY50, max_tip_length_to_length_ratio=0.12) for i in range(2)])
        for i, c in enumerate(cols):
            self.wait_until(0.04 + 0.28 * i)
            anims = [FadeIn(c, shift=UP * 0.15)]
            if i:
                anims.append(GrowArrow(arrows[i - 1]))
            self.play(*anims, run_time=0.8)

    @staticmethod
    def wrap(s: str, width: int) -> list[str]:
        out, line = [], ""
        for w in s.split():
            if line and len(line) + 1 + len(w) > width:
                out.append(line)
                line = w
            else:
                line = (line + " " + w).strip()
        return out + [line]

    def do_tree(self):
        """The tree of one person, as it grows during a visit, every node labelled."""
        self.header("a tree", "everything a visit wrote, where it grew")
        specs = [  # (glyph, colour, label, depth, side note)
            (PERSON, GOLD, "the cobbler", 0, "observed: the root"),
            (TEXT, GRAY75, "what the observing mind saw", 1, "hover to read it again"),
            (THINK, LIGHT_PURPLE, "think about the cobbler", 1, "while there is something to do"),
            (TEXT, GRAY75, "what the thinking mind wanted", 2, None),
            (DIFF[1], GOLD, "an act waiting: two sixes needed", 2, "click it whenever you like"),
            (OK, GOLD, "success: what it came to", 3, "the pool refills"),
            (FEEL, LIGHT_PURPLE, "what it stirred", 3, None),
            (REFUSAL, GRAY50, "a refusal: a mind that would not", 1, "the next asking must attend"),
            (SPEAK, GRAY85, "spoken of to a companion", 1, "planted on their map"),
        ]
        nodes = VGroup()
        y = 1.15
        for g, col, label, depth, note in specs:
            sym = glyph(g, 30, col)
            lab = self.text(label, 20, col if col != GRAY50 else GRAY60)
            row = VGroup(sym, lab).arrange(RIGHT, buff=0.25)
            row.move_to([0, y, 0]).align_to([-5.6 + depth * 0.75, 0, 0], LEFT)
            if note:
                n = self.text(note, 17, GRAY50).move_to([3.9, y, 0]).align_to([2.4, 0, 0], LEFT)
                row.add(n)
            nodes.add(row)
            y -= 0.62
        # the lines of the tree: each node to its parent (the last shallower row above it)
        lines = VGroup()
        for i, (_, _, _, depth, _) in enumerate(specs):
            if depth == 0:
                lines.add(VMobject())
                continue
            p = max(j for j in range(i) if specs[j][3] == depth - 1)
            ps = nodes[p][0]
            cs = nodes[i][0]
            corner = [ps.get_x(), cs.get_y(), 0]
            lines.add(VMobject(stroke_color=GRAY35, stroke_width=2).set_points_as_corners(
                [ps.get_bottom() + DOWN * 0.05, corner, cs.get_left() + LEFT * 0.08]))
        self.play(FadeIn(nodes[0]), run_time=0.6)
        n = len(specs) - 1
        for i in range(1, len(specs)):
            self.wait_until(0.05 + 0.85 * (i - 1) / n)
            self.play(Create(lines[i]), FadeIn(nodes[i], shift=RIGHT * 0.15), run_time=0.55)


# ── the chain ─────────────────────────────────────────────────────────────────

class Chain(BrandScene):
    """params step: "steps" (the three steps and what each costs), "dice" (the pool and the sixes)."""

    def construct(self):
        s = step(self) or "steps"
        getattr(self, "do_" + s)()
        self.fill_to_target()

    def boxes(self):
        specs = [("OBSERVE", "an observing mind", "presence needed", "1 point"),
                 ("THINK", "a thinking mind", "from anywhere", "1 point"),
                 ("ACT", "an acting mind", "walks there first", "free")]
        out = VGroup()
        for name, who, where, cost in specs:
            b = self.node(name, GOLD, 30)
            w = self.text(who, 20, GRAY85)
            p = self.text(where, 18, GRAY60)
            c = self.text(cost, 22, LIGHT_PURPLE)
            out.add(VGroup(b, w, p, c).arrange(DOWN, buff=0.18))
        out.arrange(RIGHT, buff=1.4).shift(UP * 0.3)
        return out

    def do_steps(self):
        self.header("one act, three minds")
        cols = self.boxes()
        for c in cols[1:]:
            c[0].set_y(cols[0][0].get_y())
        arrows = VGroup(*[Arrow(cols[i][0].get_right(), cols[i + 1][0].get_left(), buff=0.12, stroke_width=2.5,
                                color=GRAY50, max_tip_length_to_length_ratio=0.15) for i in range(2)])
        for i, c in enumerate(cols):
            self.wait_until(0.03 + 0.2 * i)
            anims = [FadeIn(c[:3], shift=DOWN * 0.1)]
            if i:
                anims.append(GrowArrow(arrows[i - 1]))
            self.play(*anims, run_time=0.7)
        self.wait_until(0.62)
        self.play(LaggedStart(*[FadeIn(c[3]) for c in cols], lag_ratio=0.3), run_time=1.0)
        pool = VGroup(*[Circle(0.14, color=GOLD, fill_color=GOLD, fill_opacity=1) for _ in range(4)]).arrange(RIGHT, buff=0.16)
        lab = self.text("noetic points", 22, GOLD)
        bud = VGroup(lab, pool).arrange(RIGHT, buff=0.3).to_edge(DOWN, buff=1.3)
        self.wait_until(0.74)
        self.play(FadeIn(bud), run_time=0.5)
        self.play(pool[3].animate.set_fill(BLACK_, 1), pool[2].animate.set_fill(BLACK_, 1), run_time=0.6)
        ok = self.text("a success refills the whole pool;  a failure refills nothing", 22, GRAY85).next_to(bud, DOWN, buff=0.35)
        self.wait_until(0.86)
        self.play(FadeIn(ok), *[p.animate.set_fill(GOLD, 1) for p in pool[2:]], run_time=0.8)

    def do_dice(self):
        self.header("the dice", "one die per level, in every mind of the chain")
        parts = [("observing", 2), ("thinking", 1), ("acting", 2), ("implement", 1)]
        terms = VGroup()
        for i, (who, lv) in enumerate(parts):
            terms.add(VGroup(self.text(who, 20, GRAY75 if who != "implement" else GRAY60), marks(lv)).arrange(DOWN, buff=0.1))
            if i < len(parts) - 1:
                terms.add(self.text("+", 26, GRAY60))
        terms.add(self.text("=", 26, GRAY60), self.text("6 dice", 28, GOLD))
        terms.arrange(RIGHT, buff=0.3).shift(UP * 1.5)
        self.play(FadeIn(terms, lag_ratio=0.12), run_time=1.2)
        faces = [6, 2, 4, 6, 1, 3]
        dice = VGroup(*[die(self, f, GOLD if f == 6 else GRAY85) for f in faces]).arrange(RIGHT, buff=0.3).shift(UP * 0.1)
        self.wait_until(0.25)
        self.play(LaggedStart(*[FadeIn(d, scale=0.6) for d in dice], lag_ratio=0.12), run_time=1.0)
        need = VGroup(glyph(DIFF[1], 40, GOLD), self.text("difficulty 2: two sixes needed", 24, GOLD)).arrange(RIGHT, buff=0.3)
        need.next_to(dice, DOWN, buff=0.45)
        self.wait_until(0.4)
        self.play(FadeIn(need), run_time=0.5)
        rules = VGroup(
            self.text("an eager mind:      one six fewer", 21, GRAY85),
            self.text("a reluctant mind:   one six more", 21, GRAY85),
            self.text("difficulty above the pool: it cannot succeed", 21, LIGHT_PURPLE),
            self.text("difficulty nought: no roll, it simply succeeds", 21, GRAY75),
        ).arrange(DOWN, aligned_edge=LEFT, buff=0.16).next_to(need, DOWN, buff=0.45)
        for i, r in enumerate(rules):
            self.wait_until(0.55 + 0.1 * i)
            self.play(FadeIn(r, shift=RIGHT * 0.15), run_time=0.5)


# ── the eight functions ───────────────────────────────────────────────────────

FUNCTIONS = [
    ("Observation", "it perceives: attends, and writes what it finds"),
    ("Thinking", "it deliberates: settles on a goal"),
    ("Action", "it performs: willing, reluctant, or it refuses"),
    ("Speaking", "it speaks: the replies of a conversation"),
    ("Fighting", "it unlocks fighting skills"),
    ("Emotion", "it feels: answers what an act came to"),
    ("Will", "it wants: observes and deliberates, toward its wants only"),
    ("Craft", "it knows a trade: a station, an implement"),
]


class Functions(BrandScene):
    """The eight functions; then the exclusions; then the will singled out."""

    def construct(self):
        self.header("what a mind can do", "one to three functions each, of eight")
        rows = VGroup()
        for name, what in FUNCTIONS:
            col = GOLD if name == "Will" else GRAY85
            rows.add(VGroup(self.text(name, 26, col), self.text(what, 19, GRAY60)))
        nw = max(r[0].width for r in rows)
        for i, r in enumerate(rows):
            r[0].move_to([0, -i * 0.56, 0]).align_to([-5.8, 0, 0], LEFT)
            r[1].move_to([0, -i * 0.56, 0]).align_to([-5.8 + nw + 0.5, 0, 0], LEFT)
        rows.scale_to_fit_width(min(rows.width, 9.2)).center().to_edge(LEFT, buff=0.5).shift(DOWN * 0.75)
        self.play(LaggedStart(*[FadeIn(r, shift=RIGHT * 0.15) for r in rows], lag_ratio=0.15), run_time=2.2)
        ex = VGroup(self.text("never both:", 20, LIGHT_PURPLE),
                    self.text("Thinking / Action", 20, GRAY85),
                    self.text("Action / Emotion", 20, GRAY85),
                    self.text("to want it, to do it,", 18, GRAY60),
                    self.text("to be moved by it:", 18, GRAY60),
                    self.text("three minds, not one", 18, GRAY60)).arrange(DOWN, aligned_edge=LEFT, buff=0.12)
        ex.to_edge(RIGHT, buff=0.45).align_to(rows, UP)
        self.wait_until(0.45)
        self.play(FadeIn(ex, lag_ratio=0.1), run_time=1.0)
        self.wait_until(0.7)
        will = rows[6]
        box = SurroundingRectangle(will, color=GOLD, buff=0.12, stroke_width=2)
        self.play(Create(box), rows[:6].animate.set_opacity(0.35), rows[7].animate.set_opacity(0.35), run_time=0.8)
        self.fill_to_target()


# ── a modus mentis's properties ───────────────────────────────────────────────

# Read off the classes (src/game/narrative/modimentis/*ModusMentis.cs). A will declares no readiness to
# doubt and is medium (II §1).
CARDS = [
    ("Beast Sense", [("functions", "Observation, Thinking"), ("sources", "nose + ears"), ("memory", "sensory"),
                     ("moral", "high"), ("doubt", "low: never thinks twice"), ("discreet", "no")]),
    ("Petty Thief", [("functions", "Will, Action"), ("wants", "steal something"), ("sources", "hands + eyes"),
                     ("memory", "procedural"), ("moral", "low"), ("doubt", "medium (a will)"), ("discreet", "yes")]),
    ("Diligence", [("functions", "Will, Speaking, Action"), ("wants", "find work, a bench, lay in stores"),
                   ("sources", "hands + backbone"), ("memory", "procedural"), ("moral", "high"),
                   ("doubt", "medium (a will)"), ("discreet", "no")]),
]

MEANINGS = [
    ("sources", "one region, or two organs: they set the ceiling a level can reach; a wound lowers it"),
    ("memory", "which module it can be filed in: procedural, semantic or sensory"),
    ("moral", "low, medium or high: read only when it thinks or acts"),
    ("doubt", "how many second thoughts: none, one, or two"),
    ("discreet", "it may act with someone in the room, and is not heard through a wall"),
]


class Properties(BrandScene):
    """Three real minds as cards, then what each property means."""

    def construct(self):
        self.header("what a mind declares")
        cards = VGroup()
        for name, props in CARDS:
            title = self.text(name, 26, GOLD)
            body = VGroup(*[VGroup(self.text(k, 17, GRAY60), self.text(v, 17, GRAY85)).arrange(RIGHT, buff=0.2)
                            for k, v in props]).arrange(DOWN, aligned_edge=LEFT, buff=0.12)
            kw = max(r[0].width for r in body)
            for r in body:
                r[1].align_to(r[0], LEFT).shift(RIGHT * (kw + 0.2))
            inner = VGroup(title, body).arrange(DOWN, aligned_edge=LEFT, buff=0.22)
            cards.add(VGroup(self.frame_box(inner, GRAY50, 0.22), inner))
        cards.arrange(RIGHT, buff=0.3, aligned_edge=UP)
        cards.scale_to_fit_width(min(cards.width, config.frame_width - 0.6))
        cards.move_to(UP * 0.55)
        for i, c in enumerate(cards):
            self.wait_until(0.02 + 0.13 * i)
            self.play(FadeIn(c, shift=UP * 0.15), run_time=0.7)
        means = VGroup(*[VGroup(self.text(k, 19, LIGHT_PURPLE), self.text(v, 18, GRAY75)).arrange(RIGHT, buff=0.25)
                         for k, v in MEANINGS]).arrange(DOWN, aligned_edge=LEFT, buff=0.12)
        mw = max(r[0].width for r in means)
        for r in means:
            r[1].align_to(r[0], LEFT).shift(RIGHT * (mw + 0.25))
        means.scale_to_fit_width(min(means.width, config.frame_width - 0.8))
        means.to_edge(DOWN, buff=0.35)
        for i, m in enumerate(means):
            self.wait_until(0.45 + 0.1 * i)
            self.play(FadeIn(m, shift=RIGHT * 0.15), run_time=0.5)
        self.fill_to_target()


# ── the two families of rule ──────────────────────────────────────────────────

class Rules(BrandScene):
    """Rules of action reject and cost a point; rules of choice withhold and cost nothing."""

    def construct(self):
        self.header("two families of rule")
        left = VGroup(self.text("RULES OF ACTION", 28, LIGHT_PURPLE),
                      self.text("judge an act already chosen", 20, GRAY85),
                      self.text("they reject it, in the mind's voice", 20, GRAY85),
                      self.text("and it costs a point", 20, LIGHT_PURPLE),
                      self.text("circumstance:", 18, GRAY60),
                      self.text("\"that man is watching\"", 18, GRAY75),
                      self.text("\"that door needs a key\"", 18, GRAY75),
                      self.text("\"my arm is ruined\"", 18, GRAY75)).arrange(DOWN, aligned_edge=LEFT, buff=0.15)
        right = VGroup(self.text("RULES OF CHOICE", 28, GOLD),
                       self.text("narrow what is offered", 20, GRAY85),
                       self.text("they withhold, silently", 20, GRAY85),
                       self.text("and it costs nothing", 20, GOLD),
                       self.text("character:", 18, GRAY60),
                       self.text("\"that never occurred to me\"", 18, GRAY75),
                       self.text("a high mind is not offered a crime", 18, GRAY75),
                       self.text("a speaking mind turns to people", 18, GRAY75)).arrange(DOWN, aligned_edge=LEFT, buff=0.15)
        VGroup(left, right).arrange(RIGHT, buff=1.2, aligned_edge=UP).shift(DOWN * 0.45)
        lb = self.frame_box(left, LIGHT_PURPLE, 0.3)
        rb = self.frame_box(right, GOLD, 0.3)
        self.play(Create(lb), FadeIn(left[:4], lag_ratio=0.2), run_time=1.2)
        self.wait_until(0.25)
        self.play(FadeIn(left[4:], lag_ratio=0.25), run_time=1.0)
        self.wait_until(0.5)
        self.play(Create(rb), FadeIn(right[:4], lag_ratio=0.2), run_time=1.2)
        self.wait_until(0.72)
        self.play(FadeIn(right[4:], lag_ratio=0.25), run_time=1.0)
        self.fill_to_target()


# ── a will ────────────────────────────────────────────────────────────────────

class Will(BrandScene):
    """params step: "course" (the shortest course, eager at its end), "candidates" (at most three acts),
    "leave" (no course here: only leaving offers it; a will sent out from the world)."""

    def construct(self):
        s = step(self) or "course"
        getattr(self, "do_" + s)()
        self.fill_to_target()

    def course(self, labels, colors=None):
        nodes = VGroup(*[self.node(l, (colors or {}).get(i, GRAY85), 20) for i, l in enumerate(labels)])
        nodes.arrange(RIGHT, buff=0.75)
        nodes.scale_to_fit_width(min(nodes.width, config.frame_width - 0.8))
        arrows = VGroup(*[Arrow(a.get_right(), b.get_left(), buff=0.06, stroke_width=2, color=GRAY50,
                                max_tip_length_to_length_ratio=0.25) for a, b in zip(nodes, nodes[1:])])
        return nodes, arrows

    def do_course(self):
        self.header("a will", "Petty Thief wants one thing: to steal")
        nodes, arrows = self.course(["you, in the street", "open the shop door", "walk to the shelf", "STEAL"],
                                    {3: GOLD})
        g = VGroup(nodes, arrows).shift(UP * 0.6)
        self.play(FadeIn(nodes[0]), run_time=0.5)
        for i in range(1, 4):
            self.wait_until(0.05 + 0.12 * i)
            self.play(GrowArrow(arrows[i - 1]), FadeIn(nodes[i], shift=RIGHT * 0.1), run_time=0.6)
        notes = VGroup(
            self.text("its course: the shortest chain of acts to what it wants", 21, GRAY85),
            self.text("it looks only at what begins that course", 21, GRAY85),
            self.text("asked about anything else, it declines and names the first step", 21, GRAY75),
            self.text("doing the act it wants: eager, one six fewer, and it never refuses", 21, GOLD),
        ).arrange(DOWN, aligned_edge=LEFT, buff=0.2).next_to(g, DOWN, buff=0.7)
        for i, n in enumerate(notes):
            self.wait_until(0.5 + 0.11 * i)
            self.play(FadeIn(n, shift=RIGHT * 0.15), run_time=0.5)

    def do_candidates(self):
        self.header("what a will thinks of", "acts that begin a course of at most three")
        rows = VGroup()
        for acts, ok in ((["STEAL the purse"], True), (["open the chest", "STEAL from it"], True),
                         (["climb the wall", "walk the roof", "STEAL the coin"], True),
                         (["wait for night", "force the door", "walk in", "STEAL"], False)):
            n = VGroup(*[self.node(a, GOLD if a.startswith("STEAL") else (GRAY85 if ok else GRAY50), 18) for a in acts]).arrange(RIGHT, buff=0.35)
            tag = self.text(f"{len(acts)} act{'s' if len(acts) > 1 else ''}", 18, GOLD if ok else LIGHT_PURPLE)
            mark = glyph(OK if ok else REFUSAL, 26, GOLD if ok else LIGHT_PURPLE)
            rows.add(VGroup(mark, tag, n).arrange(RIGHT, buff=0.35))
        rows.arrange(DOWN, aligned_edge=LEFT, buff=0.32).shift(DOWN * 0.2 + LEFT * 0.3)
        rows.scale_to_fit_width(min(rows.width, config.frame_width - 0.8))
        for i, r in enumerate(rows):
            self.wait_until(0.05 + 0.17 * i)
            self.play(FadeIn(r, shift=RIGHT * 0.15), run_time=0.6)
        note = self.text("of those, it proposes the shortest; a second thought takes the next", 20, GRAY75).to_edge(DOWN, buff=0.45)
        self.wait_until(0.78)
        self.play(FadeIn(note), run_time=0.5)

    def do_leave(self):
        self.header("no course here", "\"I would have to leave to find what I want\"")
        here = self.node("this place", GRAY50, 24)
        here_note = self.text("no shelf, no purse:\nthe will declines everything,\nand it costs nothing", 19, GRAY75, line_spacing=0.8)
        left = VGroup(here, here_note).arrange(DOWN, buff=0.3).to_edge(LEFT, buff=0.5).shift(DOWN * 0.3)
        self.play(FadeIn(left), run_time=0.8)
        places = [("a cobbler's shop", "2 hours", True), ("the alehouse", "1 hour", False),
                  ("a farm", "3 hours", False)]
        world = VGroup()
        for name, cost, ok in sorted(places, key=lambda p: p[1]):
            world.add(VGroup(self.node(name, GOLD if ok else GRAY60, 20), self.text(cost, 18, GRAY60),
                             self.text("a course to stealing" if ok else "none", 18, GOLD if ok else GRAY50))
                      .arrange(RIGHT, buff=0.35))
        world.arrange(DOWN, aligned_edge=LEFT, buff=0.3).to_edge(RIGHT, buff=0.6).shift(DOWN * 0.2)
        title = self.text("sent out from the world: nearest first", 21, LIGHT_PURPLE).next_to(world, UP, buff=0.45).align_to(world, LEFT)
        self.wait_until(0.4)
        self.play(FadeIn(title), run_time=0.5)
        self.play(LaggedStart(*[FadeIn(w, shift=RIGHT * 0.15) for w in world], lag_ratio=0.3), run_time=1.4)
        self.wait_until(0.75)
        note = self.text("the first with a course is the journey's end, and the visit is made for that want",
                         19, GRAY85).to_edge(DOWN, buff=0.45)
        note.scale_to_fit_width(min(note.width, config.frame_width - 0.8))
        self.play(FadeIn(note), run_time=0.6)


# ── morality ──────────────────────────────────────────────────────────────────

class Morality(BrandScene):
    """The four behaviours: high/low, thinking/acting."""

    def construct(self):
        self.header("morality", "two decisions, two minds")
        cells = {
            ("high", "think"): "crimes are never offered",
            ("high", "act"): "refuses a crime, unasked",
            ("low", "think"): "if a crime is on offer,\nonly crimes are offered",
            ("low", "act"): "cannot refuse a crime",
        }
        colh = [self.text("a THINKING mind", 24, GRAY85), self.text("an ACTING mind", 24, GRAY85)]
        rowh = [self.text("HIGH", 26, GOLD), self.text("LOW", 26, LIGHT_PURPLE)]
        grid = VGroup()
        for r, lvl in enumerate(("high", "low")):
            for c, side in enumerate(("think", "act")):
                t = self.text(cells[(lvl, side)], 21, GRAY85, line_spacing=0.8)
                box = Rectangle(width=4.6, height=1.5, stroke_color=GOLD if lvl == "high" else LIGHT_PURPLE, stroke_width=1.5)
                box.move_to([c * 4.9 - 1.6, -r * 1.75 + 0.1, 0])
                grid.add(VGroup(box, t.move_to(box)))
        for i, h in enumerate(colh):
            h.next_to(grid[i][0], UP, buff=0.25)
        for i, h in enumerate(rowh):
            h.next_to(grid[i * 2][0], LEFT, buff=0.35)
        self.play(FadeIn(VGroup(*colh)), FadeIn(VGroup(*rowh)), run_time=0.7)
        order = [0, 1, 2, 3]
        for k, i in enumerate(order):
            self.wait_until(0.06 + 0.17 * k)
            self.play(Create(grid[i][0]), FadeIn(grid[i][1]), run_time=0.6)
        note = self.text("the goal and the means are two choices: a scrupulous hand can still be\nchosen to carry out what a low mind settled on",
                         19, GRAY75, line_spacing=0.8).to_edge(DOWN, buff=0.35)
        self.wait_until(0.8)
        self.play(FadeIn(note), run_time=0.6)
        self.fill_to_target()


# ── being seen ────────────────────────────────────────────────────────────────

def room(scene, w, h, label, color=GRAY50):
    r = Rectangle(width=w, height=h, stroke_color=color, stroke_width=1.8)
    t = scene.text(label, 16, GRAY60).next_to(r.get_corner(UL), DR, buff=0.12)
    return VGroup(r, t)


class Proximity(BrandScene):
    """params step: "raw" (may I act at all?) or "effective" (what does failing cost?)."""

    def construct(self):
        s = step(self) or "raw"
        getattr(self, "do_" + s)()
        self.fill_to_target()

    def house(self):
        shop = room(self, 4.2, 2.6, "the shop")
        back = room(self, 3.0, 2.6, "the back room").next_to(shop, RIGHT, buff=0)
        street = room(self, 7.2, 1.2, "the street (another section)").next_to(VGroup(shop, back), DOWN, buff=0.5)
        door = Line(shop[0].get_corner(DL) + RIGHT * 1.6, shop[0].get_corner(DL) + RIGHT * 2.4, color=BLACK_, stroke_width=6)
        g = VGroup(shop, back, street, door)
        return g, shop, back, street

    def do_raw(self):
        self.header("may I act at all?", "raw proximity")
        g, shop, back, street = self.house()
        g.scale(0.85).to_edge(LEFT, buff=0.5).shift(DOWN * 0.6)
        you = glyph("@", 34, GOLD).move_to(shop[0].get_center() + LEFT * 0.8)
        w = glyph(PERSON, 34, GRAY85).move_to(shop[0].get_center() + RIGHT * 1.0)
        self.play(Create(g), run_time=1.0)
        self.play(FadeIn(you), FadeIn(w), run_time=0.5)
        notes = VGroup(
            self.text("someone in the room:", 21, GRAY85),
            self.text("an ordinary mind is refused", 20, LIGHT_PURPLE),
            self.text("(a rule of action: a point spent)", 18, GRAY60),
            self.text("a discreet mind may try", 20, GOLD),
            self.text("and may not back out of it", 18, GRAY60),
        ).arrange(DOWN, aligned_edge=LEFT, buff=0.16).to_edge(RIGHT, buff=0.4).shift(DOWN * 0.3)
        for i, n in enumerate(notes):
            self.wait_until(0.2 + 0.13 * i)
            self.play(FadeIn(n, shift=RIGHT * 0.1), run_time=0.5)

    def do_effective(self):
        self.header("what does failing cost?", "effective proximity, where discretion counts")
        rows = [("VISUAL", "same room", "caught in the act:\na confrontation", "they attack"),
                ("AUDIO", "same section", "they come to look", "they come to look"),
                ("NONE", "further off", "nothing", "nothing")]
        head = VGroup(self.text("", 20), self.text("", 20), self.text("a WITNESS", 22, GRAY85), self.text("an ENEMY", 22, LIGHT_PURPLE))
        table = VGroup()
        xs = [-5.0, -2.9, 0.6, 4.2]
        for c, h in enumerate(head):
            h.move_to([xs[c], 1.0, 0])
        table.add(head)
        for r, (rung, where, wit, en) in enumerate(rows):
            y = 0.1 - r * 1.05
            cells = VGroup(self.text(rung, 24, GOLD), self.text(where, 18, GRAY60),
                           self.text(wit, 19, GRAY85, line_spacing=0.8), self.text(en, 19, GRAY85))
            for c, cell in enumerate(cells):
                cell.move_to([xs[c], y, 0])
            table.add(cells)
        self.play(FadeIn(head), run_time=0.5)
        for i in range(1, 4):
            self.wait_until(0.05 + 0.15 * i)
            self.play(FadeIn(table[i], shift=RIGHT * 0.1), run_time=0.55)
        disc = self.text("discretion: VISUAL stays VISUAL; AUDIO falls to NONE", 22, GOLD)
        disc2 = self.text("silent through a wall, never in the same room", 19, GRAY75)
        VGroup(disc, disc2).arrange(DOWN, buff=0.15).to_edge(DOWN, buff=0.4)
        self.wait_until(0.66)
        self.play(FadeIn(disc), Indicate(table[2][0], color=GOLD), run_time=0.8)
        self.play(FadeIn(disc2), run_time=0.5)


class Confronted(BrandScene):
    """params step: "closed" (what a confrontation allows) or "exit" (LEAVE and RUNAWAY)."""

    def construct(self):
        s = step(self) or "closed"
        getattr(self, "do_" + s)()
        self.fill_to_target()

    def do_closed(self):
        self.header("confronted", "an enemy in the room, or a witness with a reason")
        allowed = VGroup(self.text("still open", 26, GOLD),
                         self.text("the acts that answer them", 23, GRAY85),
                         self.text("explain yourself: one six easier", 23, GRAY85),
                         self.text("acts already waiting here", 23, GRAY85),
                         self.text("(an ordinary mind is refused,", 20, GRAY60),
                         self.text(" a discreet one may try)", 20, GRAY60),
                         self.text("the way out, as a RUNAWAY", 23, GRAY85)).arrange(DOWN, aligned_edge=LEFT, buff=0.15)
        closed = VGroup(self.text("closed", 26, LIGHT_PURPLE),
                        self.text("looking at anything else", 23, GRAY60),
                        self.text("thinking about anything else", 23, GRAY60),
                        self.text("walking away", 23, GRAY60),
                        self.text("speaking to a companion", 23, GRAY60)).arrange(DOWN, aligned_edge=LEFT, buff=0.15)
        VGroup(allowed, closed).arrange(RIGHT, buff=1.6, aligned_edge=UP).shift(DOWN * 0.5)
        self.play(FadeIn(allowed[0]), FadeIn(closed[0]), run_time=0.5)
        for i in range(1, len(allowed)):
            self.wait_until(0.05 + 0.08 * i)
            self.play(FadeIn(allowed[i], shift=RIGHT * 0.1), run_time=0.4)
        self.wait_until(0.65)
        self.play(LaggedStart(*[FadeIn(c, shift=RIGHT * 0.1) for c in closed[1:]], lag_ratio=0.2), run_time=1.2)
        note = self.text("someone asleep confronts nobody, and witnesses nothing", 19, GRAY75).to_edge(DOWN, buff=0.4)
        self.wait_until(0.85)
        self.play(FadeIn(note), run_time=0.5)

    def do_exit(self):
        self.header("the way out")
        leave = VGroup(self.text("LEAVE", 34, GOLD),
                       self.text("nobody stops you", 23, GRAY60),
                       self.text("every member walks out", 23, GRAY85),
                       self.text("by known ways, for free", 23, GRAY85),
                       self.text("a walk that meets someone", 23, GRAY85),
                       self.text("who would confront you stops there", 23, GRAY85)).arrange(DOWN, aligned_edge=LEFT, buff=0.15)
        run = VGroup(self.text("RUNAWAY", 34, LIGHT_PURPLE),
                     self.text("confronted", 23, GRAY60),
                     self.text("the feet's dice: one six needed", 23, GRAY85),
                     self.text("humors may bend them", 23, GRAY85),
                     self.text("checked again at every area", 23, GRAY85),
                     self.text("failing: the fight, or the talk", 23, LIGHT_PURPLE)).arrange(DOWN, aligned_edge=LEFT, buff=0.15)
        VGroup(leave, run).arrange(RIGHT, buff=0.9, aligned_edge=UP).shift(DOWN * 0.4)
        self.play(FadeIn(leave[:2]), run_time=0.5)
        self.play(LaggedStart(*[FadeIn(x, shift=RIGHT * 0.1) for x in leave[2:]], lag_ratio=0.3), run_time=1.6)
        self.wait_until(0.48)
        self.play(FadeIn(run[:2]), run_time=0.5)
        dice = VGroup(*[die(self, f, GOLD if f == 6 else GRAY85, 0.5) for f in (3, 6)]).arrange(RIGHT, buff=0.2)
        dice.next_to(run[0], RIGHT, buff=0.4)
        self.play(LaggedStart(*[FadeIn(x, shift=RIGHT * 0.1) for x in run[2:]], lag_ratio=0.3),
                  FadeIn(dice, scale=0.6), run_time=1.6)


# ── the wages of failure ──────────────────────────────────────────────────────

class Wages(BrandScene):
    """A failure costs the budget, at most one social consequence, and one penalty of four kinds."""

    def construct(self):
        self.header("what a failure costs")
        first = VGroup(self.text("1  the pool is not refilled", 22, GRAY85),
                       self.text("2  caught, attacked, or someone comes to look", 22, GRAY85),
                       self.text("3  and one penalty, drawn from what that act risks:", 22, GRAY85)).arrange(DOWN, aligned_edge=LEFT, buff=0.2)
        first.shift(UP * 0.9)
        for i, f in enumerate(first):
            self.wait_until(0.04 + 0.12 * i)
            self.play(FadeIn(f, shift=RIGHT * 0.1), run_time=0.5)
        wages = [("a wound", "an attempt that can hurt you", LIGHT_PURPLE),
                 ("the way lost", "finding a way: you stand outside again", GOLD),
                 ("the hour wasted", "the hard and the slow: time moves on", MEDIUM_YELLOW),
                 ("the thread lost", "the senses, speech, waiting: one more point", GRAY75)]
        cards = VGroup()
        for name, what, col in wages:
            t = self.text(name, 24, col)
            w = Paragraph(*Legend.wrap(what, 20), font=FONT, font_size=17, color=GRAY75, alignment="center", line_spacing=0.8)
            inner = VGroup(t, w).arrange(DOWN, buff=0.2)
            box = Rectangle(width=max(3.0, inner.width + 0.4), height=1.6, stroke_color=col, stroke_width=1.6).move_to(inner)
            cards.add(VGroup(box, inner))
        cards.arrange(RIGHT, buff=0.25).next_to(first, DOWN, buff=0.55)
        cards.scale_to_fit_width(min(cards.width, config.frame_width - 0.6))
        for i, c in enumerate(cards):
            self.wait_until(0.4 + 0.12 * i)
            self.play(FadeIn(c, shift=UP * 0.15), run_time=0.5)
        self.fill_to_target()


# ── the party ─────────────────────────────────────────────────────────────────

class Party(BrandScene):
    """Each member its own body: its place, its points, its minds, its trees; speech plants a root."""

    def construct(self):
        self.header("the party", "each member is its own body in the place")
        cols = VGroup()
        for name, tok in (("you", "@"), ("your companion", "♞")):
            rows = VGroup(VGroup(glyph(tok, 40, GOLD), self.text(name, 24, GOLD)).arrange(RIGHT, buff=0.3),
                          self.text("stands where it was last taken", 19, GRAY85),
                          self.text("its own noetic points", 19, GRAY85),
                          self.text("its own minds", 19, GRAY85),
                          self.text("its own trees", 19, GRAY85)).arrange(DOWN, aligned_edge=LEFT, buff=0.16)
            cols.add(rows)
        cols.arrange(RIGHT, buff=2.2, aligned_edge=UP).shift(UP * 0.35)
        self.play(FadeIn(cols[0][0]), FadeIn(cols[1][0]), run_time=0.6)
        for i in range(1, 5):
            self.wait_until(0.04 + 0.08 * i)
            self.play(FadeIn(cols[0][i]), FadeIn(cols[1][i]), run_time=0.4)
        arrow = CurvedArrow(cols[0][4].get_right() + RIGHT * 0.1, cols[1][4].get_left() + LEFT * 0.1, angle=-0.6,
                            color=LIGHT_PURPLE, stroke_width=2.5)
        speak = VGroup(glyph(SPEAK, 30, LIGHT_PURPLE), self.text("speak of it: a point,\na root on their map", 18, LIGHT_PURPLE, line_spacing=0.8)).arrange(RIGHT, buff=0.2)
        speak.next_to(arrow, DOWN, buff=0.15)
        self.wait_until(0.5)
        self.play(Create(arrow), FadeIn(speak), run_time=0.8)
        notes = VGroup(self.text("clicking a token switches member, for free; the hour is common", 19, GRAY75),
                       self.text("a beast is offered no conversation, no lock, no tool, no letters", 19, GRAY75),
                       self.text("a fight takes only those standing where it breaks out", 19, GRAY75)).arrange(DOWN, buff=0.12)
        notes.to_edge(DOWN, buff=0.35)
        for i, n in enumerate(notes):
            self.wait_until(0.66 + 0.1 * i)
            self.play(FadeIn(n), run_time=0.45)
        self.fill_to_target()
