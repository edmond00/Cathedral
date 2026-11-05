# The music's idioms, one by one

Reference for `SKILL.md`: what each idiom is, which code writes it, and the decisions behind it. Read
the section for whatever you are changing. Every idiom here is held to the harness like the rest
(`poles.*` and `moods.*` in `tools/music/targets.json` carry the bands that differ, each with its why).

## How the idioms combine

One pole leads each piece (`Orchestra.Weights`, highest wins). Festive, sacred and far are
**exclusive**: when one leads, the piece takes only that pole's instruments and its own writing
(dance form, chant, the far modes). The four ordinary poles (comfy, melancholy, dark, fear)
**blend**: instruments are drawn from all of them in proportion. A minor gauge that does not lead
is a **colour**, never a second idiom: sacred adds the distant choir (`Part.Cantus`), far adds the
stranger (`Orchestration.Stranger`). Fear damps festivity, sacred and far; a fight ignores all
three; night keeps sacred and far and zeroes festivity.

When two exclusive gauges are both high the leader takes all (by weight: sacred over far over
festive at equal values): there is no "strange dance" or "festive chant". Real places do not meet
this, because each kind of place has one dominant flavour (`--music-census`); a stress test of 27
mixed and conflicting combinations (`--mood 0.5,0.3,0.5,1,0.9,0.9,0.9`) averaged 0.966, each a
single idiom. Known soft spot: a festive or far weight that does not lead can still draw a fiddle,
pipes or a reed into a blended piece (a dirge on the pipes in a cold, mysterious, festive mood).

## Festive pieces

`MusicMoodState.Festivity` (per place, `LocationMoodProfiles`: taverns,
markets, ports, circuses, towns, villages) pulls towards the festive pole. When it leads, the
piece is a dance (`Dance.Jig`, 6/8 written as Meter 2 with triplet eighths, or `Dance.Reel`,
running eighths) played the session way: `I A A' B B' A' A' B' B'`, each part twice, the tune
twice round, then the next tune of the set with no outro. Fiddle, whistle (sounds an octave
up), pipes; lute or harp strumming boom-chick (`EmitComp`); bodhrán (`EmitBodhran`); the second
instrument in heterophony; cuts and rolls (flagged `Ornament` in the score so the harness reads
the tune under them); a lilt (`Lilt`). Still Dorian or Aeolian: festive is lively, not bright.
The comp and drum come in with the melody's tier there, not at Intensity 0.75. The harness
holds festive renders to the `poles.festive` bands in `targets.json`.

## Fights

The fight filter (`Composer.Fighting`) writes the genre's martial battle music, led
by its drums: `Orchestra.ChooseFight` (the lead from horn, choir, strings, square lead, organ; a
dark pad; `Instrument.Staccato` strings and horn stabs for the ostinato; `WarDrum`, `Timpani`,
`Snare` with ruffs and rolls, a `Cymbal` crash only to mark a section). A quarter of fights
(`Orchestration.Guitar`) add a palm-muted `Distortion` doubling the strings' gallop, low in the
mix: the metal is a colour, not the sound. 84-100 bpm, never Locrian. `PlanFightForm`:
`I A A' B C A' B A'`, each section a `RiffStyle` (`EmitRiff`, `EmitKit`): Gallop (a march) under
the A tune, Tremolo (strings in sixteenths, pounding war drums, a snare roll) under the B,
HalfTime (a tolling war drum and timpani) for the tune-less breakdown. In a fight the drums'
part comes up and the ostinato's goes down (`PartLevel(part, fight)`). The band plays from the
first bar; the ostinato and drums open with the melody's tier, like a dance's. A fight cuts in at once
(`MusicCore.SetFighting`: the planned bars dropped, a 0.2 s fade, the drums 0.08 s later); its end
keeps the bridge, and it plays in a closer room (`ApplyBus(..., fight: true)`). `--mood Battle`
renders a fight, as `B` in `--music` mode plays one; `targets.json` holds Battle to its own bands.
A fight gates its layers at full Intensity whatever the mood: a travel encounter opens from the
world map's (0.25, drone and pad only), and was once written as a fight and heard as a pad.
Battle's preset is at full Intensity and hid that; `journeys/encounter.json` is the check.
`Kick` and `PowerChord` are left from the first, metal version: levelled, unused.

## Sacred places

