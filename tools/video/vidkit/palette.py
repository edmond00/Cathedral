"""The game's colours and font, for everything drawn outside the game.

Every value here is copied from `Config.Colors` in src/Config.cs, so a chart and the screen it sits
beside are painted from one palette. The visual identity allows three families and nothing else:
yellows, purples and greys. `check` enforces it.
"""

from __future__ import annotations

import colorsys


def _hex(r: float, g: float, b: float) -> str:
    return "#{:02X}{:02X}{:02X}".format(round(r * 255), round(g * 255), round(b * 255))


# Greys (Config.Colors: Black/White theme)
BLACK = _hex(0, 0, 0)
GRAY20 = _hex(0.2, 0.2, 0.2)
GRAY35 = _hex(0.35, 0.35, 0.35)
GRAY50 = _hex(0.5, 0.5, 0.5)
GRAY60 = _hex(0.6, 0.6, 0.6)
GRAY75 = _hex(0.75, 0.75, 0.75)
GRAY85 = _hex(0.85, 0.85, 0.85)
WHITE = _hex(1, 1, 1)
LIGHT_PURPLE_GRAY = _hex(0.8, 0.8, 0.9)

# Yellows: the game's headers, highlights, hover backgrounds
BRIGHT_YELLOW = _hex(1, 1, 0)
GOLD = _hex(1, 0.85, 0.2)
LIGHT_YELLOW = _hex(1, 1, 0.6)
MEDIUM_YELLOW = _hex(0.7, 0.7, 0)
DARK_YELLOW_GREY = _hex(0.6, 0.6, 0.2)
DARK_YELLOW = _hex(0.4, 0.4, 0)
BRONZE = _hex(0.72, 0.52, 0.18)                       # CoinCopper: the darkest yellow the game uses

# Purples: wounds, danger, the enemy
DARK_PURPLE = _hex(0.3, 0, 0.45)
PURPLE = _hex(0.55, 0, 0.75)
BRIGHT_PURPLE = _hex(0.72, 0, 1)
LIGHT_PURPLE = _hex(0.85, 0.55, 1)

BACKGROUND = BLACK
TEXT = GRAY85
TEXT_DIM = GRAY50
ACCENT = GOLD
ACCENT_2 = LIGHT_PURPLE

# Categorical series, in the order a chart should take them: the two families alternate so neighbours
# never share a hue, and every step is far enough in lightness to survive the dither's six levels.
SERIES = [GOLD, LIGHT_PURPLE, MEDIUM_YELLOW, PURPLE, GRAY75, BRONZE, BRIGHT_PURPLE, DARK_YELLOW_GREY, GRAY50, DARK_PURPLE]

# The terminal's font (GlyphAtlas: Consolas, DejaVu Sans Mono as the fallback).
FONT = "Consolas"
FONT_FALLBACK = "DejaVu Sans Mono"


def rgb(color: str) -> tuple[int, int, int]:
    c = color.lstrip("#")
    return int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16)


def family(color: str) -> str:
    """'grey', 'yellow', 'purple' or 'off-brand'."""
    r, g, b = (v / 255 for v in rgb(color))
    h, l, s = colorsys.rgb_to_hls(r, g, b)
    if s < 0.15 or l < 0.06 or l > 0.97:
        return "grey"
    deg = h * 360
    if 30 <= deg <= 72:
        return "yellow"
    if 255 <= deg <= 305:
        return "purple"
    # a desaturated colour that only leans a little is still read as a grey
    return "grey" if s < 0.25 else "off-brand"


def check(*colors: str) -> None:
    """Raises if any colour falls outside the yellow / purple / grey identity."""
    bad = [c for c in colors if family(c) == "off-brand"]
    if bad:
        raise ValueError(f"off-brand colour(s) {bad}: the video identity allows yellows, purples and greys only")


check(*SERIES, TEXT, TEXT_DIM, ACCENT, ACCENT_2)
