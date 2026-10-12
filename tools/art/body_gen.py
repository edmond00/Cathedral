"""Generates a body art folder (assets/art/body/<name>) from labelled shapes.

    python tools/art/body_gen.py tools/art/specs/lizard.py <out folder>

A spec is a Python file that declares, in cell coordinates (x right, y down, one unit per cell):

  W, H            grid size
  BASE            folder whose CSVs (organs, visual_zones, wounds, gears, layer_colors) are reused
  SHAPES          list of Shape: a region of the body. A shape with a zone is part of the silhouette;
                  a shape with an organ (organ_part name from organs.csv) marks that organ. Higher z is
                  in front. An organ-only shape is clipped to the silhouette under it.
  STAMPS          list of Stamp: single cells placed by hand (eyes, teeth, brain parts) - tiny
                  features a shape cannot express at this resolution
  TEXTURES        organ_part -> function(cell) -> glyph, for organs drawn with their own glyphs
  skin(cell)      -> (glyph, layer) for every body cell no texture or stamp claims
  organ_layer(cell) -> layer for textured organ cells
  GEARS           list of (alias, x, y): where each gear label is written in gears.txt
  SPLIT           organ parts allowed to be in several pieces (lungs either side of a spine)
  GRIT            optional: the retro look (see GRIT_DEFAULTS); without it the art is drawn clean
  WHERE           wounds.csv "where" words that are not organ parts -> the organ part they mean

Every grid comes from the same rasterisation, so the organ, zone, wound and gear grids line up with
the art by construction - which is the step that used to be done by hand.
"""
import csv, math, sys, importlib.util
from collections import Counter, deque
from dataclasses import dataclass, field
from pathlib import Path

SS = 6            # subsamples per cell side
BODY_COVER = 0.40 # share of a cell the silhouette must cover for the cell to be body
ORGAN_COVER = 0.45
EMPTY = "░"


# ── shapes ────────────────────────────────────────────────────────────────

def ellipse(cx, cy, rx, ry, rot=0.0):
    c, s = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    def f(x, y):
        dx, dy = x - cx, y - cy
        u, v = dx * c + dy * s, -dx * s + dy * c
        return (u / rx) ** 2 + (v / ry) ** 2 <= 1.0
    return f


def capsule(*pts):
    """A thick polyline: pts are (x, y, radius), the radius interpolated along each segment."""
    segs = list(zip(pts, pts[1:]))
    def f(x, y):
        for (x0, y0, r0), (x1, y1, r1) in segs:
            dx, dy = x1 - x0, y1 - y0
            l2 = dx * dx + dy * dy
            t = 0.0 if l2 == 0 else max(0.0, min(1.0, ((x - x0) * dx + (y - y0) * dy) / l2))
            px, py, r = x0 + t * dx, y0 + t * dy, r0 + t * (r1 - r0)
            if (x - px) ** 2 + (y - py) ** 2 <= r * r:
                return True
        return False
    return f


def polygon(*pts):
    def f(x, y):
        inside = False
        for (x0, y0), (x1, y1) in zip(pts, pts[1:] + pts[:1]):
            if (y0 > y) != (y1 > y) and x < x0 + (y - y0) * (x1 - x0) / (y1 - y0):
                inside = not inside
        return inside
    return f


@dataclass
class Shape:
    fn: object
    zone: str | None = None
    organ: str | None = None
    z: int = 0
    far: bool = False      # the far side of the body: drawn darker


@dataclass
class Stamp:
    x: int
    y: int
    glyph: str
    organ: str | None = None
    zone: str | None = None   # defaults to whatever zone the cell already has
    layer: int | None = None


@dataclass
class Cell:
    x: int
    y: int
    zone: str | None = None
    organ: str | None = None
    far: bool = False
    depth: int = 0                      # 1 on the silhouette's edge, growing inward
    out: tuple = (0, 0)                 # direction towards the outside, summed over neighbours
    up: int = 0                         # body cells straight above / below, before the outside:
    down: int = 0                       # up / (up + down) is 0 on the back and 1 on the belly
    glyph: str = EMPTY
    layer: int = 0
    stamped: bool = False


# ── grit: the retro, scanned-and-converted look ──────────────────────────

