#!/usr/bin/env python3
"""Calibrate the melodic bands on real tunes.

    exemplars.py [--repeats 2]

Each file in exemplars/ is a public-domain melody written out by hand (`Name+octave:beats`, bars
split by `|`). It is played through `--repeats` times, as a dungeon synth piece would loop its
tune, with a breath of one beat between phrases, and measured with the same symbolic metrics as a
render. Where a tune falls outside a band, the band is suspect before the tune is.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from musickit import score as sm, symbolic, targets  # noqa: E402

PC = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
MELODIC = ["mode_fit", "pc_entropy", "mel_step_ratio", "mel_leap_ratio", "mel_large_leap_ratio", "mel_range_phrase",
           "mel_distinct_phrase", "mel_density", "motif_recurrence", "bar_repetition", "phrase_len_cv", "grid_alignment"]


def parse(path: Path, repeats: int) -> sm.Score:
    meter, bpm, toks = 4, 72, []
    for line in path.read_text().splitlines():
        line = line.strip()
        if line.startswith("#"):
            continue
        if not line:
            continue
        if line.startswith("meter"):
            meter = int(line.split()[1]); continue
        if line.startswith("bpm"):
            bpm = float(line.split()[1]); continue
        toks += line.split()
    beat = 60 / bpm
    notes, t = [], 0.0
    for _ in range(repeats):
        for tok in toks:
            if tok == "|":
                continue
            m = re.fullmatch(r"([A-G])(#|b)?(-?\d):([\d.]+)", tok)
            name, acc, octv, dur = m.groups()
            p = 12 * (int(octv) + 1) + PC[name] + (1 if acc == "#" else -1 if acc == "b" else 0)
            d = float(dur) * beat
            notes.append(sm.Note(t, d * 0.95, p, 0.7, "melody"))
            t += d
    total = t
    notes.append(sm.Note(0, total, notes[0].p - 24, 0.5, "drone"))
    return sm.Score(notes, total, pieces=[{"t": 0, "bpm": bpm, "meter": meter}], meta={"engine": "exemplar"})


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--repeats", type=int, default=2)
    a = ap.parse_args()
    files = sorted((Path(__file__).parent / "exemplars").glob("*.txt"))
    rows = {f.stem: symbolic.analyse(parse(f, a.repeats)) for f in files}
    print(f"{'metric':22s}" + "".join(f"{k[:12]:>13s}" for k in rows) + "   band")
    for m in MELODIC:
        lo, hi = targets.band(m)
        cells = []
        for k, r in rows.items():
            v = r.get(m, float("nan"))
            flag = " " if lo <= v <= hi else "*"
            cells.append(f"{v:12.3g}{flag}")
        print(f"{m:22s}" + "".join(cells) + f"   {lo}..{hi}")


if __name__ == "__main__":
    main()
