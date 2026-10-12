"""A goblin, front view, on the human's organ list (assets/art/body/human).

Drawn to anatomical proportions rather than cartoon ones: the head is a fifth of the height (a man's
is an eighth), the ears swept back rather than out, the shoulders hunched up around a short neck.
Gaunt: a narrow rib cage over a slight belly, stringy limbs with the muscles still showing as
swellings (deltoid, biceps, forearm, thigh, calf), long arms reaching mid-thigh. It stands with its
weight on its right leg, the left knee eased - the way a body actually stands.

The claws and fangs are drawn, not organs: they are the human's hands, feet and teeth. Like the
human it faces the viewer, so its right side is on the viewer's left.
"""
import math
from body_gen import Shape, Stamp, ellipse, capsule, polygon

W, H = 40, 68
BASE = "assets/art/body/human"
CX = 20


def mx(x):
    return 2 * CX - x


E, V, T = "zone_encephalon", "zone_visage", "zone_trunk"
RA, LA, RL, LL = "zone_right_arm", "zone_left_arm", "zone_right_leg", "zone_left_leg"

ARROWS = "→↘↓↙←↖↑↗"


def digit(x0, y0, x1, y1, zone, organ, r=0.5):
    """A finger or a toe, its tip a claw pointing the way it points."""
    a = math.atan2(y1 - y0, x1 - x0)
    sx, sy = math.cos(a), math.sin(a)
    return (Shape(capsule((x0, y0, r + 0.05), (x1, y1, r)), zone, organ, 21),
            Stamp(round(x1 + 0.6 * sx - 0.5), round(y1 + 0.6 * sy - 0.5), ARROWS[round(a / (math.pi / 4)) % 8],
                  organ, zone))


def arm(m, zone, arm_part, hand):
    """An arm hanging a little away from the body; m mirrors x for the left side."""
    shapes = [
        Shape(ellipse(m(12.8), 19.6, 2.3, 2.6), zone, arm_part, 9),                         # deltoid
        Shape(ellipse(m(12.1), 24.0, 1.9, 3.1), zone, arm_part, 9),                         # biceps
        Shape(capsule((m(12.6), 20.0, 1.7), (m(11.6), 28.5, 1.2)), zone, arm_part, 9),
        Shape(capsule((m(11.6), 28.5, 1.3), (m(11.1), 31.5, 1.6), (m(10.6), 38.0, 0.9)), zone, arm_part, 9),
        Shape(ellipse(m(10.4), 40.2, 1.5, 2.0), zone, hand, 21),
    ]
    stamps = []
    for x0, y0, x1, y1 in ((9.6, 41.5, 8.8, 46.0), (10.3, 41.8, 10.1, 46.8), (11.0, 41.6, 11.4, 46.2),
                           (11.6, 40.5, 12.6, 43.6)):
        s, st = digit(m(x0), y0, m(x1), y1, zone, hand)
        shapes.append(s); stamps.append(st)
    return shapes, stamps


def leg(zone, leg_part, foot, hip, knee, calf, ankle, toes):
    """hip, knee, calf, ankle: (x, y); toes: list of tip (x, y) fanning from the foot."""
    thigh_angle = -math.degrees(math.atan2(knee[0] - hip[0], knee[1] - hip[1]))
    shapes = [
        Shape(capsule((*hip, 2.6), (*knee, 1.6)), zone, leg_part, 9),
        Shape(ellipse(hip[0] + (knee[0] - hip[0]) * 0.4, hip[1] + (knee[1] - hip[1]) * 0.4, 2.4, 4.4,
                      thigh_angle), zone, leg_part, 9),
        Shape(capsule((*knee, 1.5), (*calf, 1.8), (*ankle, 0.9)), zone, leg_part, 9),
        Shape(ellipse(ankle[0] - 0.3, ankle[1] + 2.0, 1.8, 1.1), zone, foot, 21),
    ]
    stamps = []
    for tx, ty in toes:
        s, st = digit(ankle[0] - 0.3, ankle[1] + 2.2, tx, ty, zone, foot, 0.6)
        shapes.append(s); stamps.append(st)
    return shapes, stamps