# A spec opts in with GRIT = {...}; every key is optional. Anything that moves cells (the warp, the
# glitched rows) acts on the sampling, before any grid is derived, so the maps stay aligned with
# the art. Deterministic: the same seed gives the same body.
GRIT_DEFAULTS = {
    "seed": 1,
    "warp": 0.55,        # how far, in cells, the silhouette's edge wanders
    "warp_scale": 0.22,  # how fast it wanders (cycles per cell)
    "glitch": 3,         # bands of rows shifted sideways
    "noise": 0.30,       # brightness noise on skin
    "layer_noise": 0.25, # separate brightness noise on the layer map, as a photo's would be
    "dropout": 0.12,     # share of organ cells that drop to the skin's ramp
    "corrupt": 0.02,     # share of body cells replaced by a block glyph
}
RAMP = ".·:~-=+*≡"           # skin, dark to bright
CORRUPT = "▒▓¦╌¬"


def _hash(xi, yi, seed):
    h = (xi * 374761393 + yi * 668265263 + seed * 1442695041) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFF) / 32767.5 - 1.0


def vnoise(x, y, seed):
    """Smooth value noise in [-1, 1]."""
    xi, yi = math.floor(x), math.floor(y)
    fx, fy = x - xi, y - yi
    sx, sy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = _hash(xi, yi, seed), _hash(xi + 1, yi, seed)
    c, d = _hash(xi, yi + 1, seed), _hash(xi + 1, yi + 1, seed)
    return (a + (b - a) * sx) * (1 - sy) + (c + (d - c) * sx) * sy


def grit_of(spec):
    g = getattr(spec, "GRIT", None)
    return None if g is None else {**GRIT_DEFAULTS, **g}


def glitch_bands(g, H):
    """Rows shifted sideways: {row: shift}."""
    rng = __import__("random").Random(g["seed"])
    bands = {}
    for _ in range(g["glitch"]):
        y0, h, s = rng.randrange(H), rng.choice((1, 1, 2, 3)), rng.choice((-1, 1))  # one cell: a one-wide organ stays joined diagonally
        for y in range(y0, min(H, y0 + h)):
            bands[y] = s
    return bands


def make_warp(g, H):
    if not g:
        return lambda x, y: (x, y)
    bands, a, k, s = glitch_bands(g, H), g["warp"], g["warp_scale"], g["seed"]
    def warp(x, y):
        x -= bands.get(int(y), 0)
        return (x + a * vnoise(x * k, y * k, s), y + a * vnoise(x * k + 31.7, y * k + 17.3, s + 1))
    return warp


def apply_grit(grid, g, spec):
    rng = __import__("random").Random(g["seed"] + 7)
    s = g["seed"]
    for row in grid:
        for c in row:
            if not c.zone:
                continue
            # brightness: lit from the upper left, deeper is fuller, plus noise at two scales
            ox, oy = c.out
            n = math.hypot(ox, oy) or 1.0
            light = -(ox * -0.6 + oy * -0.8) / n if (ox or oy) else 0.0
            lum = (0.45 - 0.22 * light + 0.12 * min(c.depth, 4) / 4
                   + g["noise"] * (0.6 * vnoise(c.x * 0.35, c.y * 0.35, s + 2) + 0.4 * rng.uniform(-1, 1)))
            lay = lum + g["layer_noise"] * rng.uniform(-1, 1)
            layer = 0 if lay < 0.12 else 1 if lay < 0.38 else 2 if lay < 0.62 else 3 if lay < 0.85 else 4
            ramp = RAMP[max(0, min(len(RAMP) - 1, int(lum * len(RAMP))))]
            if c.stamped:
                continue
            if c.organ in spec.TEXTURES:
                if rng.random() < g["dropout"]:
                    c.glyph, c.layer = ramp, max(1, layer - 1)
                elif rng.random() < 0.3:
                    c.layer = max(1, min(4, c.layer + rng.choice((-1, 1))))
            else:
                c.glyph, c.layer = ramp, max(1, layer)
            if rng.random() < g["corrupt"]:
                c.glyph = rng.choice(CORRUPT)
            if c.far:
                c.layer = max(0, c.layer - 1)


# ── generation ────────────────────────────────────────────────────────────

