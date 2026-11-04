"""Animations for the tutorial ("the first hour").

Every rule drawn here is the manual's, which is derived from the code (docs/manual, synced at HEAD):
  System        - how a character fits together: body -> derived measures, body -> a modus mentis's ceiling,
                  levels -> dice (01-anatomy.md §6, 02-memory.md §3, 06-narration.md §5); its example is the
                  tutorial's own character: Swordsmanship on arms and hands (SwordsmanshipModusMentis.Organs),
                  the contributions by MaxLevelCurve.Linear (round(3 x score / max), away from zero)
  Organs        - the derived measures, by source (docs/manual/01-anatomy.md §6; src/game/narrative/body/stats/)
  NarrationTurn - the three steps of an act and the noetic budget (06-narration.md §3, §5)
  DiceHumors    - the pool, the difficulty, the transmuting virtues and the viscera limit
                  (06-narration.md §5, 03-humors.md §4-5)
  AttackDefence - the exchange in a fight (08-fighting.md §6)
"""

from vidkit.manimkit import *  # noqa: F401,F403


# ── 1. organs -> systems ──────────────────────────────────────────────────────

# (system, [(source, what it sets)]) in the order the creation footage takes them; kept short enough to
# read at a glance at half size
SYSTEMS = [
    ("thinking and memory", [
        ("encephalon", "noetic points"),
        ("anamnesis", "places remembered"),
        ("cerebrum", "knowledge"),
        ("cerebellum", "practised skills"),
        ("hippocampus", "sensing minds"),
        ("pineal gland", "experience, doubt"),
    ]),
    ("the senses", [
        ("eyes", "reach"),
        ("ears", "initiative"),
        ("nose", "danger, food"),
    ]),
    ("speech", [
        ("tongue", "replies"),
        ("visage", "charm"),
        ("teeth", "hard food"),
    ]),
    ("the humors", [
        ("paunch", "a queue"),
        ("hepar", "a queue"),
        ("spleen", "a queue"),
        ("pulmones", "a queue"),
        ("viscera", "humors per roll"),
    ]),
    ("life", [
        ("heart", "lifespan, party"),
        ("trunk", "health"),
        ("genitories", "blow dice"),
    ]),
    ("the body in a fight", [
        ("backbone", "movement, load"),
        ("hands", "tools"),
        ("legs", "stride"),
        ("lower limbs", "journeys, defence"),
        ("feet", "footing, flight"),
    ]),
]


class Organs(BrandScene):
    """Six panels, one per system: each organ on one line with what it sets, in type large enough to read."""

    def construct(self):
        self.header("one body, every measure")
        panels = []
        for name, rows in SYSTEMS:
            names = [self.text(src, 30, GOLD) for src, _ in rows]
            whats = [self.text(what, 26, GRAY85) for _, what in rows]
            col = max(n.width for n in names) + 0.35
            lines = VGroup()
            for n, w in zip(names, whats):
                w.next_to(n, RIGHT, buff=0).align_to(n, DOWN).shift(RIGHT * (col - n.width))
                lines.add(VGroup(n, w))
            lines.arrange(DOWN, aligned_edge=LEFT, buff=0.16)
            box = self.frame_box(lines, GRAY50, buff=0.25, label=name.upper())
            panels.append(VGroup(box, lines))
        top = VGroup(*panels[:3]).arrange(RIGHT, buff=0.3, aligned_edge=UP)
        bottom = VGroup(*panels[3:]).arrange(RIGHT, buff=0.3, aligned_edge=UP)
        grid = VGroup(top, bottom).arrange(DOWN, buff=0.35)
        grid.scale_to_fit_width(min(grid.width, config.frame_width - 0.6))
        if grid.height > config.frame_height - 1.9:
            grid.scale_to_fit_height(config.frame_height - 1.9)
        grid.to_edge(DOWN, buff=0.25)
        for p in panels:
            self.play(Create(p[0][0]), FadeIn(p[0][1:]), FadeIn(p[1], shift=UP * 0.1, lag_ratio=0.1), run_time=0.8)
            self.beat(0.1, 0.5)
        self.fill_to_target()


# ── 1b. how a character fits together ────────────────────────────────────────