`MusicMoodState.Sacred` (per place, `LocationMoodProfiles.Sacred`, drawn
after festivity so every other place keeps its mood) pulls towards the sacred pole. When it
leads (temples, cathedrals, churches, monasteries, shrines, sanctuaries; a castle's or a
palace's chapel about one time in nine or five) the piece is chant: a choir lead (the organ now
and then), the church modes only (Dorian, Phrygian, Aeolian), `Plainchant` writing the line
(an intonation rising by step to a reciting tone, the recitation, a stepwise cadence to the final,
almost no leaps), the second voice in organum, a drone, bells, almost no ostinato, the longest
hall. Lower (peaks, snowfields, glaciers, groves, ruins, burial places) it leaves the place's own
music and adds `Part.Cantus`: a distant wordless choir, one held chord tone every two bars,
nearly all hall, in some B, C and varied-A sections (`Orchestration.Cantus`, `EmitCantus`). Fear
damps it, mystery does not; a fight ignores it, night keeps it. `9`/`Shift+9` in `--music` mode;
`targets.json` holds chant to `poles.sacred`. `--music-census [n]` samples every kind of place
and prints which pole leads how often and where the distant choir may come in: run it after
changing a profile.

## Far places

`MusicMoodState.Far` (per place, `LocationMoodProfiles.Far`, drawn last) is how
far a place is from the empire's ear. Strange, not borrowed: nothing imitates a real people's
music (no "snake-charmer" scale, no "tribal" drums). When it leads (jungle, desert, oasis, the
hot steppe; a pyramid mostly, a sunken city sometimes) the piece is far throughout: the far modes
(`Mode.DorianSharp4`, `Mode.DorianFlat2`, a minor third and a true fifth, their own
progressions keeping off the diminished chords; or Phrygian and Aeolian), a third a little under
halfway to major (`Piece.NeutralThird`, carried as `ScoreEvent.Inflect` so the score names the
scale note and the harness reads it as one), long notes sliding into place (`ScoreEvent.Glide`,
read by `Reed`, `BuzzString`, `Glass` and `BentDrum`), `Metallophone` and `BuzzString` on an
eighth-note figure grouped 3+3+2 (`OstinatoStyle.Additive`), the `BentDrum` on the same accents, a
drier room. Lower (ports, markets, the forum, swamps, canyons, old ruins) the place keeps its own
music and a stranger (`Orchestration.Stranger`, `FarVoice`: a reed, metal or the buzzing string)
takes the line for a section or two, bending it; in a festive piece, a B part the second time
round. A far holy place chants in a stranger mode now and then. Fear damps it, a fight ignores it,
night keeps it. `8`/`Shift+8` in `--music` mode; `poles.far` bands; the census has columns for it.
The first draft's Locrian with a natural second was dropped: its diminished home chord ground
against the drone (dissonance 0.8).

## Night

A visit's period reaches the music in `NarrativeController.ApplyTimePeriod` (the one
writer of the period): at Night, in exploration, `AmbianceEngine.SetNight(true)` plays the
place's `MusicMoodState.AtNight()` (colder, stranger, a little uneasy, festivity 0, Intensity
x0.82 so the ostinato and percussion thin first). Each place keeps its own character, shifted.
Any new `SetMood` is day again; the pause menu hands the night back. Other periods have no
variant. The harness renders one as `tavern:12@night`.

## Mood changes

`SetMood` glides the mood (≈2 s). Structure is decided at piece and section starts; a
drift above 0.12 (or the fight filter changing) ends the piece through a one- or two-bar bridge,
in which the tune comes to rest on the nearest tonic and holds it; the next piece then starts on
its tune, with no intro. A tune-less breakdown (C) lasts four bars at most.
Track count and Intensity gate layers on the old ladder (1 drone+pad, 2 melody, 3 counter+bass,
4 ostinato+percussion).

## Breaks

`AmbianceEngine.Transition(seconds)` is a clean cut between parts of the game, not
a change of place: everything planned is dropped, every voice fades over 1.2 s, the hall rings
out into silence, and a new piece for the mood already set begins from its start (no glide).
Called entering the get-up (its own `MusicMoodState.GetUp`, two tracks) and on the first world
map after it; the childhood's entry gets its break from dropping to no tracks. Set the mood and
tracks first. Journeys script one with `"break": seconds` (`journeys/opening.json`); `Space` in
`--music` mode plays one. A change of place keeps the one-bar bridge: a break every time would
chop up play.

## Events

Positive: a bell arpeggio on the next beat and a brighter bar. Negative: a timpani
stroke on the next beat and the tune stepping back for a bar or two. Neutral: the tune a little
softer. The tune never stops for an event: outcomes come every few seconds in play, and resting
the melody on each one was heard as the music dropping out. The stings (the old
notes) play at once on the SFX bus. Hover and click remain PCM ticks outside the music.
