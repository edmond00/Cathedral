# tools/music: measuring the music

The game's music is generated, and nobody working on it by machine can hear it. Everything here
exists to turn "is it better?" into things that can be looked at and counted. The `music` skill
(`.claude/skills/music/SKILL.md`) says how the engine works and when to use what.

```bash
pip install numpy scipy soundfile pyloudnorm mido matplotlib librosa   # librosa only for timbre.py
dotnet build
```

## Producing music to measure

| command | what |
|---|---|
| `dotnet bin/Debug/net8.0/Cathedral.dll --render-music out.wav --seconds 300 --mood Tavern --seed 1` | the game's music, rendered offline (about 13x faster than real time), with `out.score.json` beside it. `--mood` takes a preset (`Neutral`, `Tavern`, `Lament`, `DarkDungeon`, `Battle`, `WorldView`, `Creation`, `Childhood`) or a location sampled as the game samples it (`tavern:12`, `catacombs:5`, `port:3`); `QuietTavern` is
the tavern without its festivity, and `Battle` is rendered as a fight (the fight filter over the mood, as the game plays one). `--journey j.json` scripts mood changes, events, filters and track counts over time (`journeys/tour.json`). `--verbose` prints voices and time spent every 30 s |
| `… --music-audition <dir>` | every instrument alone, one WAV and score each |
| `… --music-census [n]` | every kind of place sampled n times (200): which pole leads how often (comfy, melancholy, dark, fear, festive, sacred) and where a distant choir may come in. No audio |
| `… --export-music out.mid --seconds n --mood m` | real time, MIDI: what `--record` captures |
| `… --music-engine legacy` | in the game: the first engine, for comparison |

The score JSON carries what MIDI cannot: each note's part and instrument, the pieces (mode, tonic,
tempo, metre, form, palette, theme), the sections and their layers, the chords, each part's stem
level, the hall's wetness, render speed and peak voice count.

## Measuring it

| script | what |
|---|---|
| `eval.py analyse x.wav [--score s.json \| --midi m.mid]` | every metric against its band, the total, and `x.png`: spectrogram, piano roll by part with sections, chroma self-similarity, loudness and playing parts, octave spectrum |
| `eval.py matrix --label L [--moods …] [--seeds 1 2] [--seconds 300]` | renders every mood × seed into `runs/L/`, analyses all, writes `summary.json`, lists what is out of band and how often |
| `eval.py compare runs/A runs/B` | medians side by side |
| `eval.py notes s.json [--first n --count k --parts melody counter]` | the score as text, bar by bar with the chord: **read it**. Leaps, stuck tunes and static counters show here before any metric |
| `exemplars.py` | the melodic metrics on public-domain tunes written out in `exemplars/`, each played twice: the calibration of the melodic bands |
| `timbre.py <audition-dir>` | each instrument beside the General MIDI preset it stands in for (rendered through FluidSynth and a reference SoundFont, never shipped): attack, centroid, vibrato, third-octave envelope distance, and a sheet |
| `levels.py [--apply]` | measures every instrument's audition and writes level corrections into `Voices.cs` (run twice; it converges) |
| `eval.py calibrate reference/` | acoustic bands from reference recordings (put WAVs in `reference/`, gitignored): `targets.reference.json`, which `targets.py` then prefers |
| `eval.py report runs/A runs/B -o page.html` | an HTML page with the audio, the sheets and the metrics |

## The metrics

The bands and the reason for each are in `targets.json`; per-mood overrides (a fight may be more
dissonant and busier) sit under `moods`, per-pole overrides under `poles` (a festive render is
dance music: denser, a chord a bar, a drier room, a lilt; a sacred one is chant: stepwise, soft-edged, dark, long-phrased; each held to its own bands). Scores: in band is 1, out of band decays with the distance
in band widths, the total is the weighted mean.

- **Mode and harmony:** `mode_fit`, `pc_entropy`, `harmonic_rhythm_s`, `dissonance_time`.
- **Melody:** `mel_step_ratio`, `mel_leap_ratio`, `mel_large_leap_ratio`, `mel_range_phrase`,
  `mel_distinct_phrase`, `mel_density`, `phrase_len_cv`.
- **Repetition and variation:** `motif_recurrence` (4-note figures in scale steps and rhythm,
  heard in the last 180 s), `bar_repetition`, `long_novelty` (windows four minutes apart share
  little: never a loop), `audio_repeat` (the repetition, heard through the mix).
- **Texture and voices:** `active_parts`, `layer_changes_min`, `silence_ratio`, `grid_alignment`,
  `counter_repeat`, `counter_above_mel`.
- **Mix:** `melody_prominence`, `drone_vs_melody` (stem levels), `wet_db`.
- **Sound:** `lufs`, `peak_dbfs`, `clip_ratio`, `tilt_db_oct`, `hf6_share`, `hf8_share`,
  `presence_share`, `low_share`, `side_ratio`, `envelope_roughness`, `onset_rate`, `swell_db`,
  `hole_ratio`, `spike_db`.
- **Cost:** `render_speed`, `max_voices`.

Where they came from: the melodic bands were checked against real tunes (`exemplars.py`); the
acoustic ones are genre knowledge (dark, warm, low-passed, wet, slow swells) until reference
recordings calibrate them. They are a regression signal. When a change improves what the score
reads like and a band disagrees, fix the band and say why.

## What the harness cannot tell

Whether it is beautiful. Whether a timbre is pleasant rather than merely close to its GM namesake.
How the music feels against the game's screens. The metrics catch regressions and departures from
the genre; the notes and the sheets catch faults of craft; the rest needs ears.
