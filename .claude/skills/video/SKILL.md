---
name: video
description: Make a narrated video about the game — a survey or analysis summarised for the developer, or technical/promotional material to share — from gameplay recorded by an agent playing the game (`--record`), manim animations (charts, location graphs, system diagrams), a local Piper voice-over and the game's own music, all in the game's visual identity (yellow/purple/grey, Consolas, Bayer dither). Use whenever asked for a video, a clip, a trailer, an explainer, a recording of gameplay, an animated chart or diagram of game content, or to turn an analysis into something watchable. Carries the record mode's command rules, the storyboard schema, the brand rules and the verification procedure.
---

# Making a video

Everything lives in `tools/video/`; one video is one folder under `tools/video/productions/<name>/`.
The worked example is `productions/cities/` ("How cities work") — **copy it** rather than starting
from nothing.

```
productions/<name>/
  storyboard.json   what plays, in order, and what is said over it         (committed)
  scenes.py         the manim scenes, built on vidkit.manimkit.BrandScene   (committed)
  <name>.cli        the gameplay, as player commands for --record           (committed)
  build/            recordings, data, voice, segments, the final mp4         (ignored; all regenerated)
```

```bash
tools/video/.venv/Scripts/python tools/video/make_video.py build tools/video/productions/<name>/storyboard.json --sheet
```

A build records any missing recording, regenerates missing data, voices every line, renders every
segment, mixes the sound and runs the game's dither over the whole picture. Every step is cached:
change one line of narration and only that line is re-voiced and that segment re-rendered.
`--only 3,5` rebuilds those segments and reuses the rest; `--preview` builds at half size.

**Run builds and recordings with `run_in_background`.** They take minutes, the game window is hidden,
nothing plays aloud, and the developer keeps working. You are notified when they finish.

## Setup (once per machine)

```powershell
powershell -ExecutionPolicy Bypass -File tools/video/setup.ps1
```

Creates `tools/video/.venv` (manim, Piper, a bundled ffmpeg, numpy/scipy) and fetches three Piper
voices into `tools/video/voices/`. Nothing system-wide; no LaTeX (do not use `MathTex`/`Tex`). The
game must be built (`dotnet build`) — the pipeline runs `dotnet run --no-build`.

## 1. Recording gameplay: `--record`

`--record <dir>` is the CLI with a camera. A script — or an agent on stdin — plays; the run is filmed
with a drawn cursor that travels to each thing clicked, and a timeline says what happened when.

```bash
tools/video/.venv/Scripts/python tools/video/make_video.py record my.cli build/rec_x \
    --timeout 3600 --flags "--skip-childhood --seed 42 --no-encounters --allow-reentry --start-at city"
tools/video/.venv/Scripts/python tools/video/make_video.py inspect build/rec_x     # what to cut
```

(or a `"recordings"` entry in the storyboard, which records itself when its directory is missing).

What `--record` changes about a run:

