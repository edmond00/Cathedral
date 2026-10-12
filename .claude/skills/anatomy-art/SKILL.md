---
name: anatomy-art
description: Create the ASCII anatomy art for a body — the aligned grids under assets/art/body/<name>/ (ascii_art, layer_map, organs, visual_zones, wounds, gears) that the protagonist screen draws and clicks on — by generating them from labelled shapes with tools/art/body_gen.py, rendering previews with tools/art/preview.py, and iterating on the PNG. Use whenever asked for a new creature's or species' body art (a lizard, a bird, a goblin, an elf, a rat…), to rework an existing one, to show body-art proposals, or to change how generated art looks. Carries the file format, the procedure, how to share proposals as PNGs, and the art direction: anatomical proportions, the retro/glitch grit, and the organ glyph vocabulary.
---

# Anatomy art

An agent cannot draw by typing characters: it reads text as tokens, not shapes, and cannot see
whether a column of `=` lines up. So **never compose the art as text.** Describe the body as
labelled shapes, let `tools/art/body_gen.py` turn them into every grid at once, render the result
to a PNG with `tools/art/preview.py`, **Read the PNG**, and correct the shapes. You can see images;
that is the loop.

The human (`assets/art/body/human`) and the beast (`assets/art/body/beast`) were drawn by hand
from converted photographs. Generated art is held to the look of those two.

## What a body art folder is

Six text grids of identical size, aligned cell for cell, plus five CSVs. `░` is empty.

| file | holds |
|---|---|
| `ascii_art.txt` | the glyphs drawn |
| `layer_map.txt` | each cell's brightness, `0`–`4`, an index into `layer_colors.csv` (greys, 0.1 to 0.9) |
| `organs.txt` | the art, with every organ cell replaced by its organ part id from `organs.csv` |
| `visual_zones.txt` | the art, with every body cell replaced by its zone digit from `visual_zones.csv`. **A cell is drawn only if it has a zone** (`BodyArtData.IsBodyCell`) |
| `wounds.txt` | the art, with one cell per wound in `wounds.csv` replaced by its id: where that wound's glyph is shown |
| `gears.txt` | the art, with each gear alias from `gears.csv` written in capitals; `GearAnchorData` anchors on the middle of the word |

Things the format implies, each of which has bitten:

- **No ASCII letter or digit in the art.** The map files are the art with ids written over it, so
  an `a` or a `3` left in the art reads as an organ, a zone or a wound. The generator refuses them.
- **Width at most ~66.** `BodyArtViewer.PanelX` is 68; anything wider runs into the side panel.
- **A wound needs a cell of its own.** The human gives each eye two wounds (black eye, pierced eye),
  so an eye must be two cells, or one wound has nowhere to go.
- **Front view: the body's right is the viewer's left.** The human's right arm is drawn on the left.
- **An organ may be in several pieces** — the human's lungs either side of its spine — but only on
  purpose. The generator reports any split organ the spec did not declare.

Which organs exist is not the art's decision. `organs.csv`, `visual_zones.csv` and `wounds.csv` come
from an anatomy, and a species picks its art folder with `Species.ArtFolderPath`. **Reusing the
human's or the beast's lists needs no code**: an elf, a goblin or an ogre is a human body, a lizard,
a rat or a bear a beast one. Claws on a goblin are then drawn on the human's hands, not organs. A
body whose organs differ (wings, a tail with its own wounds, a crop) needs a new `IAnatomyFactory`
in code, and that is a rule change: invoke the `manual` skill too.

## The tools

```
tools/art/body_gen.py      shapes -> the six grids + the five CSVs (copied from BASE), and the checks
tools/art/preview.py       a folder -> a PNG drawn as BodyArtViewer draws it
tools/art/specs/*.py       one spec per body
```

Worked examples, both passing every check and both shipped (`assets/art/body/goblin`,
`assets/art/body/lizard`) — **copy the closer one** rather than starting cold:

| spec | shows |
|---|---|
| `specs/goblin.py` | front view on the human's lists; anatomical proportions, contrapposto, muscles as swellings, fingers and toes with claws |
| `specs/lizard.py` | seen from above on the beast's lists; splayed limbs under the trunk, a curling tail, organs split by the spine |

