"""Animations for "How cities work".

Every number and rule drawn here comes from the code, not from a reading of it:
  Sprawl  - SettlementSprawl.Build, run on a hex grid by a port of its rules (src/game/history/SettlementSprawl.cs)
  Plan    - a real city's street graph, written by `--scene-export city --ids 2701` (CityLayout.Build)
  Sizes   - 150 generated cities, by size (build/data/city_*.json, made by the storyboard's "prepare")
  Crews   - SettledSceneFactory.BuildTown's rosters, and who those 60 plain cities actually house
  Styles  - CityLayout.Styles and BuildingDescriptions.MaterialsOf
"""

import collections
import json
import random

from vidkit.manimkit import *  # noqa: F401,F403


def _load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


# ── 1. where cities stand ─────────────────────────────────────────────────────

HEX_R = 0.29


def _axial_to_xy(q, r):
    return np.array([HEX_R * 1.5 * q, HEX_R * np.sqrt(3) * (r + q / 2), 0])


def _neighbours(q, r):
    return [(q + 1, r), (q - 1, r), (q, r + 1), (q, r - 1), (q + 1, r - 1), (q - 1, r + 1)]


class Sprawl(BrandScene):
    """History founds a place; the country grows around it, one rule at a time."""

    COLS, ROWS = 25, 10

    def construct(self):
        self.header("the settled country", "where a city comes from")

        cells = {}
        for q in range(self.COLS):
            for r in range(-q // 2, self.ROWS - q // 2):
                cells[(q, r)] = _axial_to_xy(q, r)
        centre = np.mean(list(cells.values()), axis=0)
        offset = np.array([0, -0.95, 0]) - centre

        rng = random.Random(1347)
        # wild ground (forest: left wild) and barren ground: no sprawl may take them
        wild = {c for c in cells if rng.random() < 0.13}
        border_q = 14                                    # the realm border runs between columns 13 and 14
        citadel, castle = (7, 1), (19, -6)               # history's two places: urban west, rural east
        for place in (citadel, castle):                  # history chose these cells; nothing wild on or by them
            wild.discard(place)
            wild.difference_update(_neighbours(*place))

        hexes = {}
        for c, xy in cells.items():
            h = RegularPolygon(6, radius=HEX_R * 0.96, stroke_color=GRAY20, stroke_width=1.2,
                               fill_color=BLACK_, fill_opacity=1).rotate(PI / 6 * 0).move_to(xy + offset)
            hexes[c] = h
        grid = VGroup(*hexes.values())
        wild_marks = VGroup(*[self.text("^", 16, GRAY35).move_to(hexes[c]) for c in wild])
        self.play(FadeIn(grid, lag_ratio=0.002), run_time=1.2)
        self.play(FadeIn(wild_marks), run_time=0.5)

        # the border
        pts = []
        for r in range(-border_q // 2 - 1, self.ROWS - border_q // 2 + 1):
            a = _axial_to_xy(border_q - 0.5, r + 0.25) + offset
            pts.append(a)
        border = DashedVMobject(VMobject().set_points_smoothly(pts), num_dashes=40).set_stroke(BRIGHT_PURPLE, 3)
        border_label = self.text("a realm border", 20, LIGHT_PURPLE).next_to(border, UP, buff=0.06)
        self.play(Create(border), FadeIn(border_label), run_time=0.9)

        def paint(cs, color, opacity=1.0):
            return [hexes[c].animate.set_fill(color, opacity=opacity).set_stroke(GRAY35, 1.2) for c in cs]

        def label(c, s, color, direction=UP):
            t = self.text(s, 20, color)
            back = BackgroundRectangle(t, color=BLACK_, fill_opacity=0.85, buff=0.06)
            return VGroup(back, t).next_to(hexes[c], direction, buff=0.12)

        taken = set(wild)
        realm = lambda c: 0 if c[0] < border_q else 1   # noqa: E731

        def free_around(c, rngx):
            out = [n for n in _neighbours(*c) if n in cells and n not in taken and realm(n) == realm(c)]
            rngx.shuffle(out)
            return out

        def branches(rngx):
            return rngx.randint(1, 3)

        def rural(start, rngx):
            """SettlementSprawl.Rural: two or three levels, each node putting out one to three."""
            levels = []
            frontier = [start]
            for _ in range(rngx.randint(2, 3)):
                nxt = []
                for node in frontier:
                    for v in free_around(node, rngx)[: branches(rngx)]:
                        taken.add(v)
                        nxt.append(v)
                if not nxt:
                    break
                levels.append(nxt)
                frontier = nxt
            return levels

        # history: an urban place west of the border, a rural one east of it
        taken.update([citadel, castle])
        cit_hex = hexes[citadel]
        self.play(*paint([citadel], GOLD), run_time=0.6)
        cit_label = label(citadel, "a citadel - urban", GOLD)
        self.play(FadeIn(cit_label), Flash(cit_hex, color=GOLD, line_length=0.18), run_time=0.8)
        self.beat(0.06)

        # 1-3 city cells on city ground
        r1 = random.Random(5)
        cities = free_around(citadel, r1)[:3]
        taken.update(cities)
        self.play(*paint(cities, LIGHT_YELLOW), run_time=0.7)
        city_label = label(cities[0], "one to three city cells", LIGHT_YELLOW, DOWN)
        self.play(FadeOut(cit_label), FadeIn(city_label), run_time=0.6)
        self.beat(0.06)

        # farmland: each city sprawls as a rural place, ring after ring
        rings = collections.defaultdict(list)
        for c in cities:
            for depth, level in enumerate(rural(c, r1)):
                rings[depth].extend(level)
        farm_label = None
        for depth in sorted(rings):
            self.play(*paint(rings[depth], DARK_YELLOW), run_time=0.55)
            if farm_label is None:
                farm_label = label(rings[0][0], "farmland, ring after ring", MEDIUM_YELLOW, UP)
                self.play(FadeOut(city_label), FadeIn(farm_label), run_time=0.5)
        self.beat(0.05)

        # a rural place spreads farmland directly, on its own side of the border
        self.play(*paint([castle], GOLD), run_time=0.5)
        castle_label = label(castle, "a castle - rural", GOLD, DOWN)
        self.play(FadeIn(castle_label), run_time=0.4)
        r2 = random.Random(9)
        east = rural(castle, r2)
        for level in east:
            self.play(*paint(level, DARK_YELLOW), run_time=0.45)
        self.beat(0.04)

        # one cell of farmland in three becomes a settlement or a place where stock is kept
        farmland = [c for lvl in rings.values() for c in lvl] + [c for lvl in east for c in lvl]
        r3 = random.Random(3)
        settled, stock = [], []
        for c in farmland:
            if r3.random() >= 0.33:
                continue
            (stock if r3.random() < 0.5 else settled).append(c)
        self.play(*paint(settled, LIGHT_PURPLE), *paint(stock, PURPLE), run_time=0.8)
        third = label(settled[0] if settled else farmland[0], "one in three: a village, or stock", LIGHT_PURPLE, UP)
        self.play(FadeOut(farm_label), FadeIn(third), run_time=0.5)

        # the border held
        self.play(Indicate(border, color=BRIGHT_PURPLE, scale_factor=1.0), run_time=0.9)
        self.fill_to_target(tail=0.8)


# ── 2. the plan of one city ───────────────────────────────────────────────────

class Plan(BrandScene):
    """The street graph of the city the footage walks through, built in the order CityLayout lays it."""

    COLORS = {"SquareArea": GOLD, "StreetArea": GRAY85, "AlleyArea": GRAY50, "GatewayArea": BRONZE}

    def construct(self):
        scene = _load(self.params["data"])["scenes"][0]
        out = [a for a in scene["areas"] if not a["interior"]]
        ids = {a["i"] for a in out}
        edges = sorted({(e[0], e[1]) for e in scene["edges"] if e[0] in ids and e[1] in ids and e[2] == "path"})

        self.header("one city, as a graph", f"the streets of city {scene['id']}, as the game builds them")

        # Laid out from the graph itself (Kamada-Kawai: edge length ~ graph distance), on the right
        # two thirds of the frame; the notes build up down the left third.
        import networkx as nx
        kinds = collections.defaultdict(list)
        for a in out:
            kinds[a["kind"]].append(a)
        sq, streets = kinds["SquareArea"], kinds["StreetArea"]
        g = nx.Graph()
        g.add_nodes_from(ids)
        g.add_edges_from(edges)
        raw = nx.kamada_kawai_layout(g, scale=1.0)
        xs = np.array([raw[i][0] for i in ids]); ys = np.array([raw[i][1] for i in ids])
        box_l, box_r, box_b, box_t = -1.2, 6.0, -3.2, 1.5
        def fit(v, lo, hi, a_, b_):
            return a_ + (v - lo) / max(hi - lo, 1e-6) * (b_ - a_)
        pos = {i: np.array([fit(raw[i][0], xs.min(), xs.max(), box_l, box_r),
                            fit(raw[i][1], ys.min(), ys.max(), box_b, box_t), 0]) for i in ids}

        nodes = {a["i"]: self.node(a["name"], self.COLORS[a["kind"]], 20).move_to(pos[a["i"]]) for a in out}
        self.untangle(list(nodes.values()), margin=0.3, bounds=(box_l - 0.6, 6.9, -3.75, 2.0))
        kind_of = {a["i"]: a["kind"] for a in out}

        def links(pred):
            return [(a, b) for a, b in edges if pred(kind_of[a], kind_of[b])]

        def draw_edges(pairs, color=GRAY50, dashed=False):
            return [self.edge(nodes[a], nodes[b], color=color, dashed=dashed) for a, b in pairs]

        notes = VGroup()

        def note(s, color):
            t = self.text(s, 21, color)
            if t.width > 4.6:
                t.scale_to_fit_width(4.6)
            notes.add(t)
            t.to_edge(LEFT, buff=0.5).set_y(0.9 - 0.62 * (len(notes) - 1))
            return t

        # the hubs
        self.play(*[GrowFromCenter(nodes[a["i"]]) for a in sq], run_time=0.8)
        self.play(FadeIn(note(f"{len(sq)} square{'s' if len(sq) > 1 else ''}: the hubs", GOLD)), run_time=0.4)
        self.beat(0.08)

        # streets, joined to the squares
        s_edges = links(lambda x, y: {x, y} == {"SquareArea", "StreetArea"})
        self.play(*[FadeIn(nodes[a["i"]], shift=DOWN * 0.2) for a in streets], run_time=0.8)
        self.play(*[Create(e) for e in draw_edges(s_edges, GRAY60)], run_time=0.9)
        self.play(FadeIn(note(f"{len(streets)} streets off the squares", GRAY85)), run_time=0.4)
        self.beat(0.08)

        # street to street, so the town is not only a star
        ss = links(lambda x, y: x == y == "StreetArea")
        if ss:
            self.play(*[Create(e) for e in draw_edges(ss, GRAY60)], run_time=0.7)
            self.play(FadeIn(note("streets joined end to end", GRAY75)), run_time=0.4)
        self.beat(0.06)

        # alleys hang off one street; the gate opens onto the first street
        al = links(lambda x, y: "AlleyArea" in (x, y))
        gt = links(lambda x, y: "GatewayArea" in (x, y))
        self.play(*[FadeIn(nodes[a["i"]], shift=UP * 0.2) for a in kinds["AlleyArea"]], run_time=0.6)
        self.play(*[Create(e) for e in draw_edges(al, GRAY35, dashed=True)], run_time=0.7)
        self.play(FadeIn(note("an alley hangs off one street", GRAY50)), run_time=0.4)
        self.play(*[FadeIn(nodes[a["i"]], shift=UP * 0.2) for a in kinds["GatewayArea"]], run_time=0.5)
        self.play(*[Create(e) for e in draw_edges(gt, BRONZE)], run_time=0.6)
        self.play(FadeIn(note("the gate, where the road comes in", BRONZE)), run_time=0.4)

        # the doors: every building opens onto a street or a square
        doors = collections.Counter()
        interior = {a["i"] for a in scene["areas"] if a["interior"]}
        for e in scene["edges"]:
            if e[2] == "Door":
                a, b = e[0], e[1]
                if a in ids and b in interior:
                    doors[a] += 1
                elif b in ids and a in interior:
                    doors[b] += 1
        ticks = VGroup()
        for i, n in doors.items():
            for k in range(min(n, 6)):
                ticks.add(Square(0.09, fill_color=LIGHT_PURPLE, fill_opacity=1, stroke_width=0)
                          .next_to(nodes[i], DOWN, buff=0.07).shift(RIGHT * (k - (min(n, 6) - 1) / 2) * 0.14))
        if len(ticks):
            self.play(LaggedStartMap(FadeIn, ticks, lag_ratio=0.05), run_time=0.8)
            self.play(FadeIn(note("each mark: a door into a building", LIGHT_PURPLE)), run_time=0.4)
        self.fill_to_target(tail=0.8)


# ── 3. size ───────────────────────────────────────────────────────────────────

HOUSES = {"Merchant House", "Counting House", "Wool Exchange", "Spice House", "Cloth Exchange"}


def _by_size(paths):
    rows = collections.defaultdict(list)
    for p in paths:
        for s in _load(p)["scenes"]:
            size = sum(1 for x in s["sections"] if x["name"] in HOUSES)
            rows[size].append({
                "Streets and squares": sum(1 for a in s["areas"] if not a["interior"]),
                "Buildings": sum(1 for x in s["sections"] if x["interior"]),
                "Residents": sum(1 for n in s["npcs"] if n["named"]),
                "Areas in all": len(s["areas"]),
            })
    return rows


class Sizes(BrandScene):
    """Size is rolled per city, one to three, and it decides nearly everything."""

    def construct(self):
        paths = self.params["data"]
        rows = _by_size(paths)
        n = sum(len(v) for v in rows.values())
        self.header("size: one, two or three", f"averages over {n} generated cities")

        metrics = ["Streets and squares", "Buildings", "Residents", "Areas in all"]
        sizes = sorted(rows)
        colors = [GRAY60, MEDIUM_YELLOW, GOLD]
        panels = VGroup()
        for m in metrics:
            vals = [round(sum(r[m] for r in rows[s]) / len(rows[s]), 1) for s in sizes]
            chart = self.hbar_chart([f"size {s}" for s in sizes], vals, colors, width=3.0, bar_height=0.32,
                                    gap=0.16, label_size=19)
            title = self.text(m, 23, GRAY85).next_to(chart, UP, buff=0.3).align_to(chart, LEFT)
            panels.add(VGroup(title, chart))
        panels.arrange_in_grid(rows=2, cols=2, buff=(1.3, 0.9)).next_to(self.mobjects[-1], DOWN, buff=0.5)
        if panels.width > 13:
            panels.scale_to_fit_width(13)
        for p in panels:
            self.play(FadeIn(p[0]), *self.grow(p[1], run_time=0.8), run_time=1.0)
            self.beat(0.05, minimum=0.2)
        self.fill_to_target(tail=0.8)


# ── 4. who lives there ───────────────────────────────────────────────────────

class Crews(BrandScene):
    """BuildTown's rosters, then who 60 cities actually hold."""

    def construct(self):
        self.header("who keeps a city", "its crews, and who that comes to")
        cards = [
            ("INN", GOLD, ["an innkeeper", "+ sailors, in a port"]),
            ("MERCHANT HOUSES  x N", MEDIUM_YELLOW, ["a merchant", "and a clerk"]),
            ("WORKSHOPS  x N+1", LIGHT_YELLOW, ["a master of one of six trades", "an apprentice, housed nearby"]),
            ("WATCH HOUSE", LIGHT_PURPLE, ["N+1 guards", "a captain, at size 3"]),
        ]
        boxes = VGroup()
        for title, color, lines in cards:
            body = VGroup(self.text(title, 22, color), *[self.text(l, 19, GRAY75) for l in lines]).arrange(
                DOWN, aligned_edge=LEFT, buff=0.16)
            frame = self.frame_box(body, color=GRAY35, buff=0.28)
            boxes.add(VGroup(frame, body))
        boxes.arrange_in_grid(rows=2, cols=2, buff=(0.6, 0.45)).shift(DOWN * 0.45)
        for b in boxes:
            self.play(FadeIn(b, shift=UP * 0.15), run_time=0.55)
            self.beat(0.05, minimum=0.25)
        trades = self.text("forge . carpenter . cooper . weaver . bakery . mill", 19, GRAY50).next_to(boxes, DOWN, buff=0.35)
        self.play(FadeIn(trades), run_time=0.5)
        self.beat(0.12)

        # Who one city holds, on average — per city, because a total over sixty cities reads as one
        # city's count to anybody not told otherwise.
        counts = collections.Counter()
        scenes = _load(self.params["data"])["scenes"]
        for s in scenes:
            for npc in s["npcs"]:
                if npc["named"]:
                    counts[npc["archetype"]] += 1
        top = counts.most_common(9)
        per_city = [round(c / len(scenes), 1) for _, c in top]
        chart = self.hbar_chart([a for a, _ in top], per_city, [GOLD] * len(top),
                                width=6.0, bar_height=0.3, gap=0.14, label_size=19, fmt="{:.1f}")
        chart.shift(DOWN * 0.7)
        cap = VGroup(self.text("in one city, on average", 24, GRAY85),
                     self.text(f"counted over {len(scenes)} generated cities", 18, GRAY50)).arrange(DOWN, buff=0.1)
        cap.next_to(chart, UP, buff=0.3)
        self.play(FadeOut(boxes), FadeOut(trades), run_time=0.5)
        self.play(FadeIn(cap), *self.grow(chart), run_time=1.2)
        self.fill_to_target(tail=0.8)


# ── 5. climate ────────────────────────────────────────────────────────────────

STYLES = [   # CityLayout.Styles and BuildingDescriptions.MaterialsOf
    ("PLAIN", ["Market Square", "High Street", "Cutpurse Alley"], "timber, wattle-and-daub"),
    ("MOUNTAIN", ["Upper Terrace", "Stair Street", "Goat Steps"], "stone"),
    ("HOT STEPPE", ["Bazaar", "Spice Lane", "Covered Passage"], "mudbrick"),
    ("COLD STEPPE", ["Fair Ground", "Sledge Way", "Woodpile Gap"], "logs"),
]


class Styles(BrandScene):
    """One plan, four climates: the ground names the streets and builds the houses."""

    def construct(self):
        self.header("the ground dresses it", "same seed, same plan, four climates")
        cols = VGroup()
        roles = [("square", GOLD), ("street", GRAY85), ("alley", GRAY50)]
        for name, ways, material in STYLES:
            head = self.text(self.spaced(name), 22, LIGHT_PURPLE)
            items = VGroup(*[VGroup(self.text(role, 16, GRAY35), self.text(w, 22, c)).arrange(DOWN, aligned_edge=LEFT, buff=0.06)
                             for (role, c), w in zip(roles, ways)]).arrange(DOWN, aligned_edge=LEFT, buff=0.32)
            mat = VGroup(self.text("built in", 16, GRAY35), self.text(material, 20, BRONZE)).arrange(DOWN, aligned_edge=LEFT, buff=0.06)
            col = VGroup(head, items, mat).arrange(DOWN, aligned_edge=LEFT, buff=0.42)
            cols.add(col)
        cols.arrange(RIGHT, buff=0.7, aligned_edge=UP).shift(DOWN * 0.5)
        if cols.width > 13.2:
            cols.scale_to_fit_width(13.2)
        for c in cols:
            self.play(FadeIn(c, shift=UP * 0.15, lag_ratio=0.1), run_time=0.8)
            self.beat(0.05, minimum=0.2)
        self.fill_to_target(tail=0.8)