class System(BrandScene):
    """How a character fits together, in five steps, one segment each (params "step"), so each picture
    comes with the sentences about it. A small map at the top (body, measures, ways of thinking) lights
    the part the step is about; below it, the step works through this very character:
      1 body       its five regions and their scores (as created in the take)
      2 measures   backbone 4 -> 8 movement points; head 10 -> 6 noetic points
      3 minds      Swordsmanship on arms (2 of 4: +2) and hands (4 of 6: +2): ceiling 5
      4 dice       a level is dice: Swordsmanship level 2 rolls two; sixes are what count
      5 around     items lend dice, clothes protect and charm, humors bend a die"""

    def mini_map(self, lit):
        def box(name, color, on):
            t = self.text(name, 20, color if on else GRAY50)
            r = RoundedRectangle(corner_radius=0.06, width=t.width + 0.4, height=t.height + 0.3,
                                 stroke_color=color if on else GRAY35, stroke_width=2.5 if on else 1.2)
            return VGroup(r.move_to(t), t)
        body = box("BODY", GOLD, lit in ("body", "measures", "minds"))
        meas = box("MEASURES", GRAY85, lit == "measures")
        minds = box("WAYS OF THINKING", LIGHT_PURPLE, lit in ("minds", "dice"))
        dice = box("DICE", LIGHT_PURPLE, lit == "dice")
        row = VGroup(meas, body, minds, dice).arrange(RIGHT, buff=0.9)
        arrows = VGroup(
            Arrow(body.get_left(), meas.get_right(), buff=0.08, stroke_width=2,
                  color=GOLD if lit == "measures" else GRAY35, max_tip_length_to_length_ratio=0.2),
            Arrow(body.get_right(), minds.get_left(), buff=0.08, stroke_width=2,
                  color=GOLD if lit == "minds" else GRAY35, max_tip_length_to_length_ratio=0.2),
            Arrow(minds.get_right(), dice.get_left(), buff=0.08, stroke_width=2,
                  color=LIGHT_PURPLE if lit == "dice" else GRAY35, max_tip_length_to_length_ratio=0.2))
        g = VGroup(row, arrows)
        g.scale_to_fit_width(min(g.width, config.frame_width - 1.0))
        return g.next_to(self.head, DOWN, buff=0.3)

    def bar(self, score, top, color, size=0.34):
        return VGroup(*[Square(size, stroke_color=GRAY50, stroke_width=1.5, fill_color=color if i < score else BLACK_,
                               fill_opacity=1 if i < score else 0) for i in range(top)]).arrange(RIGHT, buff=0.05)

    def construct(self):
        step = int(self.params.get("step", 1))
        names = {1: "body", 2: "measures", 3: "minds", 4: "dice", 5: "around"}
        self.head = self.header("how a character works", animate=(step == 1))
        m = self.mini_map(names[step])
        self.add(m) if step > 1 else self.play(FadeIn(m), run_time=0.6)
        stage = DOWN * 0.9
        getattr(self, "step_" + names[step])(stage)
        self.fill_to_target()

    def step_body(self, at):
        regions = [("head", 10, 23), ("face", 11, 22), ("trunk", 20, 28), ("arms", 6, 10), ("legs", 6, 10)]
        rows = VGroup()
        for name, score, top in regions:
            n = self.text(name, 28, GOLD)
            b = self.bar(score, top, GOLD, 0.2)
            v = self.text(f"{score}", 26, GRAY85)
            rows.add(VGroup(n, b, v))
        col = max(r[0].width for r in rows) + 0.4
        for r in rows:
            r[1].next_to(r[0], RIGHT, buff=col - r[0].width)
            r[2].next_to(r[1], RIGHT, buff=0.3)
        rows.arrange(DOWN, aligned_edge=LEFT, buff=0.22).move_to(at)
        self.beat(0.15)
        for r in rows:
            self.play(FadeIn(r[0]), LaggedStart(*[FadeIn(q) for q in r[1]], lag_ratio=0.02), FadeIn(r[2]), run_time=0.7)
        self.beat(0.05)
        sum_note = self.text("a region scores the sum of its organs", 24, GRAY60).next_to(rows, DOWN, buff=0.3)
        self.play(FadeIn(sum_note), run_time=0.5)
        # but most rules read one organ on its own
        self.wait_until(0.42)
        organs = VGroup(*[VGroup(self.text(o, 24, GOLD), self.text("->", 22, GRAY50), self.text(w, 24, GRAY85))
                          .arrange(RIGHT, buff=0.15)
                          for o, w in (("ears", "initiative"), ("backbone", "movement"), ("heart", "lifespan"))])
        organs.arrange(RIGHT, buff=0.7).next_to(sum_note, DOWN, buff=0.3)
        organs.scale_to_fit_width(min(organs.width, config.frame_width - 1.4))
        for o in organs:
            self.play(FadeIn(o, shift=UP * 0.1), run_time=0.5)
            self.beat(0.04, minimum=0.2)

    def step_measures(self, at):
        def row(src, score, top, arrow_label, value, what):
            n = self.text(src, 30, GOLD)
            b = self.bar(score, top, GOLD, 0.26)
            ar = Arrow(LEFT, RIGHT, stroke_width=3, color=GOLD, buff=0).scale(0.8)
            lab = self.text(arrow_label, 20, GOLD)
            v = self.text(f"{value} {what}", 30, GRAY85)
            b.next_to(n, RIGHT, buff=0.35)
            ar.next_to(b, RIGHT, buff=0.35)
            lab.next_to(ar, UP, buff=0.05)
            v.next_to(ar, RIGHT, buff=0.35)
            return VGroup(n, b, ar, lab, v)
        r1 = row("backbone", 4, 4, "x 2", 8, "movement points")
        r2 = row("head", 10, 23, "half, + 1", 6, "noetic points")
        both = VGroup(r1, r2).arrange(DOWN, aligned_edge=LEFT, buff=0.7)
        both.scale_to_fit_width(min(both.width, config.frame_width - 1.4)).move_to(at)
        for r, wait in ((r1, 0.12), (r2, 0.2)):
            self.beat(wait)
            self.play(FadeIn(r[0]), FadeIn(r[1], lag_ratio=0.05), run_time=0.6)
            self.play(GrowArrow(r[2]), FadeIn(r[3]), run_time=0.5)
            self.play(FadeIn(r[4], shift=RIGHT * 0.2), run_time=0.5)

    def step_minds(self, at):
        # like a classic skill, and unlike one
        cols = []
        for k, (head, color, lines) in enumerate((
                ("like a skill", GOLD, ("a level", "experience from use", "its level in dice")),
                ("unlike a skill", LIGHT_PURPLE, ("learned by living", "can be forgotten", "a voice of its own")))):
            h = self.text(head, 30, color)
            items = VGroup(*[self.text(l, 26, GRAY85) for l in lines]).arrange(DOWN, aligned_edge=LEFT, buff=0.22)
            cols.append(VGroup(h, items.next_to(h, DOWN, aligned_edge=LEFT, buff=0.35)))
        table = VGroup(*cols).arrange(RIGHT, aligned_edge=UP, buff=1.6).move_to(at + UP * 0.2)
        for k, c in enumerate(cols):
            self.wait_until(0.04 + k * 0.2)
            self.play(FadeIn(c[0]), run_time=0.4)
            for item in c[1]:
                self.play(FadeIn(item, shift=RIGHT * 0.15), run_time=0.4)
                self.beat(0.03, minimum=0.2)
        self.wait_until(0.5)
        self.play(FadeOut(table), run_time=0.5)
        title = self.text("SWORDSMANSHIP", 30, LIGHT_PURPLE).move_to(at + UP * 1.4)
        self.play(FadeIn(title), run_time=0.5)
        rows = []
        for i, (name, score, top) in enumerate((("arms", 2, 4), ("hands", 4, 6))):
            n = self.text(name, 28, GOLD)
            b = self.bar(score, top, GOLD)
            f = self.text(f"{score} of {top}", 24, GRAY85)
            pl = self.text("+2", 30, LIGHT_PURPLE)
            n.move_to(at + LEFT * 4.2 + UP * (0.45 - i * 0.8), aligned_edge=LEFT)
            b.next_to(n, RIGHT, buff=1.6 - n.width + 0.3)
            f.next_to(b, RIGHT, buff=0.4)
            pl.next_to(f, RIGHT, buff=0.6)
            rows.append(VGroup(n, b, f, pl))
        self.beat(0.03)
        for r in rows:
            self.play(FadeIn(r[0]), LaggedStart(*[FadeIn(q) for q in r[1]], lag_ratio=0.08), FadeIn(r[2]), run_time=0.8)
            self.beat(0.06)
            self.play(FadeIn(r[3], scale=1.4), run_time=0.4)
        ceiling = self.text("ceiling  1 + 2 + 2 = 5", 32, LIGHT_PURPLE).move_to(at + DOWN * 1.35)
        self.play(TransformFromCopy(VGroup(rows[0][3], rows[1][3]), ceiling), run_time=0.9)
        self.wait_until(0.86)
        hint = self.text("hover an organ: it lists the ceilings it raises", 24, GRAY60).next_to(ceiling, DOWN, buff=0.3)
        self.play(FadeIn(hint), run_time=0.5)

    def step_dice(self, at):
        lv = self.text("Swordsmanship, level 2", 30, LIGHT_PURPLE).move_to(at + UP * 1.3)
        squares = VGroup(*[Square(0.42, stroke_color=LIGHT_PURPLE, stroke_width=2,
                                  fill_color=LIGHT_PURPLE if i < 2 else BLACK_, fill_opacity=0.8 if i < 2 else 0)
                           for i in range(5)]).arrange(RIGHT, buff=0.1).next_to(lv, DOWN, buff=0.3)
        self.play(FadeIn(lv), FadeIn(squares, lag_ratio=0.15), run_time=0.8)
        self.beat(0.15)

        def die(face, color=GRAY85):
            sq = RoundedRectangle(corner_radius=0.08, width=0.7, height=0.7, stroke_color=color, stroke_width=2,
                                  fill_color=BLACK_, fill_opacity=1)
            return VGroup(sq, self.text(str(face), 30, color).move_to(sq))
        dice = VGroup(die(3), die(6, GOLD)).arrange(RIGHT, buff=0.4).move_to(at + DOWN * 0.5)
        self.play(TransformFromCopy(squares[:2], dice), run_time=0.8)
        self.beat(0.1)
        rule = self.text("each six counts: the task says how many it needs", 26, GOLD).next_to(dice, DOWN, buff=0.4)
        self.play(FadeIn(rule), Indicate(dice[1], color=GOLD), run_time=0.8)

    def step_around(self, at):
        lines = [("what you carry", "can lend dice to an act", GOLD),
                 ("what you wear", "protects in a fight, charms in a conversation", GOLD),
                 ("the humors", "made by four organs, can turn a die after it is rolled", LIGHT_PURPLE)]
        rows = VGroup()
        for a, b, c in lines:
            rows.add(VGroup(self.text(a, 28, c), self.text(b, 24, GRAY85)).arrange(DOWN, aligned_edge=LEFT, buff=0.08))
        rows.arrange(DOWN, aligned_edge=LEFT, buff=0.4).move_to(at)
        for r in rows:
            self.beat(0.12)
            self.play(FadeIn(r, shift=UP * 0.15), run_time=0.6)


