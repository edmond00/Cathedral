"""A great reptile seen from above, head up - the reptile anatomy's own list (assets/art/body/reptile).

The same view as the lizard, and for the same reason: from above, the two things the reptile has
and the beast has not are both in plain sight. The horns sweep forward off the brow with a third on
the nose; the thagomizer is the pairs of spikes standing out of the end of the tail. There are no
claws - the reptile list has none - so the toes end blunt.

Heavy rather than lithe: a broad skull, a short thick neck, a barrel of a body slung between four
sprawling limbs, and a tail as long again as the body that curls away to the left. Any reptile of
the anatomy draws on it, the crocodile and the carnotaurus alike, as every beast draws on the wolf.

From above the animal's left is the viewer's left.
"""
import math
from body_gen import Shape, Stamp, ellipse, capsule

W, H = 50, 72
BASE = "assets/art/body/reptile"
Y = 1

T, M, E = "zone_trunk", "zone_muzzle", "zone_encephalon"
LF, RF, LH, RH = "zone_left_forelimbs", "zone_right_forelimbs", "zone_left_hindlimbs", "zone_right_hindlimbs"


def cap(*pts):
    return capsule(*[(x, y + Y, r) for x, y, r in pts])


def ell(cx, cy, rx, ry, rot=0.0):
    return ellipse(cx, cy + Y, rx, ry, rot)


def toe(x0, y0, x1, y1, zone, leg):
    """One short, thick toe; the tip is blunt - a reptile of this list has no claws."""
    return (Shape(cap((x0, y0, 0.75), (x1, y1, 0.65)), zone, leg, 9),
            Stamp(round(x1 - 0.5), round(y1 + Y - 0.5), "◦", leg, zone))


def limb(zone, leg, joints, toes):
    (x0, y0) = joints[-1][:2]
    parts = [toe(x0, y0, x, y, zone, leg) for x, y in toes]
    return [Shape(cap(*joints), zone, leg, 9)] + [p[0] for p in parts], [p[1] for p in parts]


limbs = [
    limb(LF, "left_foreleg", [(20, 20, 2.9), (13.5, 21.5, 2.4), (11.8, 18.0, 2.1)],
         [(9.0, 15.6), (11.2, 14.8), (13.6, 15.4)]),
    limb(RF, "right_foreleg", [(30, 20, 2.9), (36.5, 21.5, 2.4), (38.2, 18.0, 2.1)],
         [(41.0, 15.6), (38.8, 14.8), (36.4, 15.4)]),
    limb(LH, "left_hindleg", [(21, 35, 3.2), (12.5, 34, 2.6), (10.8, 40, 2.1)],
         [(7.8, 42.6), (9.8, 43.8), (12.3, 43.8)]),
    limb(RH, "right_hindleg", [(30, 35, 3.2), (38.5, 34, 2.6), (40.2, 40, 2.1)],
         [(43.2, 42.6), (41.2, 43.8), (38.7, 43.8)]),
]

SPINE = [(25, 14, 0.6), (25, 19, 0.6), (25.3, 27, 0.6), (25.8, 35, 0.6), (26.5, 41, 0.6), (28, 48, 0.6),
         (28.5, 54, 0.6), (26, 59.5, 0.6)]

# the end of the tail, and the four spikes standing out of it in two pairs
TAIL_END = [(26, 59.5, 1.8), (22.5, 63.5, 1.3), (18.5, 66, 1.0), (15, 67, 0.7)]
def _spikes():
    """Two pairs of spikes standing out of the tail's end, raked back the way the tail runs."""
    out = []
    for (x0, y0), (x1, y1) in (((27.0, 57.5), (23.5, 62.5)), ((21.5, 64.3), (16.5, 66.6))):
        dx, dy = x1 - x0, y1 - y0
        l = math.hypot(dx, dy)
        dx, dy = dx / l, dy / l
        for side in (1, -1):
            nx, ny = -dy * side, dx * side
            ax, ay = nx * 0.85 + dx * 0.55, ny * 0.85 + dy * 0.55
            m = math.hypot(ax, ay)
            out.append(((x0 + dx, y0 + dy), (x0 + dx + ax / m * 4.6, y0 + dy + ay / m * 4.6)))
    return out


SPIKES = _spikes()

