"""A lizard seen from above, head up, like the lizard emoji. Reuses the beast's organ list
(assets/art/body/beast), so any species with the beast's anatomy can use it.

From above the animal's left is the viewer's left. The backbone runs down the middle, so the lungs,
the gut and the claws come in pieces either side of it - which the beast's maps never needed.
"""
import math
from body_gen import Shape, Stamp, ellipse, capsule

W, H = 46, 64
BASE = "assets/art/body/beast"
Y = 2   # everything below is drawn 2 rows down, leaving room for the tongue

T, M, E = "zone_trunk", "zone_muzzle", "zone_encephalon"
LF, RF, LH, RH = "zone_left_forelimbs", "zone_right_forelimbs", "zone_left_hindlimbs", "zone_right_hindlimbs"


def cap(*pts):
    return capsule(*[(x, y + Y, r) for x, y, r in pts])


def ell(cx, cy, rx, ry, rot=0.0):
    return ellipse(cx, cy + Y, rx, ry, rot)


ARROWS = "→↘↓↙←↖↑↗"


def toe(x0, y0, x1, y1, zone, leg, claws):
    """One toe: a thin line from the wrist, its tip a claw pointing the way the toe points."""
    a = math.atan2(y1 - y0, x1 - x0)
    tip = ARROWS[round(a / (math.pi / 4)) % 8]
    sx, sy = math.cos(a), math.sin(a)
    return (Shape(cap((x0, y0, 0.6), (x1, y1, 0.55)), zone, leg, 9),
            Stamp(round(x1 + 0.6 * sx - 0.5), round(y1 + Y + 0.6 * sy - 0.5), tip, claws, zone))


def limb(zone, leg, claws, joints, toes):
    (x0, y0) = joints[-1][:2]
    parts = [toe(x0, y0, x, y, zone, leg, claws) for x, y in toes]
    return [Shape(cap(*joints), zone, leg, 9)] + [p[0] for p in parts], [p[1] for p in parts]


limbs = [
    limb(LF, "left_foreleg", "left_foreclaws", [(18, 17, 2.0), (11, 18, 1.5), (9, 13, 1.2)],
         [(6.2, 10.4), (8.6, 9.6), (11.0, 10.4), (5.8, 13.6)]),
    limb(RF, "right_foreleg", "right_foreclaws", [(27, 17, 2.0), (34, 18, 1.5), (36, 13, 1.2)],
         [(38.8, 10.4), (36.4, 9.6), (34.0, 10.4), (39.2, 13.6)]),
    limb(LH, "left_hindleg", "left_hindclaws", [(20, 30, 2.4), (12.5, 28, 1.8), (10.5, 34, 1.3)],
         [(7.5, 36.5), (9.3, 37.8), (11.5, 38.3), (7.0, 34.0)]),
    limb(RH, "right_hindleg", "right_hindclaws", [(28, 30, 2.4), (35.5, 28, 1.8), (37.5, 34, 1.3)],
         [(40.5, 36.5), (38.7, 37.8), (36.5, 38.3), (41.0, 34.0)]),
]

SPINE = [(22, 10, 0.5), (22, 15, 0.5), (23, 23, 0.5), (24, 30, 0.5), (25.5, 36, 0.5), (28, 42, 0.5),
         (29.5, 48, 0.5), (26.5, 54, 0.5), (21, 57.8, 0.5), (16, 59.5, 0.45), (12.5, 59.1, 0.4)]

SHAPES = [
    # trunk: neck, body and the tail curling away to the left - one zone, as on the beast
    Shape(cap((22, 9, 3.2), (22, 15, 4.2)), T, z=10),
    Shape(cap((22, 15, 5.0), (23, 23, 5.8), (24, 30, 4.6)), T, z=10),
    Shape(cap((24, 30, 4.2), (25.5, 36, 3.2), (28, 42, 2.5), (29.5, 48, 1.9), (26.5, 54, 1.5),
              (21, 57.8, 1.1), (16, 59.5, 0.8), (12, 59, 0.5)), T, z=10),

    # trunk organs, either side of the spine
    Shape(cap(*SPINE), organ="backbone", z=15),
    Shape(ell(19.2, 19.5, 2.0, 3.2), organ="pulmones", z=11),
    Shape(ell(26.4, 19.5, 2.0, 3.2), organ="pulmones", z=11),
    Shape(ell(20.0, 24.6, 1.4, 1.2), organ="heart", z=12),
    Shape(ell(26.6, 24.4, 2.0, 1.3), organ="hepar", z=12),
    Shape(ell(20.3, 27.7, 1.7, 1.3), organ="paunch", z=12),
    Shape(ell(27.3, 27.5, 1.2, 1.0), organ="spleen", z=12),
    Shape(ell(24.2, 31.0, 3.6, 1.6), organ="viscera", z=11),

    # head: the skull behind, the snout tapering ahead
    Shape(ell(22, 6.5, 4.2, 4.2), M, z=30),
    Shape(cap((22, 6, 3.8), (22, 1.2, 1.6)), M, z=30),
    Shape(ell(22, 8.0, 2.8, 2.0), E, z=31),
    Shape(ell(22, 2.0, 1.6, 1.3), organ="snout", z=32),
] + [s for shapes, _ in limbs for s in shapes]

STAMPS = [
    # brain. No parietal eye: the beast anatomy has no pineal gland for it to stand for
    Stamp(21, 7 + Y, "©", "cerebrum"), Stamp(22, 7 + Y, "⟁", "hippocampus"), Stamp(23, 7 + Y, "©", "cerebrum"),
    Stamp(21, 8 + Y, "♋", "anamnesis"), Stamp(23, 8 + Y, "Ѳ", "cerebellum"),
    # eyes on either side of the head, ear openings behind them
    Stamp(19, 5 + Y, "◐", "left_eye", M), Stamp(25, 5 + Y, "◑", "right_eye", M),
    Stamp(18, 8 + Y, "◌", "left_ear", M), Stamp(26, 8 + Y, "◌", "right_ear", M),
    # nostrils, teeth along both edges of the jaw, the forked tongue flicked out ahead
    Stamp(21, 1 + Y, "∘", "snout"), Stamp(23, 1 + Y, "∘", "snout"),
    Stamp(19, 3 + Y, "▸", "fangs", M), Stamp(25, 3 + Y, "◂", "fangs", M),
    Stamp(22, 1, "|", "tongue", M), Stamp(22, 0, "⋎", "tongue", M),
] + [st for _, stamps in limbs for st in stamps]

SPLIT = {"cerebrum", "pulmones", "viscera", "fangs", "left_foreclaws", "right_foreclaws", "left_hindclaws",
         "right_hindclaws"}

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
}

BRIGHT = {"heart": 3, "backbone": 2, "fangs": 4, "left_eye": 4, "right_eye": 4,
          "left_foreclaws": 4, "right_foreclaws": 4, "left_hindclaws": 4, "right_hindclaws": 4}


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
    return ("▪" if (c.x + c.y) % 2 == 0 else "·"), 3      # scales along the back


GEARS = [("neckwear", 18, 13 + Y)]

GRIT = {"seed": 2}
