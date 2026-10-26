#!/usr/bin/env python3
"""Timbre sheets: each synthesised instrument beside a sampled reference playing the same notes.

    timbre.py <audition-dir> [--sf2 GeneralUser-GS.sf2] [-o sheet.png]

`dotnet Cathedral.dll --music-audition <dir>` writes every instrument's WAV and its score. This
renders the same score on the General MIDI preset the instrument stands in for, through FluidSynth
and a SoundFont (GeneralUser GS by default: assets/audio/README.md says how to fetch it; it is a
measuring stick here, never shipped), and compares the two on what can be compared without ears:

    attack_s        time from 10% to 90% of the first note's envelope peak
    centroid_hz     median spectral centroid while sounding
    vib_rate_hz / vib_cents   vibrato from a pitch track of the phrase (pyin)
    envelope_db     1/3-octave long-term spectrum, normalised at its peak; the sheet overlays the two
    env_distance_db RMS difference of the two envelopes over 100 Hz - 8 kHz: the "does it sound like
                    that kind of instrument" proxy. Small is close in colour, not in quality.

The reference is a sampled ROMpler, which is near what the genre was played on, so a big distance
says the patch has drifted far from its namesake; it does not say which of the two is better.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import tempfile
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import mido  # noqa: E402
import numpy as np  # noqa: E402
from scipy import signal  # noqa: E402

sys.path.insert(0, str(Path(__file__).resolve().parent))
from musickit import acoustic  # noqa: E402

GM = {"Organ": 19, "OrganFlute": 19, "Choir": 52, "ChoirOoh": 53, "Strings": 49, "DarkPad": 95,
      "Recorder": 74, "Harpsichord": 6, "Harp": 46, "Lute": 24, "Bell": 14, "Timpani": 47,
      "HandDrum": 116, "Bourdon": 19, "Horn": 60, "SquareLead": 80, "Wind": 122,
      "Fiddle": 110, "Whistle": 78, "Pipes": 109, "Bodhran": 116}


def to_midi(score: dict, program: int, path: Path):
    mid = mido.MidiFile(ticks_per_beat=480)
    tr = mido.MidiTrack()
    mid.tracks.append(tr)
    tr.append(mido.MetaMessage("set_tempo", tempo=500000))
    tr.append(mido.Message("program_change", program=program, channel=0))
    tr.append(mido.Message("control_change", control=91, value=60, channel=0))
    ev = []
    for n in score["notes"]:
        ev.append((n["t"], 1, n["p"], int(n["v"] * 127)))
        ev.append((n["t"] + n["d"], 0, n["p"], 0))
    ev.sort()
    last = 0
    for t, on, p, v in ev:
        tick = int(t * 960)
        tr.append(mido.Message("note_on" if on else "note_off", note=p, velocity=v, time=tick - last, channel=0))
        last = tick
    tr.append(mido.Message("note_off", note=0, velocity=0, time=960 * 6, channel=0))
    mid.save(str(path))


def third_octave(x, sr):
    f, p = signal.welch(x, sr, nperseg=4096)
    centres = 100 * 2 ** (np.arange(0, 21) / 3)   # 100 Hz .. 10 kHz
    lv = []
    for c in centres:
        sel = (f >= c * 2 ** (-1 / 6)) & (f < c * 2 ** (1 / 6))
        lv.append(10 * np.log10(p[sel].sum() + 1e-20))
    lv = np.array(lv)
    return centres, lv - lv.max()


def features(path: Path, first_note_end: float) -> dict:
    x, sr = acoustic.load(path)
    m = x.mean(1)
    out = {}
    seg = m[: int(first_note_end * sr)]
    env = np.abs(signal.hilbert(seg)) if len(seg) else np.zeros(1)
    env = np.convolve(env, np.ones(int(sr * 0.005)) / int(sr * 0.005), "same")
    pk = env.max() + 1e-12
    i10 = np.argmax(env > 0.1 * pk)
    i90 = np.argmax(env > 0.9 * pk)
    out["attack_s"] = max(0.0, (i90 - i10) / sr)
    f, t, Z = signal.stft(m, sr, nperseg=2048)
    P = np.abs(Z) ** 2
    loud = P.sum(0)
    on = loud > loud.max() * 1e-3
    cen = (f[:, None] * P).sum(0) / (P.sum(0) + 1e-20)
    out["centroid_hz"] = float(np.median(cen[on])) if on.any() else 0.0
    try:
        import librosa
        y = librosa.resample(m[: int(sr * 5)], orig_sr=sr, target_sr=16000)
        f0, voiced, _ = librosa.pyin(y, fmin=60, fmax=1200, sr=16000, frame_length=2048)
        f0 = f0[voiced & np.isfinite(f0)]
        if len(f0) > 20:
            cents = 1200 * np.log2(f0 / np.median(f0))
            cents = cents[np.abs(cents) < 100]
            d = cents - signal.medfilt(cents, 31)
            out["vib_cents"] = float(np.std(d) * 2.8)
            spec = np.abs(np.fft.rfft(d * np.hanning(len(d))))
            fr = np.fft.rfftfreq(len(d), 512 / 16000)
            sel = (fr > 2) & (fr < 9)
            out["vib_rate_hz"] = float(fr[sel][np.argmax(spec[sel])]) if sel.any() else 0.0
    except Exception as e:  # pitch tracking is a nicety
        out["pyin_error"] = str(e)
    c, lv = third_octave(m, sr)
    out["_centres"] = c.tolist()
    out["_envelope"] = lv.tolist()
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("dir")
    ap.add_argument("--sf2", default="/tmp/gugs/GeneralUser-GS.sf2")
    ap.add_argument("-o", "--out")
    a = ap.parse_args()
    d = Path(a.dir)
    rows = []
    insts = sorted(p.stem for p in d.glob("*.wav") if not p.stem.endswith(".ref"))
    fig, axes = plt.subplots(len(insts), 3, figsize=(18, 2.6 * len(insts)), facecolor="#111",
                             gridspec_kw={"width_ratios": [2, 2, 1.2]})
    for row, inst in enumerate(insts):
        score = json.loads((d / f"{inst}.score.json").read_text())
        ref = d / f"{inst}.ref.wav"
        if not ref.exists() and Path(a.sf2).exists():
            with tempfile.TemporaryDirectory() as td:
                mp = Path(td) / "x.mid"
                to_midi(score, GM.get(inst, 0), mp)
                subprocess.run(["fluidsynth", "-ni", "-g", "0.7", "-r", "32000", "-F", str(ref), a.sf2, str(mp)],
                               capture_output=True)
        first_end = score["notes"][0]["d"] + 0.05
        mine = features(d / f"{inst}.wav", first_end)
        theirs = features(ref, first_end) if ref.exists() else None
        r = {"inst": inst, **{k: v for k, v in mine.items() if not k.startswith("_")}}
        if theirs:
            c = np.array(mine["_centres"])
            sel = (c >= 100) & (c <= 8000)
            r["env_distance_db"] = float(np.sqrt(np.mean((np.array(mine["_envelope"])[sel] - np.array(theirs["_envelope"])[sel]) ** 2)))
            r["ref"] = {k: v for k, v in theirs.items() if not k.startswith("_")}
        rows.append(r)
        for col, (path, title) in enumerate([(d / f"{inst}.wav", f"{inst} (ours)"), (ref, f"GM {GM.get(inst)} (reference)")]):
            ax = axes[row, col]
            if path.exists():
                x, sr = acoustic.load(path)
                f, t, Z = signal.stft(x.mean(1), sr, nperseg=1024, noverlap=768)
                S = 20 * np.log10(np.abs(Z) + 1e-7)
                ax.pcolormesh(t, f, S, shading="auto", cmap="magma", vmin=S.max() - 75, vmax=S.max())
                ax.set_yscale("log")
                ax.set_ylim(50, 12000)
            ax.set_title(title, color="#ddd", fontsize=9)
            ax.tick_params(colors="#aaa", labelsize=7)
        ax = axes[row, 2]
        ax.semilogx(mine["_centres"], mine["_envelope"], color="#e8c547", label="ours")
        if theirs:
            ax.semilogx(theirs["_centres"], theirs["_envelope"], color="#4cc9f0", label="ref")
        ax.set_ylim(-70, 3)
        ax.set_facecolor("#181818")
        ax.tick_params(colors="#aaa", labelsize=7)
        txt = f"att {mine['attack_s']*1000:.0f}ms cen {mine['centroid_hz']:.0f}"
        if theirs:
            txt += f"\nref att {theirs['attack_s']*1000:.0f}ms cen {theirs['centroid_hz']:.0f}\nenv dist {r['env_distance_db']:.1f} dB"
        ax.text(0.02, 0.05, txt, transform=ax.transAxes, color="#ddd", fontsize=7, family="monospace")
    out = Path(a.out) if a.out else d / "timbre.png"
    fig.tight_layout()
    fig.savefig(out, dpi=60, facecolor=fig.get_facecolor())
    (out.with_suffix(".json")).write_text(json.dumps(rows, indent=1))
    for r in rows:
        ref = r.get("ref", {})
        print(f"{r['inst']:12s} att {r['attack_s']*1000:6.0f}ms ({ref.get('attack_s', 0)*1000:5.0f})  "
              f"cen {r['centroid_hz']:6.0f} ({ref.get('centroid_hz', 0):6.0f})  "
              f"vib {r.get('vib_rate_hz', 0):4.1f}Hz {r.get('vib_cents', 0):4.0f}c "
              f"({ref.get('vib_rate_hz', 0):4.1f} {ref.get('vib_cents', 0):4.0f})  env {r.get('env_distance_db', float('nan')):5.1f} dB")
    print(f"sheet: {out}")


if __name__ == "__main__":
    main()