# ── 1c. memory ────────────────────────────────────────────────────────────────

KIND_COLOR = {"procedural": GOLD, "semantic": LIGHT_PURPLE, "sensory": GRAY85}
# this run's own ways of thinking, by the memory they are kept in (their MemoryType in the code)
RUN_MINDS = {"Empathy": "sensory", "Bargaining": "semantic", "Knotwork": "procedural",
             "Nautical Jargon": "semantic", "Exploration": "sensory", "Vigor": "procedural",
             "Endurance": "procedural", "Hard Labor": "procedural", "Archaeology": "sensory"}


class Memory(BrandScene):
    """Where a modus mentis lives (docs/manual/02-memory.md §5-6), in three steps (params "step"):
      1 arrive    a new one enters working memory at the front; a full queue loses its oldest for good
      2 file      consolidate into the long-term memory of its kind; reject to make room
      3 residual  archive from long-term memory to the residual queue; its oldest is then forgotten"""

    def chip(self, name):
        color = KIND_COLOR[RUN_MINDS[name]]
        t = self.text(name, 20, color)
        r = RoundedRectangle(corner_radius=0.08, width=2.0, height=0.5, stroke_color=color, stroke_width=2,
                             fill_color=BLACK_, fill_opacity=1)
        if t.width > 1.8:
            t.scale_to_fit_width(1.8)
        return VGroup(r, t.move_to(r))

    def slots(self, n, label, sub, y, color=GRAY50):
        row = VGroup(*[RoundedRectangle(corner_radius=0.08, width=2.25, height=0.6, stroke_color=GRAY35,
                                        stroke_width=1.2) for _ in range(n)]).arrange(RIGHT, buff=0.12)
        row.move_to(UP * y)
        lab = self.text(label, 20, color).next_to(row, UP, buff=0.12).align_to(row, LEFT)
        note = self.text(sub, 18, GRAY60).next_to(lab, RIGHT, buff=0.3)
        return VGroup(row, lab, note)

    def construct(self):
        step = int(self.params.get("step", 1))
        self.header("memory", animate=(step == 1))
        wm = self.slots(5, "WORKING MEMORY", "everything learned arrives here, at the front", 1.75)
        self.add(wm) if step > 1 else self.play(FadeIn(wm), run_time=0.6)
        getattr(self, f"step{step}")(wm)
        self.fill_to_target()

    def place(self, chips, row):
        for c, slot in zip(chips, row):
            c.move_to(slot)

    def step1(self, wm):
        row = wm[0]
        order = ["Exploration", "Nautical Jargon", "Knotwork", "Bargaining", "Empathy"]   # front first
        chips = []
        for k, name in enumerate(reversed(order)):          # Empathy came first
            c = self.chip(name).move_to(row[0].get_left() + LEFT * 1.6)
            anims = [c.animate.move_to(row[0])] + [chips[i].animate.move_to(row[len(chips) - i])
                                                    for i in range(len(chips))]
            self.play(*anims, run_time=0.5)
            chips.append(c)
        self.beat(0.15)
        new = self.chip("Vigor").move_to(row[0].get_left() + LEFT * 1.6)
        lost = chips[0]                                      # the oldest, at the back
        self.play(new.animate.move_to(row[0]),
                  *[chips[i].animate.move_to(row[len(chips) - i]) for i in range(1, len(chips))],
                  lost.animate.shift(RIGHT * 2.4 + DOWN * 0.6).set_opacity(0.2), run_time=1.0)
        note = self.text("full: the oldest falls out, lost for good", 22, LIGHT_PURPLE)
        note.next_to(row, DOWN, buff=0.7).align_to(row, RIGHT)
        self.play(FadeIn(note), FadeOut(lost), run_time=0.6)

    def long_term(self):
        boxes = VGroup()
        for kind, organ in (("procedural", "cerebellum"), ("semantic", "cerebrum"), ("sensory", "hippocampus")):
            r = RoundedRectangle(corner_radius=0.1, width=4.35, height=1.1, stroke_color=KIND_COLOR[kind],
                                 stroke_width=1.5)
            lab = self.text(f"{kind.upper()}  ({organ})", 18, KIND_COLOR[kind]).next_to(r, UP, buff=0.08)
            boxes.add(VGroup(r, lab))
        boxes.arrange(RIGHT, buff=0.3).move_to(DOWN * 0.35)
        return boxes

    def step2(self, wm):
        row = wm[0]
        names = ["Vigor", "Exploration", "Nautical Jargon", "Knotwork", "Bargaining"]
        chips = [self.chip(n) for n in names]
        self.place(chips, row)
        self.add(*chips)
        lt = self.long_term()
        self.play(FadeIn(lt), run_time=0.6)
        self.beat(0.12)
        cons = self.text("consolidate: kept for good, in the memory of its kind", 22, GOLD).move_to(DOWN * 2.0)
        self.play(chips[3].animate.move_to(lt[0][0]), chips[4].animate.move_to(lt[1][0]),
                  chips[1].animate.move_to(lt[2][0]), FadeIn(cons), run_time=1.1)
        self.beat(0.18)
        rej = self.text("reject: let it go, to make room before the queue decides", 22, LIGHT_PURPLE)
        rej.next_to(cons, DOWN, buff=0.25)
        self.play(chips[2].animate.set_opacity(0.15).shift(UP * 0.5), FadeIn(rej), run_time=0.9)
        self.play(FadeOut(chips[2]), run_time=0.3)

    def step3(self, wm):
        row = wm[0]
        vig = self.chip("Vigor")
        self.place([vig], row)
        lt = self.long_term()
        left = lambda box: box[0].get_center() + LEFT * 1.05    # noqa: E731 — two chips side by side
        right = lambda box: box[0].get_center() + RIGHT * 1.05  # noqa: E731
        knot, endu = self.chip("Knotwork").move_to(left(lt[0])), self.chip("Endurance").move_to(right(lt[0]))
        barg, expl = self.chip("Bargaining").move_to(lt[1][0]), self.chip("Exploration").move_to(lt[2][0])
        self.add(vig, lt, knot, endu, barg, expl)
        res = self.slots(3, "RESIDUAL", "set aside: the anamnesis keeps them a while", -2.3)
        held = [self.chip(n) for n in ("Hard Labor", "Archaeology", "Empathy")]   # front first
        self.place(held, res[0])
        self.play(FadeIn(res), FadeIn(VGroup(*held)), run_time=0.7)
        self.beat(0.12)
        arch = self.text("archive", 22, GOLD).next_to(lt[0], DOWN, buff=0.2).align_to(lt[0], LEFT)
        self.play(FadeIn(arch), Indicate(endu, color=GOLD), run_time=0.6)
        self.play(endu.animate.move_to(res[0][0]),
                  held[0].animate.move_to(res[0][1]), held[1].animate.move_to(res[0][2]),
                  held[2].animate.shift(RIGHT * 2.4).set_opacity(0.2), run_time=1.1)
        gone = self.text("the oldest is forgotten", 22, LIGHT_PURPLE).next_to(res[0], DOWN, buff=0.15)
        gone.align_to(res[0], RIGHT)
        self.play(FadeOut(held[2]), FadeIn(gone), run_time=0.6)
        keep = self.text("a long-term memory always keeps at least one", 20, GRAY60)
        keep.next_to(lt, DOWN, buff=0.15).align_to(lt, RIGHT)
        self.play(FadeIn(keep), Indicate(knot, color=GOLD), run_time=0.7)


