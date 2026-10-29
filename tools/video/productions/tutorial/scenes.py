"""Animations for the tutorial ("the first hour").

Every rule drawn here is the manual's, which is derived from the code (docs/manual, synced at HEAD):
  Organs        - the derived measures, by source (docs/manual/01-anatomy.md §6; src/game/narrative/body/stats/)
  NarrationTurn - the three steps of an act and the noetic budget (06-narration.md §3, §5)
  DiceHumors    - the pool, the difficulty, the transmuting virtues and the viscera limit
                  (06-narration.md §5, 03-humors.md §4-5)
  AttackDefence - the exchange in a fight (08-fighting.md §6)
"""

from vidkit.manimkit import *  # noqa: F401,F403


# ── 1. organs -> systems ──────────────────────────────────────────────────────

# (system, [(source, measures)]) in the order the creation footage takes them
SYSTEMS = [
    ("thinking and memory", [
        ("encephalon", "noetic points, working memory"),
        ("anamnesis", "residual memory, places remembered"),
        ("cerebrum", "semantic memory"),
        ("cerebellum", "procedural memory, learning in a fight"),
        ("hippocampus", "sensory memory"),
        ("pineal gland", "experience per level, doubt"),
    ]),
    ("the senses", [
        ("eyes", "reach of a ranged blow"),
        ("ears", "initiative in a fight"),
        ("nose", "avoiding danger, reading food"),
    ]),
    ("speech", [
        ("tongue", "replies offered"),
        ("visage", "dice in conversation"),
        ("teeth", "hard food"),
    ]),
    ("the humors", [
        ("paunch, hepar, spleen, pulmones", "the four queues"),
        ("viscera", "humors per roll, healing"),
    ]),
    ("life", [
        ("heart", "lifespan, companions, youth"),
        ("trunk", "health"),
        ("genitories", "dice on every blow"),
    ]),
    ("the body in a fight", [
        ("backbone", "movement points, carrying"),
        ("hands", "which tools, how well"),
        ("legs", "distance per movement point"),
        ("lower limbs", "journey length, defence"),
        ("feet", "footing, flight"),
    ]),
]


class Organs(BrandScene):
    """Six panels, one per system, each listing the organs that feed it and what they set."""

    def construct(self):
        self.header("one body, every measure", "each organ or region feeds the systems it is named for")
        panels = []
        for name, rows in SYSTEMS:
            lines = VGroup()
            for src, what in rows:
                a = self.text(src, 19, GOLD)
                b = self.text(what, 17, GRAY75) if what else VMobject()
                row = VGroup(a, b).arrange(DOWN, aligned_edge=LEFT, buff=0.06) if what else VGroup(a)
                lines.add(row)
            lines.arrange(DOWN, aligned_edge=LEFT, buff=0.14)
            box = self.frame_box(lines, GRAY50, buff=0.22, label=name.upper())
            panels.append(VGroup(box, lines))
        # equal-width columns: three across, two down
        w = max(p.width for p in panels)
        for p in panels:
            p[0][0].stretch_to_fit_width(w).align_to(p[1], LEFT).shift(LEFT * 0.22)
            p[0][1:].align_to(p[0][0], LEFT).shift(RIGHT * 0.3)
        top = VGroup(*panels[:3]).arrange(RIGHT, buff=0.3, aligned_edge=UP)
        bottom = VGroup(*panels[3:]).arrange(RIGHT, buff=0.3, aligned_edge=UP)
        grid = VGroup(top, bottom).arrange(DOWN, buff=0.3)
        grid.scale_to_fit_width(min(grid.width, config.frame_width - 0.8))
        if grid.height > config.frame_height - 2.3:
            grid.scale_to_fit_height(config.frame_height - 2.3)
        grid.to_edge(DOWN, buff=0.3)
        for p in panels:
            self.play(Create(p[0][0]), FadeIn(p[0][1:]), FadeIn(p[1], shift=UP * 0.1, lag_ratio=0.1), run_time=0.8)
            self.beat(0.1, 0.5)
        self.fill_to_target()


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