right_arm = arm(lambda x: x, RA, "right_arm", "right_hand")
left_arm = arm(mx, LA, "left_arm", "left_hand")
# weight on the right leg (viewer's left): straight, under the body; the left knee eased in and down
right_leg = leg(RL, "right_leg", "right_foot", (17.2, 37.0), (16.6, 48.0), (16.4, 52.0), (16.8, 61.0),
                [(14.4, 65.0), (15.8, 65.6), (17.4, 65.3)])
left_leg = leg(LL, "left_leg", "left_foot", (22.8, 37.6), (23.9, 48.6), (24.5, 52.6), (25.2, 61.4),
               [(24.0, 65.6), (25.6, 66.0), (27.2, 65.5)])

SHAPES = [
    # trunk: a short neck sunk between hunched shoulders, a narrow rib cage, waist, belly, pelvis
    Shape(capsule((CX, 13.5, 1.9), (CX, 17.0, 2.0)), T, z=10),
    Shape(polygon((17.4, 14.8), (22.6, 14.8), (27.8, 18.2), (26.5, 19.6), (13.5, 19.6), (12.2, 18.2)), T, z=10),
    Shape(ellipse(CX, 23.2, 6.6, 5.6), T, z=10),
    Shape(capsule((CX, 28.0, 4.4), (CX, 33.0, 4.0)), T, z=10),
    Shape(ellipse(CX, 32.2, 4.9, 3.6), T, z=10),
    Shape(polygon((15.0, 34.0), (25.0, 34.0), (25.8, 37.6), (20.0, 40.2), (14.4, 37.4)), T, z=10),

    # trunk organs, the backbone down the middle
    Shape(capsule((CX, 15.5, 0.5), (CX, 35.5, 0.5)), organ="backbone", z=15),
    Shape(ellipse(17.6, 22.6, 2.2, 3.2), organ="pulmones", z=11),
    Shape(ellipse(22.4, 22.6, 2.2, 3.2), organ="pulmones", z=11),
    Shape(ellipse(21.7, 25.8, 1.1, 0.9), organ="heart", z=12),
    Shape(ellipse(17.5, 28.1, 2.0, 1.0), organ="hepar", z=12),
    Shape(ellipse(23.2, 28.2, 0.9, 0.8), organ="spleen", z=12),
    Shape(ellipse(21.9, 30.0, 1.6, 1.0), organ="paunch", z=12),
    Shape(ellipse(CX, 32.8, 3.6, 2.0), organ="viscera", z=11),
    Shape(ellipse(CX, 38.3, 1.1, 0.9), organ="genitories", z=13),

    # head: cranium, a heavy jaw, the brain in the top of it
    Shape(ellipse(CX, 7.0, 5.6, 5.2), V, z=30),
    Shape(ellipse(CX, 10.6, 4.5, 4.0), V, z=30),
    Shape(ellipse(CX, 5.4, 5.2, 3.6), E, z=31),
    Shape(ellipse(CX, 4.8, 4.0, 2.0), organ="cerebrum", z=32),
    Shape(ellipse(CX, 7.6, 1.8, 0.7), organ="cerebellum", z=32),

    # ears: long and pointed, swept back and up
    Shape(polygon((15.2, 6.2), (8.2, 2.6), (10.4, 6.4), (15.0, 9.8)), V, "right_ear", 33),
    Shape(polygon((mx(15.2), 6.2), (mx(8.2), 2.6), (mx(10.4), 6.4), (mx(15.0), 9.8)), V, "left_ear", 33),

    # the pig's nose
    Shape(ellipse(CX, 10.2, 1.8, 1.0), organ="nose", z=33),
] + right_arm[0] + left_arm[0] + right_leg[0] + left_leg[0]

