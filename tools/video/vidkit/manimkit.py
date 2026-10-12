"""The brand scene for manim: what every animation in a Proscribed Palimpsest video is built on.

    from vidkit.manimkit import *

    class MyScene(BrandScene):
        def construct(self):
            self.header("THE SETTLED COUNTRY")
            chart = self.hbar_chart(["city", "farm"], [3, 12])
            self.play(*self.grow(chart))
            self.fill_to_target()

What it gives you:

* the palette (`GOLD`, `LIGHT_PURPLE`, `GRAY50`… from vidkit.palette) and the terminal's font;
* `text`, `header` and `frame_box` in the game's own idiom — spaced capitals between rules, thin grey
  frames, yellow for what matters and purple for what threatens;
* `hbar_chart` and `graph` — data drawn plainly, labels on the marks, no legend to decode;
* pacing: `self.target` is how long the voice-over for this segment runs (the build passes it in), and
  `fill_to_target()` waits out whatever is left, so an animation never ends before its narration;
* `self.params`: whatever the storyboard put in the segment's "params", e.g. a data file.

Colours are checked: a mobject coloured off the yellow/purple/grey identity fails the render.
The dither is NOT applied here — the build runs the game's dither over the whole finished video.
"""

from __future__ import annotations

import json
import os

from manim import *  # noqa: F401,F403 — scenes import manim through here
from manim import config

from . import palette as P
from .palette import (BLACK as BLACK_, BRIGHT_PURPLE, BRIGHT_YELLOW, BRONZE, DARK_PURPLE, DARK_YELLOW,  # noqa: F401
                      DARK_YELLOW_GREY, GOLD, GRAY20, GRAY35, GRAY50, GRAY60, GRAY75, GRAY85, LIGHT_PURPLE,
                      LIGHT_PURPLE_GRAY, LIGHT_YELLOW, MEDIUM_YELLOW, PURPLE, SERIES)

FONT = P.FONT
config.background_color = P.BACKGROUND