# ── 2. one narration turn ─────────────────────────────────────────────────────

STEPS = [
    ("observe", "an observing mind\nchooses what to notice", "model"),
    ("keyword", "you click\na lit word", "you"),
    ("think", "a thinking mind\nreasons, picks a goal", "model"),
    ("act", "the acting mind\nsays how willing", "model"),
    ("dice", "pool and difficulty,\nsixes counted", "code"),
    ("outcome", "the world changes;\nthe acting mind tells it", "code"),
]
STEP_COLOR = {"model": GOLD, "you": LIGHT_PURPLE, "code": GRAY75}


class NarrationTurn(BrandScene):
    """Observe -> keyword -> think -> act -> dice -> outcome, and where noetic points go."""

    def construct(self):
        self.header("one narration turn", "who decides each step")
        nodes, notes = [], []
        for name, note, who in STEPS:
            nodes.append(self.node(name.upper(), STEP_COLOR[who], 22))
        w = max(n.width for n in nodes)
        for n in nodes:
            n[0].stretch_to_fit_width(w)
        col = VGroup(*nodes).arrange(DOWN, buff=0.32).to_edge(LEFT, buff=1.2).shift(DOWN * 0.55)
        for (name, note, who), n in zip(STEPS, nodes):
            notes.append(self.text(note.replace("\n", " "), 18, GRAY60).next_to(n, RIGHT, buff=0.4))
        arrows = [Arrow(a.get_bottom(), b.get_top(), buff=0.04, stroke_width=2, color=GRAY50,
                        max_tip_length_to_length_ratio=0.35) for a, b in zip(nodes, nodes[1:])]

        legend = VGroup(*[VGroup(Square(0.18, color=c, fill_color=c, fill_opacity=1), self.text(t, 18, c))
                          .arrange(RIGHT, buff=0.15)
                          for c, t in ((GOLD, "written by the model"), (LIGHT_PURPLE, "your choice"),
                                       (GRAY75, "decided by code"))]).arrange(DOWN, aligned_edge=LEFT, buff=0.18)
        legend.to_edge(RIGHT, buff=0.5).align_to(col, UP)

        # the noetic budget, on the right under the legend
        pips = VGroup(*[Circle(0.13, color=GOLD, fill_color=GOLD, fill_opacity=1) for _ in range(3)]).arrange(RIGHT, buff=0.18)
        budget_label = self.text("noetic points", 20, GOLD)
        budget = VGroup(budget_label, pips).arrange(RIGHT, buff=0.3)
        rule1 = self.text("one point per thought,\nand per extra look", 18, GRAY75, line_spacing=0.8)
        rule2 = self.text("refilled each new scene,\nbut not after a failed roll", 18, GRAY75, line_spacing=0.8)
        VGroup(budget, rule1, rule2).arrange(DOWN, aligned_edge=LEFT, buff=0.25).align_to(legend, LEFT).align_to(col, DOWN)
        side = VGroup(legend, budget, rule1, rule2)
        side.shift(LEFT * max(0, side.get_right()[0] - (config.frame_width / 2 - 0.4)))

        self.play(FadeIn(legend), run_time=0.6)
        for i, (n, t) in enumerate(zip(nodes, notes)):
            anims = [FadeIn(n, shift=DOWN * 0.1), FadeIn(t)]
            if i:
                anims.append(GrowArrow(arrows[i - 1]))
            self.play(*anims, run_time=0.6)
            if i == 0:
                self.play(FadeIn(budget), run_time=0.5)
            if i == 2:                                   # thinking spends a point
                self.play(Indicate(nodes[2], color=GOLD), pips[-1].animate.set_fill(BLACK_, opacity=1), run_time=0.7)
                self.play(FadeIn(rule1), run_time=0.5)
            self.beat(0.06, 0.4)
        self.play(FadeIn(rule2), run_time=0.5)
        self.fill_to_target()