STAMPS = [
    # brain
    Stamp(CX, 3, "☯", "pineal_gland"),
    Stamp(18, 6, "⟁", "hippocampus"), Stamp(22, 6, "⟁", "hippocampus"), Stamp(CX, 6, "♋", "anamnesis"),
    # a heavy brow over deep-set eyes
    Stamp(16, 7, "\\", zone=V), Stamp(17, 7, "\\", zone=V), Stamp(mx(16), 7, "/", zone=V), Stamp(mx(17), 7, "/", zone=V),
    Stamp(17, 8, "◖", "right_eye"), Stamp(18, 8, "◗", "right_eye"),
    Stamp(mx(18), 8, "◖", "left_eye"), Stamp(mx(17), 8, "◗", "left_eye"),
    # nostrils
    Stamp(19, 10, "●", "nose"), Stamp(21, 10, "●", "nose"),
    # the mouth: fangs at the corners, teeth between, tusks from the lower jaw, the tongue
    *[Stamp(x, 12, "▼" if x in (17, 23) else "▾", "teeths") for x in range(17, 24)],
    Stamp(18, 13, "▲", "teeths"), Stamp(22, 13, "▲", "teeths"),
    Stamp(19, 13, "◡", "tongue"), Stamp(20, 13, "◡", "tongue"), Stamp(21, 13, "◡", "tongue"),
] + right_arm[1] + left_arm[1] + right_leg[1] + left_leg[1]

SPLIT = {"pulmones", "viscera", "hippocampus", "teeths"}

WHERE = {"lungs": "pulmones", "teeth": "teeths"}

VISCERA = "ოჩშმნტჹცპჟϑქღყწჭსჲრძუფ"
TEXTURES = {
    "backbone": lambda c: "╬" if c.y % 2 == 0 else "║",
    "pulmones": lambda c: "≈",
    "heart": lambda c: "♥",
    "hepar": lambda c: "↺" if c.x % 2 == 0 else "↻",
    "spleen": lambda c: "∴",
    "paunch": lambda c: "∭",
    "viscera": lambda c: VISCERA[(c.x * 7 + c.y * 13) % len(VISCERA)],
    "genitories": lambda c: "♂",
    "cerebrum": lambda c: "∾" if (c.x + c.y) % 2 == 0 else "∿",
    "cerebellum": lambda c: "Ѳ",
    "nose": lambda c: "○",
}

BRIGHT = {"right_eye": 4, "left_eye": 4, "teeths": 4, "heart": 3, "nose": 3, "pineal_gland": 4,
          "cerebrum": 3, "backbone": 2}


def organ_layer(c):
    if c.stamped and c.organ in ("right_hand", "left_hand", "right_foot", "left_foot"):
        return 4                                        # the claws
    return BRIGHT.get(c.organ, 2)


def skin(c):
    ox, oy = c.out
    if c.depth == 1:
        if ox == 0 and oy == 0:
            return ":", 1
        if abs(ox) > 2 * abs(oy):
            return ("(" if ox < 0 else ")"), 2
        if abs(oy) > 2 * abs(ox):
            return ("^" if oy < 0 else "‿"), 2
        return ("/" if ox * oy < 0 else "\\"), 2
    if c.zone == T and 18 <= c.y <= 28:                 # ribs showing through a gaunt chest
        return ("═" if c.y % 2 == 0 else "-"), 1
    return ("~" if (c.x + c.y) % 2 else "·"), 1


GEARS = [
    ("headgear", 16, 0), ("eyewear", 0, 8), ("neckwear", 24, 15), ("outerwear", 15, 19),
    ("bodywear", 16, 23), ("underwear", 15, 27), ("belt gear", 15, 34), ("r. handwear", 0, 40),
    ("l. handwear", 29, 40), ("r. hold", 0, 45), ("l. hold", 33, 45), ("legwear", 16, 52),
    ("footwear", 16, 67),
]

GRIT = {"seed": 3}