A side view (the beast's convention) has no example now; it was tried for the lizard and dropped
for the view from above. Draw the far-side limbs behind the trunk with `far=True`, one shade darker.

```powershell
$env:PYTHONPATH = "tools\art"
python tools\art\body_gen.py tools\art\specs\goblin.py <out folder>     # exit 1 on any failed check
python tools\art\preview.py <out folder> <out>.png                         # art | organs | zones
python tools\art\preview.py <out folder> <out>_art.png --panels art --cell 22
```

(bash: `PYTHONPATH=tools/art python tools/art/body_gen.py …`.) Python 3 with Pillow; nothing else.

### Writing a spec

Coordinates are in cells, x right, y down; a cell is a unit square. The docstring of
`body_gen.py` is the reference; in short:

- `W`, `H`, and `BASE` — the folder whose CSVs are reused (`assets/art/body/human` or `…/beast`).
- `SHAPES` — `Shape(fn, zone, organ, z, far)`, with `fn` from `ellipse`, `capsule` (a thick polyline
  whose radius varies along it — limbs, tails, necks) and `polygon` (ears, a pelvis, shoulders). A
  shape with a zone is silhouette; a shape with only an organ marks that organ, clipped to the
  silhouette beneath it. Higher `z` is in front.
- `STAMPS` — `Stamp(x, y, glyph, organ, zone)`: single cells placed by hand. **Use them for anything
  smaller than about three cells**: eyes, nostrils, teeth, brain parts, claw tips. A shape that
  small rasterises to nothing or to a blob.
- `TEXTURES` — organ part → glyph function, for the organs drawn with their own glyphs.
- `skin(cell)` and `organ_layer(cell)` — the glyph and brightness of everything else. With `GRIT`
  on, `skin` is replaced by the noise ramp and only decides the clean version.
- `SPLIT` — organ parts allowed in pieces. `WHERE` — `wounds.csv` "where" words that are not organ
  parts (the human's say `lungs` and `teeth`; the parts are `pulmones` and `teeths`).
- `GEARS` — `(alias, x, y)` for each row of `gears.csv`, where its label is written. Copy the
  human's placement pattern: labels may run off the body.
- `GRIT` — the retro look; see the art direction below.

A `Cell` handed to `skin`/`organ_layer`/textures carries `x`, `y`, `zone`, `organ`, `far`, `depth`
(1 on the edge, growing inward), `out` (which way the outside lies), `up`/`down` (body cells above
and below) and `stamped`.

### What the checks mean

`body_gen.py` exits non-zero and prints a `!` line for each problem. **Fix every one before showing
anything.**

| message | usual cause |
|---|---|
| `organ part X has no cell` | something in front covers it (a limb drawn over the trunk), or it is smaller than a cell — make it a stamp, or move the occluder under (`z` below the trunk's) |
| `organ part X is in pieces` | a thin line broken by the warp or a glitched row (thicken it: radius ≥ 0.6), a stamp not touching its shape, or a genuinely split organ that belongs in `SPLIT` |
| `wound N … has no cell to sit on` | the organ is one cell and two wounds want it — widen the organ, or the "where" word needs a `WHERE` entry |
| `glyph … reads as an id` | an ASCII letter or digit in the art — pick a symbol |
| `zone X has no cell` | a limb or head shape missing its zone |

## The procedure

1. **Settle three things with the user first**: which body list (human or beast — or a new
   anatomy, which is code), which view (front like the human, side like the beast, or from above),
   and what the creature is like. The view is a convention on the protagonist screen; say so if it
   departs from the human and beast.
2. **Copy the closest spec** to `tools/art/specs/<name>.py` and rewrite the shapes. Lay the body
   out on paper first, in cells: total height, head height, where the joints are.
3. **Generate into the scratchpad**, never into `assets/` while iterating.
4. **Render and Read the PNG** — the three-panel one for the maps, the art panel alone at
   `--cell 22` for the look. Judge it as a picture: proportions, pose, whether each organ reads.
   Check the organ panel for anything hidden or smeared and the zone panel for limbs that run into
   the trunk. Iterate. Two to four passes is normal.
5. **Share proposals as PNGs.** Every proposal the user sees is a rendered image, never the text
   grid. Give the path of the art panel and of the three-panel image, and send them as files if a
   file-sending tool is available. When offering alternatives (seeds, poses), render each one.
   Say what changed and what is still weak — the hands and feet usually are.
6. **When the user approves**, generate into `assets/art/body/<name>/`, point the species at it
   (`ArtFolderPath`), and check it in the game: the protagonist screen is reachable through the
   main menu (`click menu Protagonist`; see `verifying`), and `playtest` covers handing it to the
   user. The preview mimics `BodyArtViewer` but is not it.
7. **Commit the spec with the art.** The spec is the source; the grids are its output. Regenerating
   a body is the way to change it.

### Polishing by hand

The grids are plain text and can be touched up in an editor, but every map file is a copy of the
art: an edit to `ascii_art.txt` alone leaves the other five out of step, and the next generation
overwrites it. Prefer fixing the spec. If hand polish is wanted, make it the last step and say that
the spec no longer reproduces the folder.

## Art direction

What the user asked for, in their words: **retro and glitchy, anatomical and realistic — not
clean, not cartoon.** Generated art must sit beside the hand-made human and beast without looking
like a different game.

### Anatomy, not cartoon

The first generated goblin was rejected as too cartoonish and the second accepted. What changed:

- **Real proportions.** A man's head is an eighth of his height; a stylised humanoid's at most a
  fifth. A head a third of the body is a cartoon.
- **The body's structure shows in the silhouette.** Build limbs from the muscles, not as tubes:
  deltoid, biceps and forearm swelling and tapering to the wrist; thigh, knee and calf to the ankle.
  A trunk is a rib cage over a waist over a pelvis, not one ellipse. Shoulders slope into the neck.
- **A real stance.** Contrapposto — weight on one leg, straight under the body, the other knee
  eased — or a hunch. Perfect left–right symmetry reads as a diagram.
- **Features sized to the head, not exaggerated.** Ears swept back rather than spread like wings;
  eyes deep-set under a brow.
- **Limbs join under the trunk.** Draw arms and legs at a `z` below the trunk so they emerge from
  it; drawn over it they hide the organs and fuse into the pelvis.
- **From above, limbs splay** with bent elbows and knees and fanned digits; from the side, the far
  limbs sit behind the trunk, one shade darker (`far=True`).

### The grit: retro and glitchy

The hand-made art came from converted photographs: ragged edges, noisy brightness, textures that
drop out. `GRIT = {"seed": n}` reproduces that, and **is on for every body**. Without it the art is
drawn clean, which is only useful for checking the shapes.

| key | does | default |
|---|---|---|
| `seed` | another variant, equally gritty — render two or three and pick | 1 |
| `warp` | how far, in cells, the silhouette's edge wanders | 0.55 |
| `glitch` | bands of rows shifted sideways by one cell | 3 |
| `noise` / `layer_noise` | glyph-ramp noise / independent brightness noise, as a photo's layer map | 0.30 / 0.25 |
| `dropout` | share of organ cells breaking up into the skin's noise | 0.12 |
| `corrupt` | share of body cells replaced by a block glyph (`▒▓¦╌¬`) | 0.02 |

The user approved these levels. The warp and the glitched rows act on the sampling, before any grid
is derived, so they distort the maps exactly as they distort the art — stamps ride their glitched
row. **Glitch shifts stay at one cell**: a two-cell shift cuts a one-wide organ (the backbone) in
two. The skin ramp is `.·:~-=+*≡`, lit from the upper left.

### The organ vocabulary

The same organ is drawn with the same glyphs in every body, so a player who has learned the human
can read a lizard. Take these from the human and the beast; add to the table only for an organ
neither has.

| organ | glyphs | | organ | glyphs |
|---|---|---|---|---|
| backbone | `▇⑆` (beast) or `╬║` (human) | | heart | `♥` |
| lungs | `▞/` (beast) or `≈` | | liver | `↺↻` |
| stomach | `∭` | | spleen | `≈` (beast) or `∴` |
| gut | Georgian letters `ოჩშმნტჹცპჟϑ…` | | genitals | `♂` |
| cerebrum | `©`, or `∾∿` over a large brain | | cerebellum | `Ѳ` |
| anamnesis | `♋` | | hippocampus | `⟁` |
| pineal gland | `☯` — on a lizard, the parietal eye | | eyes | `◉`, `◐◑`, or `◖◗` over two cells |
| teeth, fangs | `▼▾` above, `▲` below | | claws | arrows pointing the way the digit points `↓↙↘←→↖↗` (`↡` on the beast) |
| nose | `○` with `●` nostrils (a pig's), `∘` | | tongue | `◡`; a forked one `<` or `⋎` |

Brightness: eyes, teeth, claws and the pineal at layer 4; the heart, nose and brain at 3; organs
at 2; skin 1–3 from the grit; far-side limbs one lower.

### Glyphs

Use only glyphs FreeMono or DejaVu Sans Mono carry (`assets/fonts/`); the preview falls back the
same way the game does, so a glyph that renders in the preview renders in the game. Never `░` or a
space inside the body (both are skipped), and never an ASCII letter or digit (see the format).

## Previews

`preview.py` draws each glyph in its layer's grey on black, body cells only, in FreeMono with the
DejaVu fallback — as `BodyArtViewer` does — and beside it the organ map (tinted, labelled with ids)
and the zone map. Cells are square by default; the game's cell is the window divided by its 100×100
grid, so `--aspect 0.56` shows a 16:9 fullscreen. The game draws glyphs 1.2× their cell
(`GlyphScale`), so they look a little bolder there. The tints in the map panels are a debugging aid
and are not held to the game's palette.