SHAPES = [
    # trunk: neck, body and the tail - one zone, as on the beast
    Shape(cap((25, 13, 4.6), (25, 18, 5.6)), T, z=10),
    Shape(cap((25, 18, 7.0), (25.3, 27, 7.8), (25.8, 35, 6.0)), T, z=10),
    Shape(cap((25.8, 35, 5.4), (26.5, 41, 4.4), (28, 48, 3.3), (28.5, 54, 2.6), (26, 59.5, 2.0)), T, z=10),
    Shape(cap(*TAIL_END), T, z=10),
    *[Shape(cap((a[0], a[1], 0.8), (b[0], b[1], 0.55)), T, "thagomizer", z=10) for a, b in SPIKES],

    # trunk organs, either side of the spine
    Shape(cap(*SPINE), organ="backbone", z=15),
    Shape(ell(21.4, 21.5, 2.4, 3.8), organ="pulmones", z=11),
    Shape(ell(29.0, 21.5, 2.4, 3.8), organ="pulmones", z=11),
    Shape(ell(22.0, 27.0, 1.6, 1.4), organ="heart", z=12),
    Shape(ell(29.3, 26.8, 2.3, 1.5), organ="hepar", z=12),
    Shape(ell(22.0, 30.6, 2.0, 1.5), organ="paunch", z=12),
    Shape(ell(29.6, 30.4, 1.4, 1.2), organ="spleen", z=12),
    Shape(ell(25.7, 34.4, 4.0, 1.8), organ="viscera", z=11),
    Shape(cap((26.6, 58.4, 0.7), (22.5, 63.5, 0.7), (17.0, 66.4, 0.6)), organ="thagomizer", z=12),

    # head: a broad skull, the snout tapering ahead, the brow horns sweeping forward and out
    Shape(ell(25, 10.0, 5.2, 4.0), M, z=30),
    Shape(cap((25, 9, 4.6), (25, 4, 3.4), (25, 1.2, 2.4)), M, z=30),
    Shape(ell(25, 11.0, 3.2, 2.0), E, z=31),
    Shape(ell(25, 1.2, 2.0, 1.1), organ="snout", z=32),
    Shape(cap((21.0, 9.0, 1.0), (18.6, 6.4, 0.8), (17.8, 3.4, 0.55)), M, "horns", z=29),
    Shape(cap((29.0, 9.0, 1.0), (31.4, 6.4, 0.8), (32.2, 3.4, 0.55)), M, "horns", z=29),
] + [s for shapes, _ in limbs for s in shapes]

STAMPS = [
    # brain. No parietal eye: the reptile list, like the beast's, has no pineal gland
    Stamp(24, 11 + Y, "©", "cerebrum"), Stamp(25, 11 + Y, "⟁", "hippocampus"), Stamp(26, 11 + Y, "©", "cerebrum"),
    Stamp(24, 12 + Y, "♋", "anamnesis"), Stamp(26, 12 + Y, "Ѳ", "cerebellum"),
    # eyes under the brow, ear openings behind them
    Stamp(22, 8 + Y, "◐", "left_eye", M), Stamp(28, 8 + Y, "◑", "right_eye", M),
    Stamp(21, 11 + Y, "◌", "left_ear", M), Stamp(29, 11 + Y, "◌", "right_ear", M),
    # the nose horn, nostrils either side of it, teeth along both edges of the jaw, the tongue in the mouth
    Stamp(25, 3 + Y, "∆", "horns", M),
    Stamp(24, 1 + Y, "∘", "snout"), Stamp(26, 1 + Y, "∘", "snout"),
    Stamp(22, 3 + Y, "▸", "fangs", M), Stamp(22, 5 + Y, "▸", "fangs", M),
    Stamp(28, 3 + Y, "◂", "fangs", M), Stamp(28, 5 + Y, "◂", "fangs", M),
    Stamp(25, 6 + Y, "◡", "tongue", M),
] + [st for _, stamps in limbs for st in stamps]

SPLIT = {"cerebrum", "pulmones", "viscera", "fangs", "horns", "thagomizer"}

VISCERA = "ოჩშმნტჹცპჟϑქღყწჭსჲრძუფ"
TEXTURES = {
    "backbone": lambda c: "▇" if c.y % 2 == 0 else "⑆",
    "pulmones": lambda c: "▞" if (c.x + c.y) % 2 == 0 else "/",
    "heart": lambda c: "♥",
    "hepar": lambda c: "↺" if c.x % 2 == 0 else "↻",
    "spleen": lambda c: "≈",
    "paunch": lambda c: "∭",
    "viscera": lambda c: VISCERA[(c.x * 7 + c.y * 13) % len(VISCERA)],
    "snout": lambda c: "-",
    "horns": lambda c: "∆" if c.depth == 1 else "‖",
    "thagomizer": lambda c: "†" if c.depth == 1 else "‡",
}

BRIGHT = {"heart": 3, "backbone": 2, "fangs": 4, "left_eye": 4, "right_eye": 4, "horns": 4, "thagomizer": 4}


def organ_layer(c):
    return BRIGHT.get(c.organ, 2)


def skin(c):
    ox, oy = c.out
    limb = c.zone not in (T, M, E)
    if c.depth == 1:                                    # the outline, drawn along its own direction
        if ox == 0 and oy == 0:
            return ":", 1
        if abs(ox) > 2 * abs(oy):
            return "│" if limb else "(" if ox < 0 else ")", 2
        if abs(oy) > 2 * abs(ox):
            return "─" if limb else "^" if oy < 0 else "‿", 2
        return ("/" if ox * oy < 0 else "\\"), 2
    if limb:
        return ("*" if (c.x + c.y) % 3 == 0 else "+" if (c.x + c.y) % 3 == 1 else "="), 2
    if c.depth == 2:
        return "~", 2
    return ("▪" if (c.x + c.y) % 2 == 0 else "·"), 3      # armoured scutes along the back


GEARS = [("neckwear", 21, 16 + Y)]

GRIT = {"seed": 2}