# ── 3. dice and humors ────────────────────────────────────────────────────────

def die(scene, face, color=GRAY85):
    sq = RoundedRectangle(corner_radius=0.08, width=0.62, height=0.62, stroke_color=color, stroke_width=2,
                          fill_color=BLACK_, fill_opacity=1)
    return VGroup(sq, scene.text(str(face), 28, color).move_to(sq))


HUMOR_RULES = [
    ("blood", "+1 to a die", GOLD),
    ("yellow bile", "-1 to a die", MEDIUM_YELLOW),
    ("phlegm", "nothing, but clears the way", GRAY75),
    ("black bile", "nothing, and never leaves", PURPLE),
]


class DiceHumors(BrandScene):
    """The pool, the sixes, and a humor bending one die after the roll."""

    def construct(self):
        self.header("dice and humors")
        # the pool: one die per level of each mind in the chain
        parts = [("observing mind", 1), ("thinking mind", 1), ("acting mind", 2)]
        terms = VGroup()
        for i, (who, lv) in enumerate(parts):
            terms.add(VGroup(self.text(who, 20, GRAY75), self.text(f"level {lv}", 22, GOLD)).arrange(DOWN, buff=0.08))
            if i < len(parts) - 1:
                terms.add(self.text("+", 26, GRAY60))
        terms.add(self.text("=", 26, GRAY60), self.text("4 dice", 26, GOLD))
        terms.arrange(RIGHT, buff=0.3).shift(UP * 1.6)
        self.play(FadeIn(terms, lag_ratio=0.15), run_time=1.2)
        self.beat(0.08)

        faces = [3, 5, 1, 4]
        dice = VGroup(*[die(self, f) for f in faces]).arrange(RIGHT, buff=0.3).shift(UP * 0.55 + LEFT * 2.2)
        need = self.text("difficulty 1: one six needed", 22, LIGHT_PURPLE).next_to(dice, RIGHT, buff=0.6).shift(UP * 0.2)
        self.play(LaggedStart(*[FadeIn(d, scale=0.6) for d in dice], lag_ratio=0.15), run_time=0.9)
        self.play(FadeIn(need), run_time=0.5)
        fail = self.text("no six: failing", 20, PURPLE).next_to(need, DOWN, buff=0.15).align_to(need, LEFT)
        self.play(FadeIn(fail), run_time=0.4)
        self.beat(0.08)

        # four queues, the back of each the only humor that may be spent
        queues = VGroup()
        names = ["paunch", "hepar", "spleen", "pulmones"]
        backs = ["blood", "phlegm", "yellow bile", "black bile"]
        for qn, back in zip(names, backs):
            cells = VGroup(*[Square(0.34, stroke_color=GRAY35, stroke_width=1.2) for _ in range(7)]).arrange(RIGHT, buff=0.04)
            col = {"blood": GOLD, "phlegm": GRAY75, "yellow bile": MEDIUM_YELLOW, "black bile": PURPLE}[back]
            cells[-1].set_stroke(col, 2.5).set_fill(col, 0.5)
            label = self.text(qn, 18, GRAY60).next_to(cells, LEFT, buff=0.2)
            tag = self.text(back, 16, col).next_to(cells, RIGHT, buff=0.2)
            queues.add(VGroup(label, cells, tag))
        queues.arrange(DOWN, aligned_edge=LEFT, buff=0.14).to_edge(LEFT, buff=0.8).to_edge(DOWN, buff=0.85)
        for q in queues:
            q[1].align_to(queues[0][1], LEFT)
            q[0].next_to(q[1], LEFT, buff=0.2)
            q[2].next_to(q[1], RIGHT, buff=0.2)
        back_note = self.text("the four queues: only the back of each can be spent", 18, GRAY75)
        back_note.next_to(queues, UP, buff=0.2).align_to(queues, LEFT)
        self.play(FadeIn(queues, lag_ratio=0.1), FadeIn(back_note), run_time=1.0)
        self.beat(0.06)

        rules = VGroup(*[VGroup(self.text(n, 19, c), self.text(e, 17, GRAY75)).arrange(RIGHT, buff=0.25)
                         for n, e, c in HUMOR_RULES]).arrange(DOWN, aligned_edge=LEFT, buff=0.14)
        rules.to_edge(RIGHT, buff=1.1).align_to(queues, DOWN)
        self.play(FadeIn(rules, lag_ratio=0.2), run_time=1.0)
        self.beat(0.06)

        # spend the paunch's blood on the 5: it becomes a 6
        blood = queues[0][1][-1]
        token = blood.copy()
        self.play(token.animate.move_to(dice[1]).scale(0.6), run_time=0.8)
        six = die(self, 6, GOLD).move_to(dice[1])
        self.play(FadeOut(token), Transform(dice[1], six), run_time=0.5)
        blood.set_fill(BLACK_, 0).set_stroke(GRAY35, 1.2)
        ok = self.text("one six: success", 20, GOLD).move_to(fail)
        self.play(Transform(fail, ok), run_time=0.4)
        limit = self.text("humors per roll: the viscera's score", 20, GOLD).next_to(rules, UP, buff=0.3).align_to(rules, LEFT)
        self.play(FadeIn(limit), run_time=0.5)
        self.beat(0.06)
        dead = self.text("a queue of only black bile is spent; all four, and the body dies", 18, LIGHT_PURPLE)
        dead.to_edge(DOWN, buff=0.25)
        self.play(FadeIn(dead), run_time=0.5)
        self.fill_to_target()