def load_spec(path):
    spec = importlib.util.spec_from_file_location("bodyspec", path)
    mod = importlib.util.module_from_spec(spec)
    sys.modules["bodyspec"] = mod
    spec.loader.exec_module(mod)
    return mod


def rasterise(spec):
    W, H = spec.W, spec.H
    warp = make_warp(grit_of(spec), H)
    shapes = sorted(spec.SHAPES, key=lambda s: s.z)
    grid = [[Cell(x, y) for x in range(W)] for y in range(H)]
    n = SS * SS
    for y in range(H):
        for x in range(W):
            zones, organs, fars = Counter(), Counter(), Counter()
            for j in range(SS):
                for i in range(SS):
                    px, py = warp(x + (i + 0.5) / SS, y + (j + 0.5) / SS)
                    top_zone = top = None
                    for s in shapes:              # ascending z: the last hit is in front
                        if s.fn(px, py):
                            top = s
                            if s.zone:
                                top_zone = s
                    if not top_zone:
                        continue
                    zones[top_zone.zone] += 1
                    fars[top_zone.far] += 1
                    if top.organ and top.z >= top_zone.z:
                        organs[top.organ] += 1
            c = grid[y][x]
            if sum(zones.values()) >= BODY_COVER * n:
                c.zone = zones.most_common(1)[0][0]
                c.far = fars.most_common(1)[0][0]
                if organs and organs.most_common(1)[0][1] >= ORGAN_COVER * n:
                    c.organ = organs.most_common(1)[0][0]
    return grid


