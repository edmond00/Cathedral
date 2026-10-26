# assets/audio/

`soundfont.sf2` — the General MIDI SoundFont the **legacy music engine** plays with on the OpenAL
path (`--music-engine legacy`, on Linux or with `--audio openal`). **The default music no longer
reads it**: since the `music-overhaul` branch the game synthesises its own instruments
(`src/audio/music/`) on every platform, so a shipped build needs no SoundFont at all. It is kept
for comparing the two engines, and `tools/music/timbre.py` uses one as a measuring stick (never
shipped). With the legacy engine on Windows' default path, MIDI still goes to the Microsoft GS
Wavetable Synth.

Like `models/model.gguf` the file has no name of its own. The game always loads
`assets/audio/soundfont.sf2` (`SoundFontLocator`), and choosing a different font means replacing that
file. `--soundfont <file>` overrides it for one run, which is how candidates are compared. With no
file the music is off (a log line says so) and the UI ticks and the loading wash still play.

The file is **gitignored** (`assets/audio/*.sf2`): every candidate is larger than a few MB, and none
is authored here. Same reasoning as `models/README.md`.

## Which one — open decision for the developer

The font must allow redistribution in a commercial game. MeltySynth reads SF2 only, so the SF3
(Vorbis-compressed) fonts are out, MuseScore_General among them.

| candidate | bytes | licence | SHA-256 | source |
|---|---|---|---|---|
| **GeneralUser GS 2.0.3** (S. Christian Collins) | 32,319,396 | own licence: "use without restriction … private or commercial … feel free to use it in your software projects". Caveat in the licence itself: the author cannot vouch for the origin of every sample, though none came from commercial banks and nobody has complained since 2000 | `9575028c7a1f589f5770fccc8cff2734566af40cd26ed836944e9a5152688cfe` | <https://github.com/mrbumpy409/GeneralUser-GS>, `GeneralUser-GS.sf2` at commit `684543d5e5efaef08d02be50dcda8d552478fa60` (2026-02-23). The author asks that downloads link to his site or a local copy, not his files |
| **FluidR3_GM 3.1** (Frank Wen) | 148,398,306 | MIT | `74594e8f4250680adf590507a306655a299935343583256f3b722c48a1bc1cb0` | Ubuntu/Debian `fluid-soundfont-gm` 3.1-5.3, `/usr/share/sounds/sf2/FluidR3_GM.sf2` |
| **TimGM6mb 1.3** (Tim Brechbill, D. Bolton) | 5,969,788 | **GPL-2** — redistributable, but GPL terms attach to the file; whether that sits well beside a commercial game is a judgement to make, not one made here | `c5378b62028c920cb11e4803327983fee2f2cdff5dc89c708e39da417e51c854` | Ubuntu/Debian `timgm6mb-soundfont` 1.3-5, `/usr/share/sounds/sf2/TimGM6mb.sf2` |

**Suggested: GeneralUser GS.** It is a GS-compatible bank, the nearest in character to the
Microsoft GS Wavetable Synth the music was written against, and a sixth the size of FluidR3. Its
licence is explicit about commercial use; the sample-provenance caveat is the one thing to weigh.
FluidR3_GM is the clean-licence fallback at 148 MB. TimGM6mb is small enough to commit but GPL.

The Linux port's testing used GeneralUser GS. Verify a copy with `sha256sum assets/audio/soundfont.sf2`.

## Getting one

```bash
# GeneralUser GS (the git proxy in a cloud session serves public repos)
git clone --depth 1 https://github.com/mrbumpy409/GeneralUser-GS /tmp/gugs
cp /tmp/gugs/GeneralUser-GS.sf2 assets/audio/soundfont.sf2

# FluidR3_GM or TimGM6mb from the Ubuntu archive, without installing the package
apt-get download fluid-soundfont-gm && dpkg-deb -x fluid-soundfont-gm_*.deb /tmp/fluid
cp /tmp/fluid/usr/share/sounds/sf2/FluidR3_GM.sf2 assets/audio/soundfont.sf2
```