# ── 4. attack against defence ─────────────────────────────────────────────────

class AttackDefence(BrandScene):
    """Two pools of dice, sixes compared; ties go to the defender."""

    def construct(self):
        self.header("a blow in a fight")
        att_src = VGroup(*[self.text(s, 19, GOLD) for s in ("the skill's dice", "+ the genitories")]).arrange(DOWN, aligned_edge=LEFT, buff=0.1)
        def_src = VGroup(*[self.text(s, 19, LIGHT_PURPLE) for s in ("the lower limbs", "+ guards and stances", "+ armour on the part struck")]).arrange(DOWN, aligned_edge=LEFT, buff=0.1)
        att = VGroup(self.text("ATTACK", 24, GOLD), att_src).arrange(DOWN, buff=0.25)
        dfn = VGroup(self.text("DEFENCE", 24, LIGHT_PURPLE), def_src).arrange(DOWN, buff=0.25)
        cols = VGroup(att, dfn).arrange(RIGHT, buff=2.5, aligned_edge=UP).shift(UP * 0.9)
        self.play(FadeIn(att, shift=RIGHT * 0.2), FadeIn(dfn, shift=LEFT * 0.2), run_time=0.9)
        self.beat(0.15)
        a = VGroup(*[die(self, f, GOLD if f == 6 else GRAY85) for f in (6, 2, 6, 4, 1)]).arrange(RIGHT, buff=0.2)
        d = VGroup(*[die(self, f, LIGHT_PURPLE if f == 6 else GRAY85) for f in (3, 6, 5)]).arrange(RIGHT, buff=0.2)
        a.next_to(att, DOWN, buff=0.6)
        d.next_to(dfn, DOWN, buff=0.6)
        self.play(LaggedStart(*[FadeIn(x, scale=0.6) for x in [*a, *d]], lag_ratio=0.1), run_time=1.0)
        verdict = self.text("two sixes against one: the blow lands and wounds", 22, GOLD).to_edge(DOWN, buff=1.3)
        tie = self.text("a tie goes to the defender", 20, GRAY75).next_to(verdict, DOWN, buff=0.2)
        self.play(FadeIn(verdict), run_time=0.5)
        self.beat(0.15)
        self.play(FadeIn(tie), run_time=0.5)
        self.fill_to_target()


# ── 7. how a place works ──────────────────────────────────────────────────────

# The town drawn in steps 1-2: a small piece of a city as CityLayout builds one (squares, a street, an
# alley and a gateway as one outdoor section; each building its own section of rooms, behind a door).
TOWN_OPEN = {"Gateway": (-5.6, -0.7), "Market Square": (-3.3, 0.6), "High Street": (-3.3, -2.0),
             "Cross Square": (-0.7, 0.6), "Back Alley": (-0.7, -2.0)}
TOWN_SHOP = {"Shop": (2.3, 0.6), "Workshop": (4.7, 0.6), "Bedroom": (4.7, -0.8)}
TOWN_HOUSE = {"Hall": (2.3, -2.4), "Pantry": (4.7, -2.4)}
TOWN_WAYS = [("Gateway", "Market Square"), ("Market Square", "High Street"), ("Market Square", "Cross Square"),
             ("High Street", "Back Alley"), ("Cross Square", "Back Alley"),
             ("Shop", "Workshop"), ("Workshop", "Bedroom"), ("Hall", "Pantry")]
TOWN_DOORS = [("Cross Square", "Shop"), ("Back Alley", "Hall")]
TOWN_PATH = ("Gateway", "Cross Square")
PERIODS = ["dawn", "morning", "noon", "afternoon", "evening", "night"]