def measure(grid, W, H):
    """Depth from the silhouette's edge, and which way the outside lies."""
    body = lambda x, y: 0 <= x < W and 0 <= y < H and grid[y][x].zone is not None
    q = deque()
    for row in grid:
        for c in row:
            if not c.zone:
                continue
            ox = oy = 0
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    if (dx or dy) and not body(c.x + dx, c.y + dy):
                        ox += dx; oy += dy
            c.out = (ox, oy)
            c.up = next(k for k in range(H + 1) if not body(c.x, c.y - k - 1))
            c.down = next(k for k in range(H + 1) if not body(c.x, c.y + k + 1))
            if any(not body(c.x + dx, c.y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                c.depth = 1
                q.append(c)
    while q:
        c = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if body(c.x + dx, c.y + dy):
                nb = grid[c.y + dy][c.x + dx]
                if nb.depth == 0:
                    nb.depth = c.depth + 1
                    q.append(nb)


def generate(spec_path, out_dir):
    spec = load_spec(spec_path)
    W, H = spec.W, spec.H
    base = Path(spec.BASE)
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)

    organ_rows = list(csv.DictReader(open(base / "organs.csv", encoding="utf-8")))
    part_id = {r["organ_part"]: r["id"] for r in organ_rows}
    zone_id = {}
    for line in open(base / "visual_zones.csv", encoding="utf-8"):
        if line.strip():
            name, idx = line.strip().split(",")
            zone_id[name] = idx

    for s in spec.SHAPES:
        assert s.zone is None or s.zone in zone_id, f"unknown zone {s.zone}"
        assert s.organ is None or s.organ in part_id, f"unknown organ part {s.organ}"

    grid = rasterise(spec)
    measure(grid, W, H)

    g = grit_of(spec)
    bands = glitch_bands(g, H) if g else {}
    for st in spec.STAMPS:
        c = grid[st.y][st.x + bands.get(st.y, 0)]     # a stamp rides its glitched row
        c.zone = st.zone or c.zone
        assert c.zone, f"stamp {st.glyph!r} at {st.x},{st.y} is outside the body; give it a zone"
        if st.organ:
            assert st.organ in part_id, f"unknown organ part {st.organ}"
            c.organ = st.organ
        c.glyph, c.stamped = st.glyph, True
        if st.layer is not None:
            c.layer = st.layer
    measure(grid, W, H)

    for row in grid:
        for c in row:
            if not c.zone or c.stamped and c.layer:
                continue
            if c.stamped:
                c.layer = spec.organ_layer(c)
            elif c.organ in spec.TEXTURES:
                c.glyph = spec.TEXTURES[c.organ](c)
                c.layer = spec.organ_layer(c)
            else:
                c.glyph, c.layer = spec.skin(c)
            if c.far:
                c.layer = max(0, c.layer - 1)
    if g:
        apply_grit(grid, g, spec)

    problems = check(grid, W, H, part_id, zone_id, getattr(spec, "SPLIT", set()))

    art = [[c.glyph if c.zone else EMPTY for c in row] for row in grid]
    write(out / "ascii_art.txt", art)
    write(out / "layer_map.txt", [[str(c.layer) if c.zone else "0" for c in row] for row in grid])
    write(out / "organs.txt", [[part_id[c.organ] if c.organ else a for c, a in zip(row, arow)]
                               for row, arow in zip(grid, art)])
    write(out / "visual_zones.txt", [[zone_id[c.zone] if c.zone else a for c, a in zip(row, arow)]
                                     for row, arow in zip(grid, art)])
    write(out / "wounds.txt", place_wounds(grid, art, base, organ_rows, problems, getattr(spec, "WHERE", {})))
    write(out / "gears.txt", place_gears(art, getattr(spec, "GEARS", [])))
    for f in ("organs.csv", "visual_zones.csv", "wounds.csv", "gears.csv", "layer_colors.csv"):
        (out / f).write_bytes((base / f).read_bytes())

    print(f"{W}x{H} -> {out}")
    for p in problems:
        print("  !", p)
    return not problems


def write(path, rows):
    path.write_text("\n".join("".join(r) for r in rows) + "\n", encoding="utf-8", newline="\n")


def place_wounds(grid, art, base, organ_rows, problems, where_alias):
    """One cell per wound, on the organ it names, nearest the organ's centre and never shared."""
    rows = [r[:] for r in art]
    used = set()
    for w in csv.DictReader(open(base / "wounds.csv", encoding="utf-8")):
        if w["id"] == "*":
            continue
        where = w["where"].replace(" ", "_")
        where = where_alias.get(where, where)
        parts = {r["organ_part"] for r in organ_rows
                 if where in (r["organ_part"], r["organ"], r["body_part"])}
        cells = [c for row in grid for c in row if c.organ in parts and (c.x, c.y) not in used]
        if not cells:
            problems.append(f"wound {w['id']} ({w['name']}, {w['where']}) has no cell to sit on")
            continue
        if w["name"].startswith("tail"):          # a tail wound sits on the tail: the far end
            pick = min(cells, key=lambda c: c.x)
        else:
            cx = sum(c.x for c in cells) / len(cells)
            cy = sum(c.y for c in cells) / len(cells)
            pick = min(cells, key=lambda c: (c.x - cx) ** 2 + (c.y - cy) ** 2)
        used.add((pick.x, pick.y))
        rows[pick.y][pick.x] = w["id"]
    return rows


def place_gears(art, gears):
    rows = [r[:] for r in art]
    for alias, x, y in gears:
        for i, ch in enumerate(alias.upper()):
            if 0 <= x + i < len(rows[y]):
                rows[y][x + i] = ch
    return rows


def check(grid, W, H, part_id, zone_id, split):
    problems = []
    cells = [c for row in grid for c in row if c.zone]
    ids = set(part_id.values()) | set(zone_id.values())
    for c in cells:
        if c.glyph in ids or c.glyph.isascii() and c.glyph.isalnum():
            problems.append(f"glyph {c.glyph!r} at {c.x},{c.y} reads as an id in the map files")
        if c.glyph in (" ", EMPTY):
            problems.append(f"body cell {c.x},{c.y} has an invisible glyph")
    for part in part_id:
        own = {(c.x, c.y) for c in cells if c.organ == part}
        if not own:
            problems.append(f"organ part {part} has no cell")
            continue
        seen, q = set(), deque([next(iter(own))])
        while q:
            p = q.popleft()
            if p in seen:
                continue
            seen.add(p)
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    nb = (p[0] + dx, p[1] + dy)
                    if nb in own and nb not in seen:
                        q.append(nb)
        if len(seen) != len(own) and part not in split:
            problems.append(f"organ part {part} is in pieces ({len(seen)} of {len(own)} cells joined)")
    for zone in zone_id:
        if not any(c.zone == zone for c in cells):
            problems.append(f"zone {zone} has no cell")
    return problems


if __name__ == "__main__":
    ok = generate(sys.argv[1], sys.argv[2])
    sys.exit(0 if ok else 1)