class BrandScene(Scene):
    """A scene in the game's look, paced to its narration."""

    def setup(self):
        self.camera.background_color = P.BACKGROUND
        self.target = float(os.environ.get("VID_TARGET_SECONDS", "0") or 0)
        self.params = json.loads(os.environ.get("VID_PARAMS", "{}") or "{}")
        self._seen: list[Mobject] = []

    def add(self, *mobjects):
        self._seen.extend(mobjects)
        return super().add(*mobjects)

    def tear_down(self):
        """Fails the render if anything shown was coloured off the yellow / purple / grey identity."""
        bad = set()
        for top in self._seen:
            for m in top.get_family():
                if not isinstance(m, VMobject) or isinstance(m, BackgroundRectangle):
                    continue
                for c, opacity in ((m.get_stroke_color(), m.get_stroke_opacity()), (m.get_fill_color(), m.get_fill_opacity())):
                    if opacity and opacity > 0.05:
                        hexc = ManimColor(c).to_hex()[:7]
                        if P.family(hexc) == "off-brand":
                            bad.add(hexc)
        if bad:
            raise ValueError(f"{type(self).__name__}: off-brand colours {sorted(bad)} — use vidkit.palette (yellows, purples, greys)")

    # ── pacing ──────────────────────────────────────────────────────────────

    @property
    def elapsed(self) -> float:
        return self.renderer.time

    def fill_to_target(self, tail: float = 0.6):
        """Waits until the narration for this segment has had time to finish."""
        remaining = self.target + tail - self.elapsed
        if remaining > 0.05:
            self.wait(remaining)

    def beat(self, fraction: float, minimum: float = 0.4):
        """Waits a share of the narration's length — for spacing the steps of a build-up."""
        self.wait(max(minimum, self.target * fraction))

    # ── type ────────────────────────────────────────────────────────────────

    @staticmethod
    def text(s: str, size: float = 28, color: str = P.TEXT, weight: str = NORMAL, **kw) -> Text:
        P.check(color)
        return Text(s, font=FONT, font_size=size, color=color, weight=weight, **kw)

    @staticmethod
    def spaced(s: str) -> str:
        """The game's title style: V O L U M E."""
        return " ".join(s.upper())

    def header(self, s: str, sub: str | None = None, animate: bool = True) -> VGroup:
        """A title between two dotted rules, top of frame, as the main menu sets the game's name."""
        title = self.text(self.spaced(s), 34, GOLD)
        rule_w = max(title.width + 1.2, 6)
        top = self.rule(rule_w).next_to(title, UP, buff=0.18)
        bot = self.rule(rule_w).next_to(title, DOWN, buff=0.18)
        g = VGroup(top, title, bot)
        if g.width > config.frame_width - 1.0:          # a long title: shrink to fit, never run off the frame
            g.scale_to_fit_width(config.frame_width - 1.0)
        if sub:
            g.add(self.text(sub, 22, GRAY60).next_to(bot, DOWN, buff=0.22))
        g.to_edge(UP, buff=0.45)
        if animate:
            self.play(FadeIn(top, shift=RIGHT * 0.3), FadeIn(bot, shift=LEFT * 0.3), Write(title), run_time=1.0)
            if sub:
                self.play(FadeIn(g[-1]), run_time=0.5)
        else:
            self.add(g)
        return g

    def rule(self, width: float, color: str = GRAY35) -> VMobject:
        dashes = int(width / 0.28)
        return DashedLine(LEFT * width / 2, RIGHT * width / 2, dash_length=0.12, dashed_ratio=0.5,
                          color=color, stroke_width=2).set_length(width) if dashes > 0 else Line()

    def frame_box(self, mob: Mobject, color: str = GRAY50, buff: float = 0.25, label: str | None = None) -> VGroup:
        """A thin terminal frame around `mob`, with an optional [ LABEL ] cut into its top edge."""
        P.check(color)
        box = Rectangle(width=mob.width + 2 * buff, height=mob.height + 2 * buff, color=color, stroke_width=1.5)
        box.move_to(mob)
        g = VGroup(box)
        if label:
            tag = self.text(f"[{label}]", 18, color)
            tag.move_to(box.get_top()).align_to(box, LEFT).shift(RIGHT * 0.3)
            back = BackgroundRectangle(tag, color=P.BACKGROUND, fill_opacity=1, buff=0.06)
            g.add(back, tag)
        return g

    # ── charts ──────────────────────────────────────────────────────────────

    def hbar_chart(self, labels: list[str], values: list[float], colors: list[str] | None = None,
                   width: float = 7.5, bar_height: float = 0.42, gap: float = 0.22, fmt: str = "{:g}",
                   label_size: float = 22, max_value: float | None = None) -> VGroup:
        """Horizontal bars, labelled on the left and valued on the right. Built at zero length;
        animate with `self.grow(chart)`."""
        colors = colors or [SERIES[i % len(SERIES)] for i in range(len(values))]
        P.check(*colors)
        top = max_value or max(values) or 1
        rows = VGroup()
        for i, (lab, val, col) in enumerate(zip(labels, values, colors)):
            name = self.text(lab, label_size, GRAY85)
            full = max(0.02, width * val / top)
            bar = Rectangle(width=full, height=bar_height, fill_color=col, fill_opacity=1, stroke_width=0)
            value = self.text(fmt.format(val), label_size, col)
            row = VGroup(name, bar, value)
            row.bar_full_width = full
            rows.add(row)
        name_w = max(r[0].width for r in rows)
        for i, row in enumerate(rows):
            y = -i * (bar_height + gap)
            row[0].move_to([0, y, 0]).align_to([-name_w - 0.3, 0, 0], LEFT)
            row[1].move_to([0, y, 0]).align_to([0, 0, 0], LEFT)
            row[2].next_to(row[1], RIGHT, buff=0.18)
        rows.center()
        return rows

    def grow(self, chart: VGroup, run_time: float = 1.2) -> list[Animation]:
        """Animations that grow each bar from its left end and fade its labels in, staggered."""
        anims = []
        for i, row in enumerate(chart):
            anims.append(Succession(Wait(i * 0.08), AnimationGroup(
                GrowFromEdge(row[1], LEFT), FadeIn(row[0]), FadeIn(row[2], shift=LEFT * 0.2), run_time=run_time)))
        return anims

    # ── graphs ──────────────────────────────────────────────────────────────

    def node(self, label: str, color: str = GRAY85, size: float = 20, fill: str | None = None) -> VGroup:
        """A labelled node in a terminal box."""
        P.check(color)
        t = self.text(label, size, color)
        box = RoundedRectangle(corner_radius=0.06, width=t.width + 0.3, height=t.height + 0.22,
                               stroke_color=color, stroke_width=1.6,
                               fill_color=fill or P.BACKGROUND, fill_opacity=1)
        box.move_to(t)
        return VGroup(box, t)

    @staticmethod
    def untangle(mobs: list[Mobject], margin: float = 0.18, bounds: tuple[float, float, float, float] | None = None,
                 iterations: int = 300) -> None:
        """Pushes labelled nodes apart until no two boxes overlap (a graph layout places points, but a
        node is a box with a name in it). Each overlapping pair moves apart along its shallower axis.
        `bounds` = (left, right, bottom, top) keeps them in a region."""
        for _ in range(iterations):
            moved = False
            for i in range(len(mobs)):
                for j in range(i + 1, len(mobs)):
                    a, b = mobs[i], mobs[j]
                    dx = b.get_x() - a.get_x()
                    dy = b.get_y() - a.get_y()
                    ox = (a.width + b.width) / 2 + margin - abs(dx)
                    oy = (a.height + b.height) / 2 + margin - abs(dy)
                    if ox > 0 and oy > 0:
                        moved = True
                        if ox < oy * 2.2:      # boxes are wide: prefer sliding sideways only when it is cheap
                            s = (ox / 2 + 0.01) * (1 if dx >= 0 else -1)
                            a.shift(LEFT * s); b.shift(RIGHT * s)
                        else:
                            s = (oy / 2 + 0.01) * (1 if dy >= 0 else -1)
                            a.shift(DOWN * s); b.shift(UP * s)
            if bounds:
                l, r, bt, t = bounds
                for m in mobs:
                    m.set_x(min(max(m.get_x(), l + m.width / 2), r - m.width / 2))
                    m.set_y(min(max(m.get_y(), bt + m.height / 2), t - m.height / 2))
            if not moved:
                break

    def edge(self, a: Mobject, b: Mobject, color: str = GRAY50, dashed: bool = False, width: float = 2) -> VMobject:
        P.check(color)
        start, end = a.get_center(), b.get_center()
        cls = DashedLine if dashed else Line
        line = cls(start, end, color=color, stroke_width=width)
        # stop at the boxes' borders
        line.put_start_and_end_on(_border_point(a, end), _border_point(b, start))
        return line.set_z_index(-1)          # under the (opaque) boxes, so a line never crosses a label