class Locations(BrandScene):
    """How a place works (docs/manual/06-narration.md §1, §9; 05-travel.md §8; TradeGate, AskToBeLedVerb;
    the NPC schedules of SceneNpcPlacement), in five steps (params "step"):
      1 areas    areas grouped in sections; open ways, connectors (doors), a path
      2 moving   walk (difficulty 1), path (2), a door passed, going back (2) along the mental map
      3 time     six periods; each place's hour of arrival; waiting; night shuts doors; a new day
      4 people   a schedule across the six periods; trade only on premises; ask to be led; work anywhere
      5 video    what this video's debug settings changed, against a real run"""

    def chips(self, lit):
        names = ["AREAS", "MOVING", "TIME", "PEOPLE", "THIS VIDEO"]
        boxes = VGroup()
        for i, n in enumerate(names, start=1):
            on = i == lit
            t = self.text(n, 20, GOLD if on else GRAY50)
            r = RoundedRectangle(corner_radius=0.06, width=t.width + 0.4, height=t.height + 0.3,
                                 stroke_color=GOLD if on else GRAY35, stroke_width=2.5 if on else 1.2)
            boxes.add(VGroup(r.move_to(t), t))
        boxes.arrange(RIGHT, buff=0.35)
        return boxes.next_to(self.head, DOWN, buff=0.3)

    def construct(self):
        step = int(self.params.get("step", 1))
        self.head = self.header("how a place works", animate=(step == 1))
        c = self.chips(step)
        self.add(c) if step > 1 else self.play(FadeIn(c), run_time=0.6)
        getattr(self, ["", "step_areas", "step_moving", "step_time", "step_people", "step_video"][step])()
        self.fill_to_target()

    # ── the town ──
    def town(self, scale=1.0):
        pos = {**TOWN_OPEN, **TOWN_SHOP, **TOWN_HOUSE}
        nodes = {}
        for name, (x, y) in pos.items():
            color = GRAY85 if name in TOWN_OPEN else LIGHT_PURPLE
            nodes[name] = self.node(name, color, 20).move_to([x, y - 0.1, 0])
        ways = VGroup(*[self.edge(nodes[a], nodes[b], GRAY50) for a, b in TOWN_WAYS])
        doors = VGroup()
        for a, b in TOWN_DOORS:
            e = self.edge(nodes[a], nodes[b], GOLD, width=2.5)
            mid = e.point_from_proportion(0.5)
            door = Rectangle(width=0.16, height=0.42, stroke_color=GOLD, stroke_width=2,
                             fill_color=BLACK_, fill_opacity=1).move_to(mid)
            doors.add(VGroup(e, door))
        a, b = TOWN_PATH
        # over the top of the square it passes by, never through its box
        path = CubicBezier(nodes[a].get_top(), nodes[a].get_top() + UP * 1.5 + RIGHT * 0.4,
                           nodes[b].get_top() + UP * 1.5 + LEFT * 0.6, nodes[b].get_top())
        path.set_stroke(LIGHT_PURPLE, 2.5)
        path = DashedVMobject(path, num_dashes=28)
        def section(names, label, color):
            g = VGroup(*[nodes[n] for n in names])
            box = SurroundingRectangle(g, buff=0.28, corner_radius=0.12, stroke_color=color, stroke_width=1.4)
            lab = self.text(label, 16, color).next_to(box, DOWN, buff=0.08).align_to(box, LEFT)
            return VGroup(box, lab)
        sections = VGroup(section(TOWN_OPEN, "open ground: one section", GRAY60),
                          section(TOWN_SHOP, "the cobbler's: a section", LIGHT_PURPLE),
                          section(TOWN_HOUSE, "a house: a section", LIGHT_PURPLE))
        return nodes, ways, doors, path, sections

    def step_areas(self):
        nodes, ways, doors, path, sections = self.town()
        self.wait_until(0.10)
        self.play(LaggedStart(*[FadeIn(n, scale=0.9) for n in nodes.values()], lag_ratio=0.08), run_time=1.8)
        self.wait_until(0.30)
        self.play(LaggedStart(*[Create(s[0]) for s in sections], lag_ratio=0.25),
                  LaggedStart(*[FadeIn(s[1]) for s in sections], lag_ratio=0.25), run_time=1.4)
        self.wait_until(0.50)
        self.play(LaggedStart(*[Create(w) for w in ways], lag_ratio=0.1), run_time=1.2)
        self.wait_until(0.62)
        self.play(LaggedStart(*[FadeIn(d, scale=1.4) for d in doors], lag_ratio=0.3), run_time=1.0)
        tag = self.text("doors: passed, not walked", 18, GOLD).next_to(doors[0], UP, buff=0.55).align_to(doors[0], LEFT)
        self.play(FadeIn(tag), run_time=0.4)
        self.wait_until(0.82)
        self.play(Create(path), run_time=1.2)
        ptag = self.text("a path: two or three areas at once", 18, LIGHT_PURPLE).next_to(path, UP, buff=0.1)
        self.play(FadeIn(ptag), run_time=0.4)

    def step_moving(self):
        nodes, ways, doors, path, sections = self.town()
        self.add(*nodes.values(), ways, doors, path, sections)
        token = Dot(radius=0.13, color=GOLD).move_to(nodes["Gateway"].get_center() + UP * 0.32)
        known = set()

        def visit(name):
            known.add(name)
            n = nodes[name]
            return n[0].animate.set_fill(GRAY20, opacity=1)

        caption = None

        def say(text, color=GOLD):
            nonlocal caption
            t = self.text(text, 22, color).to_edge(DOWN, buff=0.18)
            anims = [FadeIn(t, shift=UP * 0.1)] + ([FadeOut(caption)] if caption else [])
            caption = t
            return anims

        self.play(FadeIn(token, scale=2), visit("Gateway"), run_time=0.6)
        self.wait_until(0.14)
        self.play(*say("follow a path: difficulty 2", LIGHT_PURPLE), run_time=0.4)
        a_, b_ = nodes["Gateway"], nodes["Cross Square"]
        route = CubicBezier(token.get_center(), a_.get_top() + UP * 1.5 + RIGHT * 0.4,
                            b_.get_top() + UP * 1.5 + LEFT * 0.6, b_.get_center() + UP * 0.32)
        self.play(MoveAlongPath(token, route), visit("Cross Square"), run_time=1.2)
        self.wait_until(0.30)
        self.play(*say("walk to a neighbour: difficulty 1"), run_time=0.4)
        self.play(token.animate.move_to(nodes["Back Alley"].get_center() + UP * 0.32), visit("Back Alley"), run_time=0.9)
        self.wait_until(0.42)
        self.play(*say("a door: opened, or unlocked, then passed"), run_time=0.4)
        self.play(Indicate(doors[1][1], color=GOLD, scale_factor=1.6), run_time=0.6)
        self.play(token.animate.move_to(nodes["Hall"].get_center() + UP * 0.32), visit("Hall"), run_time=0.9)
        self.wait_until(0.62)
        glow = VGroup(*[SurroundingRectangle(nodes[n], buff=0.06, corner_radius=0.08, stroke_color=GOLD, stroke_width=2)
                        for n in sorted(known)])
        self.play(*say("known ways: the place's mental map"), Create(glow), run_time=0.8)
        self.wait_until(0.76)
        self.play(*say("go back: difficulty 2, to any area already known", GOLD), run_time=0.4)
        self.play(token.animate.move_to(nodes["Gateway"].get_center() + UP * 0.32), run_time=1.0)
        self.play(Flash(token, color=GOLD), run_time=0.5)

    def step_time(self):
        center = DOWN * 1.0 + LEFT * 3.9
        r = 1.9
        wheel = VGroup()
        labels = VGroup()
        for i, name in enumerate(PERIODS):
            a0 = PI / 2 - i * TAU / 6
            seg = AnnularSector(inner_radius=r * 0.62, outer_radius=r, angle=-TAU / 6 + 0.04, start_angle=a0 - 0.02,
                                fill_color=GRAY35 if name == "night" else GRAY20, fill_opacity=1,
                                stroke_color=GRAY50, stroke_width=1).move_arc_center_to(center)
            mid = a0 - TAU / 12
            lab = self.text(name, 20, GRAY85).move_to(center + (r + 0.55) * np.array([np.cos(mid), np.sin(mid), 0]))
            wheel.add(seg)
            labels.add(lab)
        self.wait_until(0.04)
        self.play(LaggedStart(*[FadeIn(s) for s in wheel], lag_ratio=0.12), LaggedStart(*[FadeIn(l) for l in labels], lag_ratio=0.12),
                  run_time=1.6)
        notes = VGroup()

        def note(text, color=GRAY85):
            t = self.text(text, 21, color)
            notes.add(t)
            notes.arrange(DOWN, aligned_edge=LEFT, buff=0.36)
            notes.move_to(DOWN * 1.0).align_to(LEFT * 0.2, LEFT)
            return t

        # the hand, at this place's hour of arrival
        def hand_at(i):
            mid = PI / 2 - i * TAU / 6 - TAU / 12
            return Line(center, center + r * 0.58 * np.array([np.cos(mid), np.sin(mid), 0]), color=GOLD, stroke_width=5)
        hand = hand_at(1)
        self.wait_until(0.20)
        t1 = note("each place opens at its own hour", GOLD)
        self.play(Create(hand), FadeIn(t1), wheel[1].animate.set_fill(DARK_YELLOW, opacity=1), run_time=0.9)
        self.wait_until(0.40)
        t2 = note("sit and wait, on a seat: choose the hour")
        self.play(FadeIn(t2), run_time=0.4)
        self.play(Transform(hand, hand_at(4)), wheel[1].animate.set_fill(GRAY20, opacity=1),
                  wheel[4].animate.set_fill(DARK_YELLOW, opacity=1), run_time=1.2)
        self.wait_until(0.58)
        t3 = note("a failed roll can waste one", LIGHT_PURPLE)
        self.play(FadeIn(t3), run_time=0.4)
        self.wait_until(0.70)
        t4 = note("night into dawn: a new day")
        self.play(FadeIn(t4), Transform(hand, hand_at(5)), wheel[4].animate.set_fill(GRAY20, opacity=1),
                  wheel[5].animate.set_fill(DARK_PURPLE, opacity=1), run_time=1.0)
        self.wait_until(0.86)
        t5 = note("at night, a building's door is shut", GOLD)
        door = Rectangle(width=0.22, height=0.5, stroke_color=GOLD, stroke_width=2).next_to(t5, LEFT, buff=0.25)
        bar = Line(door.get_left(), door.get_right(), color=GOLD, stroke_width=3)
        self.play(FadeIn(t5), Create(door), Create(bar), run_time=0.6)

    def step_people(self):
        # one person's day: where a schedule puts them, period by period (an example, as a schedule reads)
        day = ["bed", "shop", "shop", "shop", "tavern", "bed"]
        cols = VGroup()
        for i, (p, where) in enumerate(zip(PERIODS, day)):
            head = self.text(p, 18, GRAY60)
            on = where == "shop"
            cell = RoundedRectangle(corner_radius=0.06, width=1.55, height=0.62,
                                    stroke_color=GOLD if on else GRAY35, stroke_width=2 if on else 1.2,
                                    fill_color=DARK_YELLOW if on else BLACK_, fill_opacity=0.35 if on else 1)
            lab = self.text(where, 20, GOLD if on else GRAY85).move_to(cell)
            cols.add(VGroup(head, VGroup(cell, lab)).arrange(DOWN, buff=0.15))
        cols.arrange(RIGHT, buff=0.12).move_to(UP * 0.3)
        who = self.text("the cobbler's day, for example", 20, LIGHT_PURPLE).next_to(cols, UP, buff=0.3).align_to(cols, LEFT)
        self.wait_until(0.05)
        self.play(FadeIn(who), LaggedStart(*[FadeIn(c, shift=UP * 0.1) for c in cols], lag_ratio=0.12), run_time=1.6)
        rules = [("trades only in his shop", GOLD, 0.40),
                 ("met elsewhere: ask to be led, to the shop, at its hour", GRAY85, 0.58),
                 ("work: asked anywhere, once introduced", LIGHT_PURPLE, 0.78)]
        shown = VGroup()
        for text, color, at in rules:
            self.wait_until(at)
            t = self.text(text, 22, color)
            shown.add(t)
            shown.arrange(DOWN, aligned_edge=LEFT, buff=0.28).next_to(cols, DOWN, buff=0.55).align_to(cols, LEFT)
            anims = [FadeIn(t, shift=RIGHT * 0.15)]
            if at == 0.40:
                anims.append(Indicate(VGroup(*[c[1] for c, w in zip(cols, day) if w == "shop"]), color=GOLD, scale_factor=1.08))
            self.play(*anims, run_time=0.7)

    def step_video(self):
        rows = [("starts inside the shop", "arrives on the open ground, at the place's hour"),
                ("the cobbler stays put all day", "he keeps his own day"),
                ("our minds notice only him", "they notice anyone, and anything"),
                ("already acquainted", "you meet people first"),
                ("humors full of pleasure", "what your organs make"),
                ("two wolves, ordered ahead", "the road decides")]
        hv = self.text("in this video", 24, LIGHT_PURPLE)
        hr = self.text("in a real run", 24, GOLD)
        left = VGroup(hv, *[self.text(a, 20, GRAY60) for a, _ in rows]).arrange(DOWN, aligned_edge=LEFT, buff=0.26)
        right = VGroup(hr, *[self.text(b, 20, GRAY85) for _, b in rows]).arrange(DOWN, aligned_edge=LEFT, buff=0.26)
        table = VGroup(left, right).arrange(RIGHT, buff=1.0, aligned_edge=UP).move_to(DOWN * 0.75)
        table.scale_to_fit_width(min(table.width, config.frame_width - 1.2))
        self.wait_until(0.06)
        self.play(FadeIn(hv), FadeIn(hr), run_time=0.5)
        n = len(rows)
        for i in range(n):
            self.wait_until(0.14 + 0.62 * i / n)
            self.play(FadeIn(left[i + 1], shift=RIGHT * 0.1), FadeIn(right[i + 1], shift=LEFT * 0.1), run_time=0.5)