| | |
|---|---|
| **Hidden** | no window unless `--record-visible`; the OS mouse is ignored, so the developer's mouse never reaches it |
| **Silent** | no audio device. The music the game composes is captured to `music.mid` instead |
| **CPU** | the model runs on the CPU. `--gpu` overrides — **only if the user asks**: the GPU has crashed this machine's desktop (see `playtest`) |
| **No save** | like `--cli`, it never touches the player's save |
| **Size** | `--record-size 1440x1080` (default; the game's grid is 100x100, so 4:3 fills a 1080p frame at 1:1), `--record-fps 30` |

Output: `game.mkv` (real time — frame *n* is the screen at *n*/fps on the run's clock, stalls
included), `timeline.jsonl`, `music.mid`, `sfx/click.wav` (the game's own click), `manifest.json`.

### Only what a player can do

Every click goes through the window's real pointer path — hover, then press, at a pixel found by the
same hit-test the game answers it with (popup rows, dialogue options, the sphere's ray-pick). **A
target that cannot be located on screen is refused, never performed some other way.**

| kind | commands |
|---|---|
| pointer (animated, clicked) | `click keyword/action/option/skill/fighter/end-turn/engage/button/continue/menu/world/moon/sky/arrow/companion-death/end-run/cell`, `click humor <queue>` and `click die <n>` (spending humors on a settled roll; `dice` shows it), `choose <n>` (scrolls a long list to the row first), `travel here/<name>/<vertex>/neighbour/back`, `travel-go`, `point moon`, `scroll up/down [n]`, and the presses `advance` makes |
| keyboard | `key <name>`, `pause` |
| menus and screens | `pause` (Escape: opens the menu), `click menu <label>` (Continue, Protagonist, Settings…), `click tab <name>` and `click back` on the protagonist screen, `click element <id>` for any named control (settings rows, trade, work). `elements` lists the ids on screen |
| directing the film | `clip <name>` … `clip end` (a span to cut), `mark <label>`, `note <text>`, `hold <secs>` (let the shot breathe), `cursor rest/hide/show` |
| passive | `wait`, `state`, `regions`, `dump`, `dice`, `elements`, `world`, `destinations`, `inspect`, `expect*`, `history`, `quit` |
| **refused** (fails the run) | `strategy`, `goal`, `observe`, `fight-*`, `wound`, `cripple`, `starve`, `clock`, `save`, `crash-report`, and the shortcuts `manage`, `select`, `routines` (open those screens through the menu instead) |

So **outcomes are not forcible**: the dice roll as the seed rolls them. Shape the situation with
**starting flags** instead — they are allowed and are the intended lever: `--seed`, `--start-at`,
`--start-area`, `--location-type`/`--location-id`, `--period`, `--grant-item`, `--grant-mm`,
`--fill-party`, `--spawn-beast`, `--start-fight`, `--npc-affinity`, `--world-variant`… (all in the
`verifying` skill). A failed roll is often worth keeping and narrating; if the story needs the other
outcome, try another seed and keep the take you want.

`--allow-reentry` is needed for `travel here` right after spawning (a player arrives by walking; a
script cannot otherwise enter the spawn cell). `--start-at city` spawns on a real generated city,
farms and history around it — prefer that to `--location-type` when the map is in shot.

### Design the protagonist for the shot

A protagonist rolled at random mostly fails, refuses, or has nothing to offer — honest, and useless
for showing a system. **Decide what the footage must show, then build the character who can get
there**, with starting flags (all applied when the protagonist is accepted, before anything derives
from them):

| flag | what it buys on screen |
|---|---|
| `--organs all=3,encephalon=5,viscera=5` | organ part scores, by part, organ, body part or `all` (ignores the point budget). **Encephalon** sets the noetic points — thoughts per phase, encephalon ÷ 3 rounded up, so 5 everywhere in it gives 8. **Viscera** sets how many humors may be spent on one roll. Others decide which modi mentis can grow, and tool proficiency (hands). An unknown id prints the full list |
| `--humors hepar=voluptas,pulmones=juvenescence,spleen=laetitia` | fills humor queues (paunch, hepar, spleen, pulmones, or bare names for all four). The rescuers: **voluptas** any die → 6, **juvenescence** +2, **blood** +1, **laetitia** 5 → 6, **zen** reroll any. Avoid black bile (unusable, and lethal when every queue is full of it) |
| `--grant-mm stealth:4,architecture:2` | modi mentis into memory, each at its own level — the ways of thinking and acting the scene needs. `--mm` fills the rest of memory at random (a long list, which `choose` scrolls) |
| `--grant-item axe,rope` | tools for the tool-gated verbs (dig, mine, fish, cut_wood, break, the climbs) |
| `--fill-party`, `--npc-affinity`, `--spawn-beast`, `--start-area`, `--period` | companions, a warm reception, a beast in the room, the right room, the right hour |

Say in the narration if the character was built for the shot when it matters (a "lucky" streak is
otherwise misleading in promotional material).

### Getting through a scene — the levers a player has

- **Think more, act once.** Each keyword click → Think spends one noetic point and can add an action
  to the list. With 8 points, think on several words (or one word with several modi mentis) before
  acting: more options, and a better one among them.
- **Choose the way of thinking for the goal.** The thinking modus mentis decides *what* is wanted
  (Vantage proposes heights and views, Ripelore food, Rhetoric talk); pick one whose nature fits.
- **The acting mind can refuse** ("I won't go to the chandler's lane"). That costs a point, not the
  scene: think again, with another modus mentis or another word.
- **A failed roll is not final.** Read it with `dice` (the faces, SUCCESS/failing, each queue's humor
  and which dice it applies to), then `click humor <queue>` and `click die <n>`, as many times as the
  viscera allow, then `click continue`. Spend the humor on camera — it is one of the game's best
  moments to show.
- **Observe** instead of Think looks closer and lights new words; **Use Tool** needs a carried item.
- When nothing on screen leads anywhere, `click button` (LEAVE / CONTINUE) moves the story on.

### Live takes: decide as you go (preferred with the real model)

A pre-written script cannot react to what the model writes. A **live take** films while you send
commands a batch at a time and read the answers between batches, exactly like a player:

```bash
make_video.py live build/rec_x --flags "--skip-childhood --seed 42 --start-at city --organs …"   # run_in_background
make_video.py send build/rec_x "wait mode MainMenu 900" "click menu New" … "regions"
make_video.py send build/rec_x dump                       # read the prose before choosing
make_video.py send build/rec_x "click keyword gaps" "choose 0" "regions"
make_video.py send build/rec_x quit
```

`send` blocks until the game has worked through the batch (long `advance` timeouts still apply) and
prints the driver's answers. The time you spend deciding is cut out of the footage automatically
(the gaps between batches), and the model's waits are fast-forwarded, so think as long as you like.
End every batch that changes the screen with `regions` (and `dice` on a roll). Put `clip` markers
around the stretches worth keeping as you go. A live take cannot be regenerated by a build — mark it
`"live": true` in the storyboard's recordings, with the flags it was made with.

### Writing the script

**Invoke `verifying` first and copy a known-good sequence** (`productions/cities/city.cli`, or any
`cli/verb/*/success.cli` minus its `strategy`/`goal` lines). Composing a narration sequence from the
docs stalls on preview boxes — `advance` drains them, and is needed before the first keyword and
again before `click button` to leave.

Direct it like footage: `hold 2`–`3` after anything the viewer must read, `clip <name>` around every
span the storyboard will use, `cursor rest` before a long read so the arrow is not on the text.

**Record with the real model — never `--playground`.** A recording shows the game as a player has it,
and a player reads prose the model wrote; `--playground` assembles placeholder text from scene
descriptions and is for tests only. On the CPU a passage takes a minute or so: give every `wait` and
`advance` a long timeout (900 s), start with `wait mode MainMenu 900` (the model loads before the
menu), and pass a generous `--timeout` (3600). Do not shorten the waits by cheating — **the cut
fast-forwards them**: the recorder logs every span spent waiting on the model (`model-busy` /
`model-idle` in the timeline), and a game segment plays those at ×8 with a small `>> x8` in the corner.
Tune with `"fast_forward": {"speed": 8, "min": 1.5}` in the storyboard, or per segment
(`"fast_forward": false` to show a wait at its real length).

Because the model writes the prose, **the footage is only known after the take**: record first, read
the take (`inspect`, a contact sheet, `grep '^\[cli\]' game.log` for what was clicked), and only then
write the narration over the game segments. Index-based commands (`click keyword 0`, `click action
0`, `choose 0`) survive whatever the model writes.

### Checking a take

```bash
tools/video/.venv/Scripts/python tools/video/make_video.py inspect build/rec_x
tools/video/.venv/Scripts/python tools/video/make_video.py sheet build/rec_x/game.mkv build/rec_x/sheet.png
```

Then **Read the sheet** (and single frames: `ffmpeg -ss <t> -i game.mkv -frames:v 1 f.png`). `inspect`
lists modes, clicks, clips and any `REFUSED` line; `grep '^\[cli\]' build/rec_x/game.log` has the
driver's own account. A recording that refused a command still writes everything — and the manifest
says `"failed": true`.

## 2. The storyboard

```json
{
  "title": "How cities work",
  "output": "build/cities.mp4",
  "size": [1920, 1080], "fps": 30,
  "dither": {"animation": {"levels": 4, "scale": 2}, "game": {"levels": 6, "scale": 1}},
  "fast_forward": {"speed": 8, "min": 1.5},
  "voice": {"name": "en_GB-alan-medium", "speed": 0.95, "grit": 0.35},
  "music": {"export": {"mood": "Creation", "tracks": 4}, "level_db": -34, "duck": 0.65},
  "clicks": {"gain": 0.5},
  "prepare": [{"creates": "build/data/city_plain.json", "game": "--scene-export city --ids 0-59 --out {creates}"}],
  "recordings": {"city": {"script": "city.cli", "dir": "build/rec_city", "flags": "--skip-childhood --seed 42 …", "timeout": 3600}},
  "manim_file": "scenes.py",
  "segments": [ … ]
}
```

| segment `type` | fields |
|---|---|
| `title` | `params`: `title`, `subtitle`, `kicker` |
| `card` | `params`: `heading`, `lines`, `color` |
| `manim` | `scene` (a class in `manim_file`), `params` (anything; strings starting `./` become absolute paths, lists too) |
| `game` | `recording`, then `clip` (name) or `mark` + `length` or `from`/`to` (seconds); `pre`/`post` to widen, `speed`, `fast_forward` (×N for model waits, or false), `zoom: {"factor": 1.5, "center": [x, y]}` (recording pixels), `hover_ticks` |
| all | `vo` (the narration), `min` (seconds), `fade` (default true), `voice` (override) |

**A segment lasts** the longest of: its picture, its narration plus a breath either side, its `min`.
A game clip holds its last frame if the narration runs on; a manim scene is told the narration's
length and paces itself (`self.target`, `self.beat()`, `self.fill_to_target()`).

`music`: `{"export": {"mood": …}}` has the game's own composer write a bed for this video (real time,
run in parallel with rendering, cached) — moods are the `MusicMoodState` presets: `Neutral`,
`Creation`, `Childhood`, `WorldView`, `Tavern`, `Battle`, `DarkDungeon`, `Lament`. Or `{"recording":
"city"}` for what played during a take, or `{"midi": "path.mid"}`. It is rendered by `vidkit.synth`
(no soundfont: a poorer, older instrument than the game's GS synth, on purpose) and ducked under the
voice.

## 3. Animations (manim)

```python
from vidkit.manimkit import *

class Sizes(BrandScene):
    def construct(self):
        self.header("size: one, two or three", "averages over 150 generated cities")
        chart = self.hbar_chart(["size 1", "size 2", "size 3"], [7, 10, 13], [GRAY60, MEDIUM_YELLOW, GOLD])
        self.play(*self.grow(chart))
        self.fill_to_target()
```

**Every chart title states what one bar is** ("in one city, on average", "counted over 60 generated
cities"), and subtitles must stay true for everything shown under them.

`BrandScene` gives: `text` (Consolas), `header` (spaced capitals between dotted rules, like the main
menu), `frame_box`, `hbar_chart` + `grow`, `node` + `edge` + `untangle` (graphs whose labels never
overlap), `beat`, `fill_to_target`, `self.params`, `self.target`. Smoke-test a scene by rendering its
last frame, and **Read it** — layout bugs (overlaps, text off the frame) only show in the image:

```bash
cd tools/video/productions/<name>
VID_TARGET_SECONDS=3 VID_PARAMS='{}' PYTHONPATH=../.. ../../.venv/Scripts/python -m manim render scenes.py MyScene -r 1280,720 --fps 10 --format png -s --media_dir <scratch> --disable_caching
```

### Numbers come from the code

Never draw a number you read in the manual or `design/`, or remember. Generate it:

| source | gives |
|---|---|
| `--scene-export <key> [--biome b] [--ids a-b] --out f.json` | settled scenes as built: sections, areas and kinds, paths and doors, residents and where they live |
| the `audits` skill | thirteen reports over content (verbs, items, NPCs, dialogue, buildings, world variants, history) |
| `--verb-probe`, `--mm-grant-csv`, `--mm-reach-csv` | who can do what, where; what teaches what |
| reading the generator | the rules a diagram animates — port them (`productions/cities/scenes.py` `Sprawl` runs `SettlementSprawl`'s rules on a hex grid) and say in the docstring which file they come from |

Put the command in the storyboard's `prepare` so the data regenerates when the game changes.

## 4. The visual identity — non-negotiable for anything shared

- **Colours: yellows, purples, greys only**, from `vidkit.palette` (every value is `Config.Colors`).
  `BrandScene` fails the render if any mobject is off-brand; `palette.check()` for anything else.
  Yellow is what matters (gold for headers and the main series), purple is what threatens or opposes
  (borders, enemies, the second series), grey is structure. Never green, red or blue — even for "good"
  and "bad".
- **Font: Consolas** (the terminal's). Titles in spaced capitals.
- **The dither**: every segment passes through the game's own Bayer 8x8 (`vidkit.dither`, transcribed
  from `PostProcessRenderer`). Game footage gets the game's own setting (six levels, one-pixel cells) —
  lattice-aware, so the footage comes through unchanged and only fades and the cursor take the
  pattern. **Animations get it harder** (four levels, two-pixel cells by default): clean vectors at
  the game's own setting read as too polished beside the footage. Do not dither inside scenes; tune
  with `"dither": {"animation": {…}}`.
- **Music sits well under the voice**: `level_db` −34 and `duck` 0.65 by default. The developer found
  −29 too loud; do not raise it without being asked.
- **Voice**: `grit` 0.35 is the house setting (band-limited, lightly crushed, a noise floor, a small
  room). Lower for long internal summaries if the user finds it tiring; never 0 for shared material.
- **Clicks**: the game's own click WAV under every cursor press in a game clip; hover ticks at a quarter
  of that.

## 5. Writing the narration

**No title card**: open straight on something happening — the game, or the first diagram — and let the
narrator start talking as if mid-conversation. The end card (name and itch.io address) stays.

**The narrator is an old man speaking casually**: the shape of the talk is loose and natural ("Now
then.", "mind", "you see"), asides allowed, but the sentences carry slightly old-fashioned words and
turns of phrase ("hard by", "nigh on", "folk", "as is proper", "better than twice"). Never parody —
no "ye olde", no thee and thou; a grandfather who reads, not a pirate. `speed` 0.9 suits him.

**Clarity beats flavour.** The old man's voice is the seasoning, never the meal: when a turn of
phrase would cost the viewer the fact, say it plainly.

- **Say what is on screen as it appears**: "each of these four panels is one measure", "now the bars",
  "look at the four boxes". The viewer should never have to guess what a shape stands for.
- **Give every number its scope and unit**, aloud and in the chart title: *per city, averaged over
  sixty*, not "residents: 184". A total over many reads as one thing's count — prefer per-unit
  averages, and when a total is the point, say "in all".
- **Read the key numbers out, with an example**: "from nine to twenty — better than twice as many";
  "about three guards and three apprentices".
- **Over game footage, narrate what the player sees happen** — the lit word chosen, the way of
  thinking, the roll, the humor spent — in the order it happens.

One sentence per idea; a number the viewer must keep, said once and drawn at the same moment.
Spell out what TTS would mangle ("hot steppe", not "hot-steppe"; "one to three", not "1-3"; no
abbreviations). Audition a line: `make_video.py say "…" out.wav --grit 0.35`. Voices:
`en_GB-alan-medium` (default narrator), `en_GB-northern_english_male-medium`, `en_US-ryan-high`.

**Every claim must be true of the code at HEAD** — the narration is the manual read aloud, under the
same rule as the `manual` skill: derived from the source, never from `design/`. For promotional
material, say only what a player can see.

## 6. Verifying a video before handing it over

1. `--sheet` writes `<output>.sheet.png`; **Read it.** Check every segment is present, nothing is
   black or frozen where it should move, text is legible, nothing runs off the frame.
2. Read single frames at the moments that matter (`ffmpeg -ss <t> -i <video> -frames:v 1 f.png`) —
   `build/edit.json` lists each segment's start and length.
3. Sound cannot be heard from here; check it numerically — duration matches the picture, voice RMS
   around −18 dBFS, peaks under 0 dBFS (`vidkit.audio.rms_db`). Say plainly to the user that the mix
   was checked by numbers, not by ear.
4. Report: where the file is, its length, what each segment shows, and anything you could not check.

## Purposes

| for | do |
|---|---|
| the developer only (a survey, an audit, an analysis made watchable) | real-model footage all the same (fast-forwarded); dense charts are fine; say where the numbers came from on screen |
| sharing (technical notes, promotion) | real-model footage, fewer and bigger numbers, the end card with `edmond00.itch.io/proscribed`, the player-facing name **Proscribed Palimpsest** everywhere (never "Cathedral") |

## The code behind it

| | |
|---|---|
| `src/game/record/` | `RecordMode` (flags, clock, timeline), `RecordGate` (filter, locator, gestures), `RecordPointer` (the eased cursor), `FrameRecorder` (capture, cursor painting, ffmpeg), `RecordSession` (start/finish, manifest) |
| `GlyphSphereCore` | `VirtualPointerOnly`, `InjectPointer*`, `FrameSink`, `TryGetClickableVertexPixel` |
| `Cli*Span` / `Cli*Cell` / `CliPopupChoicePixel` / `CliFindElementCell` | where each target is, asked of the component's own hit-test. **A new clickable UI element needs one**, or `--record` cannot click it |
| `src/audio/MidiCapture.cs`, `MusicExport.cs` | the music captured as MIDI; `--export-music` |
| `src/debug/SceneExport.cs` | `--scene-export` |
| `tools/video/vidkit/` | `build`, `recording`, `tts`, `synth`, `audio`, `dither`, `palette`, `manimkit`, `cards`, `ffmpeg` |
