"""Renders a body art folder (assets/art/body/<name>) to a PNG, the way BodyArtViewer draws it.

    python tools/art/preview.py assets/art/body/human out.png [--cell 14] [--aspect 1.0]

Three panels side by side:
  art     - ascii_art.txt in FreeMono, each glyph coloured by its layer_map.txt grey
            (layer_colors.csv), on black. Only body cells are drawn (visual_zones.txt id >= 0),
            exactly as BodyArtViewer skips the rest.
  organs  - the same art, every organ cell tinted by its organ, labelled with its organ part
            id from organs.csv, so a smeared or missing organ shows at a glance.
  zones   - visual_zones.txt, one tint per zone.

--aspect is cell height / cell width. The game's cells are the window divided by its 100x100
grid, so it depends on the window; 1.0 is a square window.
"""
import argparse, csv, colorsys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONT = ROOT / "assets/fonts/FreeMono.ttf"
FALLBACK = ROOT / "assets/fonts/DejaVuSansMono.ttf"
EMPTY = {"░", " ", ""}


def grid(path):
    lines = path.read_text(encoding="utf-8").splitlines()
    return lines


def cell(lines, x, y):
    return lines[y][x] if y < len(lines) and x < len(lines[y]) else " "


def tint(i, n, light=0.45):
    r, g, b = colorsys.hls_to_rgb((i / max(n, 1)) % 1.0, light, 0.75)
    return int(r * 255), int(g * 255), int(b * 255)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("folder")
    ap.add_argument("out")
    ap.add_argument("--cell", type=int, default=14, help="cell width in pixels")
    ap.add_argument("--aspect", type=float, default=1.0, help="cell height / width")
    ap.add_argument("--panels", default="art,organs,zones", help="which panels, comma-separated")
    a = ap.parse_args()
    d = Path(a.folder)

    art = grid(d / "ascii_art.txt")
    layers = grid(d / "layer_map.txt")
    organs = grid(d / "organs.txt")
    zones = grid(d / "visual_zones.txt")
    W = max(len(l) for l in art)
    H = len(art)

    colors = {}
    with open(d / "layer_colors.csv", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            colors[row["Layer"]] = tuple(int(float(row[k]) * 255) for k in ("ColorR", "ColorG", "ColorB"))

    organ_info = {}
    with open(d / "organs.csv", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            organ_info[row["id"]] = row
    organ_names = sorted({r["organ"] for r in organ_info.values()})

    cw = a.cell
    ch = round(cw * a.aspect)
    font = ImageFont.truetype(str(FONT), size=round(min(cw * 1.6, ch * 1.15)))
    fallback = ImageFont.truetype(str(FALLBACK), size=font.size)

    def glyph_font(c):
        # FreeMono lacks some of the art's glyphs; the game falls back to DejaVu the same way.
        return font if font.getmask(c).getbbox() else fallback

    gap = cw * 2
    label_h = ch * 3
    panels = a.panels.split(",")
    img = Image.new("RGB", (W * cw * len(panels) + gap * (len(panels) - 1), H * ch + label_h), (0, 0, 0))
    dr = ImageDraw.Draw(img)

    def is_body(x, y):
        z = cell(zones, x, y)
        return z.isdigit()

    def put(px, py, c, fg, bg=None):
        if bg:
            dr.rectangle([px, py, px + cw - 1, py + ch - 1], fill=bg)
        if c in EMPTY:
            return
        f = glyph_font(c)
        dr.text((px + cw / 2, py + ch / 2), c, font=f, fill=fg, anchor="mm")

    for slot, title in enumerate(panels):
        p = ("art", "organs", "zones").index(title)
        ox = slot * (W * cw + gap)
        dr.text((ox, ch), title, font=fallback, fill=(200, 200, 120))
        for y in range(H):
            for x in range(W):
                if not is_body(x, y):
                    continue
                px, py = ox + x * cw, label_h + y * ch
                c = cell(art, x, y)
                fg = colors.get(cell(layers, x, y), (0, 0, 0))
                if p == 0:
                    put(px, py, c, fg)
                elif p == 1:
                    oid = cell(organs, x, y)
                    if oid in organ_info:
                        bg = tint(organ_names.index(organ_info[oid]["organ"]), len(organ_names), 0.25)
                        put(px, py, oid, (255, 255, 255), bg)
                    else:
                        put(px, py, c, tuple(v // 2 for v in fg))
                else:
                    z = int(cell(zones, x, y))
                    put(px, py, c, (230, 230, 230), tint(z, 7, 0.3))

    img.save(a.out)
    missing = [i for i in organ_info if not any(i in l for l in organs)]
    print(f"{W}x{H} grid -> {a.out}  ({img.width}x{img.height}px)")
    if missing:
        print("organ ids in organs.csv with no cell:", " ".join(missing))


if __name__ == "__main__":
    main()
