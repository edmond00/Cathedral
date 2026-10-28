---
name: music
description: The game's procedural soundtrack — the hierarchical composer, the self-built synthesiser, its seven mood gauges (coldness, fear, mystery, intensity, festivity, sacred, far), the idioms they lead to (dances, chant, far music, fights, night), the live streams and the legacy engine — and the evaluation harness in tools/music/ that measures what they produce. Use whenever touching src/audio/ (music, sound effects, stings, filters, volumes), changing how a place or a mood sounds, adding a gauge, an instrument, a mode, a progression, a form, a palette or an idiom, tuning the mix, hooking the music to a new game moment, diagnosing an underrun or a CPU spike in the music, or asked how the music works or how good it is. Carries the architecture, step-by-step recipes for each kind of change, the evaluation loop, the rules and the pitfalls already met.
---

# The music

The music is **generated**: it never loops and never plays a file. It is retro dungeon synth
(Fief, Erang, Old Tower, early Mortiis): modal, slow, layered, repetitive in the way folk tunes
are, drenched in reverb, through a cassette. Every instrument is synthesised in `src/audio/music/`:
no samples, no SoundFont, nothing to licence. Nobody working on it by machine can hear it, so every
judgement goes through **tools/music/**, and a change to the music is not done until the harness
has measured it. The developer listens on the published page and in `--music` mode.

**`idioms.md`** (beside this file) describes each idiom in detail: festive dances, fights, chant
and the distant choir, far music and the stranger, night, breaks, events, and how they combine.
Read the section for whatever you are changing. `tools/music/README.md` lists the tools and metrics.

## Architecture

```
AmbianceEngine (src/audio/AmbianceEngine.cs)     the game's only door: SetMood, SetNight, SetFilter,
 │                                               SetActiveTrackCount, Transition, TriggerGameEvent;
 │                                               Requested/Restore (what the game last asked)
 ├─ Music.SynthMusicEngine   default             --music-engine synth
 │   └─ MusicCore            composer + synth on one sample clock; mood glide; layer gates; Cut
 │       ├─ Composer         piece → form → sections → chords → lines → notes, bar by bar
 │       │   Theory.cs (modes, keys, chords, progressions) · Melody.cs (motifs, rhythms, periods)
 │       │   Orchestra.cs (poles → instruments, tempo, metre, part levels, trims, hall and tape)
 │       └─ Synth            voices → part buses → chorus + FDN hall → tape → limiter
 │           Voices.cs (33 instruments, levelled) · Effects.cs · Dsp.cs
 │   └─ PcmStream            waveOut on Windows, OpenAL elsewhere (adaptive queue, underrun report)
 └─ LegacyAmbianceEngine     --music-engine legacy: the first engine, kept for comparison
LocationMoodProfiles.cs      a mood per kind of place, sampled per location id (+ the gauge tables)
MusicMoodState.cs            the gauges, the presets, AtNight()
```

- **Everything is pulled.** `MusicCore.Render` applies queued commands, asks the composer to plan
  0.3 s ahead, starts due notes, synthesises. The live engine calls it from the audio thread;
  `--render-music` calls it in a loop. Same code, same samples: what the harness measures is
  what the game plays.
- **Determinism.** Piece *n* draws from `GameRng.For("music.piece.n")` (offline:
  `GameRng.ForWorld(seed, …)`); the synth's noise is a seeded xorshift (`Noise`). The same seed
  and piece number give the same choices wherever they play; in the game the piece counter moves.
- **A piece** picks its mode, tonic (moving by fourths and fifths), tempo, metre and orchestration
  from the leading pole, then a form (`I A A' B A' O`…). Each A is a period (motif, sequence,
  second idea, open cadence, closed repeat); A' a restatement; B the motif turned over on contrast
  chords; C a breakdown or a lone chant. Layers enter one at a time.

## The gauges

`MusicMoodState`, all 0..1. The first four are the old engine's and shape every piece; the last
three are each a whole idiom, set per kind of place, and lead only where they are strong.

| gauge | set by | what it does |
|---|---|---|
| Coldness | place profile, presets | warm (Dorian, brisk) to cold (Phrygian, harmonic minor, slow); hall and tape darker |
| Fear | place, fight (≥ 0.85) | tension: faster, Locrian at the top, the fear palette; damps the three idioms |
| Mystery | place | the dark palette, pentatonic now and then, wind, a longer hall, more chorus |
| Intensity | place (0.7–1), map 0.25 | gates the layers: melody from 0.25–0.35, counter and bass 0.5–0.6, ostinato and drums 0.75–0.85 (with the melody in a dance or a fight) |
| Festivity | `LocationMoodProfiles.Festivity` | leads from ~0.5: a jig or a reel on fiddle, whistle, pipes; zero at night |
| Sacred | `LocationMoodProfiles.Sacred` | leads from ~0.5: plainchant; lower, a distant choir drifts in |
| Far | `LocationMoodProfiles.Far` | leads from ~0.5: strange modes, an in-between third, bends, new voices; lower, a stranger sits in |

Track count (`SetActiveTrackCount`, 0–4) gates the same layers from the game's side. The fight
filter, night (`SetNight`, the place's `AtNight()`) and breaks (`Transition`) act on top.

## Recipes

Each recipe ends with **Verify**, below. Never skip it: the faults found in this music were
found by measuring, never by reading the diff.

**Change how a kind of place sounds.** Edit its row in `LocationMoodProfiles.Profiles`
(coldness, fear, mystery ranges) or in the `Festivity`, `Sacred` or `Far` tables. Run
`dotnet bin/Debug/net8.0/Cathedral.dll --music-census 300` and read the place's row: which pole
leads how often, how often a distant choir or a stranger may come in. Render a few ids
(`--mood forest:3`, `forest:7`…) on two seeds and read their notes. A new kind of place needs a
row, or it falls back to `Fallback`.

**Add a gauge** (as festivity, sacred and far were added; follow `Far` through the code):
1. `MusicMoodState`: the field with a doc comment saying what it means and what never uses it; a
   constructor parameter **last**, with default 0; every `With*` and `AtNight()` passing it on;
   a `WithX` method; `ToString`.
2. `LocationMoodProfiles`: a table of ranges, drawn **after** the others in `SampleMood`, so every
   place keeps the coldness, fear and mystery (and the other gauges) it had.
3. `MusicCore.Step`: the glide. `Composer.EffectiveMood`: what a fight does with it (usually 0).
   `Composer.CheckDrift`: add it to the distance.
4. `Orchestra`: a `Pole` (instruments per role, percussion, chances, liveliness, chant), its weight
   in `Weights` (squared, scaled near 4–4.5 so it leads from about a half; damped by fear if that
   is right), exclusive in `Choose` if it is an idiom rather than a blend, its room in `ApplyBus`,
   a `XLeads` helper, columns in `Census`.
5. `Composer`: what the idiom changes (mode in `NewPiece`, form in `PlanForm`, a line writer in
   `PlanLines`, emitters), and a colour for low values that do not lead (as `Cantus`, `Stranger`).
6. Tools: `OfflineRender` (journey arrays and `--mood` lists take one more number),
   `MusicModeLauncher` (a key pair, a bar, the random mood), `inspect music` in
   `LocationTravelGameController` (append the value **at the end** of the line: scripts assert
   on contiguous text), `targets.json` `poles.<name>` with a why, the census, this skill,
   `idioms.md`, the README.
7. Check every other mood is untouched (see **Verify**).

**Add an instrument.**
1. `Instrument` enum with a doc comment (what keyboard preset it imitates); `Voice.Make`.
2. A `Voice` subclass: `Start` (set `Env`; read `Glide` if the voice can bend), `Render` adding into
   `buf` with `* e * Amp`, killing the envelope when a struck voice has died away (`energy`
   check, as `BodhranVoice`), and per-sample work kept small: move vibrato, wander and filter
   changes to a control rate as `ChoirVoice` does, or a choir of them will not render in time.
   Shaping band-passes use `Svf.BandNorm`.
3. `Audition.cs`: a root note if the default (D4) is wrong for it. `tools/music/levels.py`: add
   it to `STRUCK` if it is plucked or struck. Run `python3 tools/music/levels.py --apply` and
   rebuild, **twice** (it converges; a third time if a voice is still over 0.5 dB off).
4. `MusicCore.GmProgram` and `tools/music/timbre.py`'s `GM` (the General MIDI stand-in, for the
   MIDI capture and the timbre comparison). `Orchestra.Trim` only if it should sit above or below
   its peers.
5. Use it in a pole, or an emitter. Check `timbre.py` against its GM namesake and look at the sheet.

**Add a mode or a progression.** `Mode` in `Theory.cs`: never a major third over the tonic (the
soundtrack's rule: Dorian is the warmest), and a true fifth if a drone, a pad or organum stands on
the tonic. A diminished home chord grinds against the drone (a first far mode, Locrian with a
natural second, measured dissonance 0.8 and was dropped). Map it in `Progressions.For`, with
progressions of its own that keep off its diminished chords. If it is not a mode of the diatonic,
harmonic or melodic minor, add its collection to `COLLECTIONS` in `musickit/symbolic.py`, or
`mode_fit` will call it chromatic. Microtones go in `ScoreEvent.Inflect`, never in `Midi`, so the
score names the scale note.

**Change an idiom** (fights, dances, chant, far): read its section in `idioms.md`, change the
pole, the form planner and its emitters, render that idiom's places or preset (`--mood Battle`,
`tavern:12`, `temple:3`, `jungle:3`), and the journeys that cross it.

**Hook the music to a game moment.** Through `AmbianceEngine` only. Set it in the mode's `OnEnter…`
and ask: is this mode ever *resumed* rather than entered (the pause menu, a fight from a
journey)? `ResumeFromMenu` restores `Requested`; anything else must set its own tracks and filter
(`OnEnterFighting` sets four tracks because a travel encounter opens from a journey's one). A new
request goes into `MusicRequest` so the menu gives it back. Add an `inspect music` assertion to a
`cli/` script for it; `--silent` engines answer it.

**Tune the mix.** Balance belongs in `Orchestra.PartLevel` (per part, a fight's own column) and
`Orchestra.Trim` (per instrument); every emitter multiplies by `Trim` (the first fight emitters did
not, and their trims did nothing). Read the stems in the score (`"stems"`: dB per part) and
`melody_prominence`. Never re-level the voices to fix a balance.

## Verify

```bash
dotnet build
python3 tools/music/eval.py matrix --label <round> --seconds 300 --seeds 1 2          # the preset moods
python3 tools/music/eval.py matrix --label <round> --seconds 240 --seeds 1 2 --moods temple:3 jungle:3 port:3
python3 tools/music/eval.py compare tools/music/runs/<before> tools/music/runs/<round>
python3 tools/music/eval.py notes tools/music/runs/<round>/Tavern_s1.score.json --parts melody counter
python3 tools/music/eval.py analyse <wav>        # metrics + <wav>.png sheet: look at it
dotnet bin/Debug/net8.0/Cathedral.dll --render-music out.wav --journey tools/music/journeys/tour.json
```

1. **Metrics** (`targets.json`; bands per mood under `moods`, per pole under `poles`).
2. **Read the score**: the melody as note names, bar by bar, with the chords. Leaping sequences, a
   B tune stuck on two notes, a counter re-striking one note, a chant that leapt: all passed
   every metric and were obvious here.
3. **Look** at the sheet (spectrogram, piano roll, self-similarity, loudness and layers) and at
   `timbre.py` for a voice.
4. **Untouched means the same notes.** When a change should not reach a mood, compare its notes
   with the previous round's **as a multiset** of (time, pitch, part, instrument): a new event in
   the queue reorders notes that start together without changing a sound. Tavern, Neutral,
   Lament and the others stayed note for note identical through the festive, sacred and far work
   because every new random draw sits behind a condition that is false for them (`if (chant &&
   _rng.NextDouble() < …)`, never a bare draw).
5. **Journeys** (`journeys/tour.json`, `opening.json`, `encounter.json`) catch transitions, fights
   and events that still moods cannot. `render_speed` must stay over 8x (a temple full of
   choirs once fell to 8.1x).
6. **Game scripts**: `./run_tests.sh audits` plus `cli/system/pause_music_resumes.cli`,
   `night_music.cli`, `fight_runaway_travel_arrival.cli`, `fight_victory_narration.cli` and
   `cli/verb/get_up/` when the hooks are touched. Say which ran; the full suite is the release gate.

**Bands are a regression signal, not a verdict.** When a change makes the music better to read and
to look at and a band disagrees, widen that pole's or mood's band and write why. Every idiom has.

## Hearing it

`dotnet bin/Debug/net8.0/Cathedral.dll --music` is the live engine on its own: S/F/M/I/Y/9/8 raise
coldness, fear, mystery, intensity, festivity, sacred, far (Shift lowers), 1–4 tracks, T/L/X/B/W
presets (B is a fight), Space a break, Z a random mood, A/K/G/J/O events, H help. Clips for the
developer: render, normalise to −18 LUFS (`pyloudnorm`), encode with `ffmpeg -q:a 4`, and publish
them on a page with seek buttons at the moments that matter.

## A fresh cloud session

```bash
pip install numpy scipy soundfile pyloudnorm mido matplotlib librosa
dotnet build
# the CLI scripts need GloVe vectors (models/embeddings/, gitignored); Stanford is blocked, gensim's
# GitHub mirror is not: download glove-wiki-gigaword-100.gz, gunzip, drop its first (header) line
# into models/embeddings/glove.6B.100d.txt
# timbre.py's reference SoundFont: GeneralUser GS (assets/audio/README.md says where); never shipped
# live-path tests: xvfb-run, pulseaudio -D --exit-idle-time=-1, parec -d auto_null.monitor --format=s16le
```

## Rules and pitfalls

- **Never `new Random()`**: `GameRng` per piece, `Noise` in the synth.
- **Never borrow a real people's music** for an idiom: far is strange, not "exotic" (no
  snake-charmer scale, no tribal drums). Festive is the session idiom; chant is plainchant.
- **No major third over the tonic, anywhere.** A far piece's third is inflected a little under
  halfway (+0.45), never to major.
- **`--silent` composes nothing.** Under `--silent` (every `run_tests.sh` script) the engine starts
  with no composer and only answers `inspect music`. A CLI script proves what the game *asked*;
  what the composer does with it needs a render or a live run without `--silent` (a fight that
  wrote fight music and gated it to a pad passed every script).
- **A preset can hide a gate.** Battle is at full Intensity; real fights open from places and
  from the world map (Intensity 0.25). Test the path the game takes (`journeys/encounter.json`).
- **Levels**: `levels.py --apply` twice after changing a voice; its targets are measured after
  `Synth.PreGain` (change one, move `SUSTAINED_DB`/`STRUCK_DB` with it, or every voice is
  silently re-levelled: it once pushed the mix up 2.7 dB).
- **The pause menu borrows the music** and `ResumeFromMenu` gives it back; a mode resumed rather
  than entered sets nothing itself.
- **The live stream reports itself** on shutdown (`[audio] music stream: … n underrun(s)`).
- **Don't rebuild under a running matrix** (it mixes binaries), and don't `pkill -f` a pattern
  that also matches your own shell's command line.
- Music is not a rule the player is subject to: the manual needs nothing.