def _border_point(mob: Mobject, toward: np.ndarray) -> np.ndarray:
    c = mob.get_center()
    d = toward - c
    if np.allclose(d, 0):
        return c
    hw, hh = mob.width / 2, mob.height / 2
    sx = hw / abs(d[0]) if abs(d[0]) > 1e-6 else np.inf
    sy = hh / abs(d[1]) if abs(d[1]) > 1e-6 else np.inf
    return c + d * min(sx, sy, 1.0)


# ── generic cards the storyboard can ask for without a scene file ─────────────────

class TitleCard(BrandScene):
    """params: title, subtitle, kicker (small text above)."""

    def construct(self):
        p = self.params
        kicker = self.text(self.spaced(p.get("kicker", "proscribed palimpsest")), 22, GRAY50)
        title = self.text(p.get("title", ""), 64, GOLD)
        width = max(title.width, kicker.width) + 1.5
        top, bot = self.rule(width, GRAY35), self.rule(width, GRAY35)
        g = VGroup(kicker, top, title, bot).arrange(DOWN, buff=0.35)
        if p.get("subtitle"):
            g.add(self.text(p["subtitle"], 28, LIGHT_PURPLE).next_to(bot, DOWN, buff=0.45))
        g.center()
        self.play(FadeIn(kicker), Create(top), Create(bot), run_time=0.9)
        self.play(Write(title), run_time=1.2)
        if p.get("subtitle"):
            self.play(FadeIn(g[-1], shift=UP * 0.15), run_time=0.7)
        self.fill_to_target(tail=1.0)
        self.play(FadeOut(g), run_time=0.6)


class TextCard(BrandScene):
    """params: heading, lines (list), color for the lines."""

    def construct(self):
        p = self.params
        items = VGroup()
        if p.get("heading"):
            items.add(self.text(self.spaced(p["heading"]), 30, GOLD))
        for line in p.get("lines", []):
            items.add(self.text(line, 30, p.get("color", GRAY85)))
        items.arrange(DOWN, buff=0.45).center()
        for m in items:
            self.play(FadeIn(m, shift=UP * 0.12), run_time=0.6)
            self.wait(0.25)
        self.fill_to_target(tail=1.0)
        self.play(FadeOut(items), run_time=0.6)
