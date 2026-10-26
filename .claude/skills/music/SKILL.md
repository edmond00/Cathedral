---
name: music
description: The game's procedural soundtrack — the hierarchical composer, the self-built synthesiser, the live streams and the legacy engine — and the evaluation harness in tools/music/ that measures what they produce. Use whenever touching src/audio/ (music, sound effects, stings, filters, volumes), changing how a mood sounds, adding an instrument, a progression, a form or a palette, tuning the mix, diagnosing an underrun or a CPU spike in the music, or asked how the music works or how good it is. Carries the architecture, the evaluation loop and its metrics, and the rules that keep the music endless, modal and in the genre.
---

# The music

The music is **generated**: it never loops and never plays a file. It is retro dungeon synth
(Fief, Erang, Old Tower, early Mortiis): modal, slow, layered, repetitive in the way folk tunes
are, drenched in reverb, through a cassette. Nobody working on it by machine can hear it, so every
judgement goes through **tools/music/**, and a change to the music is not done until the harness
has measured it.

## Architecture

```
AmbianceEngine (src/audio/AmbianceEngine.cs)     the game's only door: same API as ever
 ├─ Music.SynthMusicEngine   default             --music-engine synth
 │   └─ MusicCore            composer + synth on one sample clock
 │       ├─ Composer         form → sections → chords → motifs/periods → notes, bar by bar
 │       │   Theory.cs (modes, keys, chords, progressions) · Melody.cs (motifs, rhythms, periods)
 │       │   Orchestra.cs (mood poles → instruments, tempo, metre, mix levels, hall and tape)
 │       └─ Synth            voices → part buses → chorus + FDN hall → tape → limiter
 │           Voices.cs (17 instruments) · Effects.cs · Dsp.cs
 │   └─ PcmStream            waveOut on Windows, OpenAL elsewhere (adaptive queue, underrun report)
 └─ LegacyAmbianceEngine     --music-engine legacy: the first engine, MIDI to the GS Wavetable
                             Synth (Windows) or MeltySynth + assets/audio/soundfont.sf2
```

- **Everything is pulled.** `MusicCore.Render` applies queued commands (mood, events, filters,
  volumes), asks the composer to plan 0.3 s ahead, starts due notes, synthesises. The live engine
  calls it from the audio thread; `--render-music` calls it in a loop. Same code, same samples, so
  what the harness measures is what the game plays.
- **Determinism.** Piece *n* draws from `GameRng.For("music.piece.n")` (offline:
  `GameRng.ForWorld(seed, …)`); the synth's own noise is a seeded xorshift (`Noise`), never
  `System.Random`. The world theme is piece −1.
- **Top down.** A piece picks mode (the `ModalScale` palette: no major-third mode, Dorian is the
  warmest), tonic (moves by fourths and fifths), tempo, metre, an orchestration from four poles
  (comfy, melancholy, dark, fear, festive), a form (`I A A' B A' O`…) and motifs. Each A is a period: the
  motif, its sequence, a second idea, an open cadence; then the same phrase with a closed ending.
  A' is a plain or ornamented restatement, B the inverted motif over contrast chords, C a breakdown
  or chant. Layers enter one at a time across the piece.
- **Festive pieces.** `MusicMoodState.Festivity` (per place, `LocationMoodProfiles`: taverns,
  markets, ports, circuses, towns, villages) pulls towards the festive pole. When it leads, the
  piece is a dance (`Dance.Jig`, 6/8 written as Meter 2 with triplet eighths, or `Dance.Reel`,
  running eighths) played the session way: `I A A' B B' A' A' B' B'`, each part twice, the tune
  twice round, then the next tune of the set with no outro. Fiddle, whistle (sounds an octave
  up), pipes; lute or harp strumming boom-chick (`EmitComp`); bodhrán (`EmitBodhran`); the second
  instrument in heterophony; cuts and rolls (flagged `Ornament` in the score so the harness reads
  the tune under them); a lilt (`Lilt`). Still Dorian or Aeolian: festive is lively, not bright.
  The comp and drum come in with the melody's tier there, not at Intensity 0.75. The harness
  holds festive renders to the `poles.festive` bands in `targets.json`.
- **Mood.** `SetMood` glides the mood (≈2 s). Structure is decided at piece and section starts; a
  drift above 0.12 (or the fight filter changing) ends the piece through a one- or two-bar bridge,
  in which the tune comes to rest on the nearest tonic and holds it; the next piece then starts on
  its tune, with no intro. A tune-less breakdown (C) lasts four bars at most.
  Track count and Intensity gate layers on the old ladder (1 drone+pad, 2 melody, 3 counter+bass,
  4 ostinato+percussion).
- **Breaks.** `AmbianceEngine.Transition(seconds)` is a clean cut between parts of the game, not
  a change of place: everything planned is dropped, every voice fades over 1.2 s, the hall rings
  out into silence, and a new piece for the mood already set begins from its start (no glide).
  Called entering the get-up (its own `MusicMoodState.GetUp`, two tracks) and on the first world
  map after it; the childhood's entry gets its break from dropping to no tracks. Set the mood and
  tracks first. Journeys script one with `"break": seconds` (`journeys/opening.json`); `Space` in
  `--music` mode plays one. A change of place keeps the one-bar bridge: a break every time would
  chop up play.
- **Events.** Positive: a bell arpeggio on the next beat and a brighter bar. Negative: a timpani
  stroke on the next beat and the tune stepping back for a bar or two. Neutral: the tune a little
  softer. The tune never stops for an event: outcomes come every few seconds in play, and resting
  the melody on each one was heard as the music dropping out. The stings (the old
  notes) play at once on the SFX bus. Hover and click remain PCM ticks outside the music.

## The evaluation loop

```bash
dotnet build
python3 tools/music/eval.py matrix --label <round> --seconds 300 --seeds 1 2   # every preset mood
python3 tools/music/eval.py compare tools/music/runs/<before> tools/music/runs/<round>
python3 tools/music/eval.py notes tools/music/runs/<round>/Tavern_s1.score.json --parts melody counter
python3 tools/music/eval.py analyse <wav>          # one render: metrics + <wav>.png sheet (look at it)
```

`tools/music/README.md` lists every metric, its band and why. Use all three kinds of evidence;
each catches what the others miss:

1. **Metrics** (`targets.json`): symbolic (mode fit, steps, motif recurrence in scale steps, bar
   repetition, phrase regularity, harmonic rhythm, dissonance, layering, voice ranges), mix (stem
   levels per part, melody prominence, wetness) and acoustic (loudness, spectral tilt, highs,
   stereo, swells, holes, spikes), plus cost (render speed, voices).
2. **Reading the score** (`eval.py notes`): the melody as note names, bar by bar, with the chords.
   Several real faults (sequences leaping a sixth, a B tune stuck on two notes, a counter that
   re-struck one note every half bar) passed every metric and were obvious here.
3. **Looking** at the sheet PNG (spectrogram, piano roll, chroma self-similarity, loudness and
   layers) and at `timbre.py`'s comparison of each instrument with its GM namesake.

Calibration: `exemplars.py` measures public-domain tunes written out in `exemplars/` with the same
code. Where a real tune falls outside a melodic band, the band is wrong. `eval.py calibrate
reference/` turns reference recordings (gitignored; e.g. the developer's earlier soundtrack) into
acoustic bands in `targets.reference.json`, which then override the hand-set ones.

**Bands are a regression signal, not a verdict.** Do not chase a band with a change that makes the
music worse to read or to look at; re-calibrate the band instead, and say why in its `why`.

## Rules

- **Never `new Random()`** — `GameRng` per piece, `Noise` in the synth. The legacy engine's two
  unseeded generators were converted when it moved.
- **Journeys catch what still moods cannot.** `--journey tools/music/journeys/tour.json` (moods,
  events, travel, a fight). The zombie-voice bug (an envelope re-opened after it finished) showed
  only there, as a render slowing from 13x to 4x real time; `render_speed` and `max_voices` are
  in the metrics for that reason.
- **Levels.** After changing a voice, run `python3 tools/music/levels.py --apply` (twice) so every
  instrument stays at the common level; balance belongs in `Orchestra.PartLevel` and `Trim`. Its
  targets are measured after `Synth.PreGain`: change the pre-gain and move `SUSTAINED_DB` with it,
  or the next run silently re-levels every instrument (it once pushed the whole mix up 2.7 dB).
- **Band-passes that shape a timbre use `Svf.BandNorm`** (unity peak). Plain `Band` peaks at Q.
- **The live stream reports itself** on shutdown: `[audio] music stream: …s played in …s, n
  underrun(s) (at …), slowest block …`. Underruns cluster in the first seconds (world build, model
  load, collections); the queue is 256 ms for 20 s, then 128 ms.
- **Testing the live path in a cloud box**: `xvfb-run` plus a PulseAudio null sink
  (`pulseaudio -D --exit-idle-time=-1`), record `parec -d auto_null.monitor --format=s16le` (raw:
  a killed parec leaves a broken WAV header) and analyse the capture like any render.
- **The pause menu borrows the music.** It sets the neutral mood with no tracks; a phase resumed
  from it is not entered afresh and sets nothing, so `ResumeFromMenu` hands back what
  `AmbianceEngine.Requested` held when the menu opened. A new place's music is set by its own entry
  (`StartLocationInteraction`); anything that sets music on a mode's entry must still be right when
  that mode is resumed rather than started. `inspect music` reads the request back, even under
  `--silent` (the engine then starts silent: no stream, no composer, but it answers).
- Music is not a rule the player is subject to: the manual needs nothing.
