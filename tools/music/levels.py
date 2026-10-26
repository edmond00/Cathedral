#!/usr/bin/env python3
"""Level every instrument: render the audition, measure, write the corrections into Voices.cs.

    levels.py [--apply]

Each instrument's audition (the same phrase and chords at velocity 0.6-0.7) is measured as
loudness while sounding. Sustained voices are brought to -16 dB, plucked and struck ones to -19
(their attacks carry more than their average says). With --apply the table between LEVELS-BEGIN
and LEVELS-END in src/audio/music/Voices.cs is rewritten, multiplying into what is there, so it
can be run again until it converges.
"""

import argparse
import re
import subprocess
import sys
import tempfile
from pathlib import Path

import numpy as np
import soundfile as sf

sys.path.insert(0, str(Path(__file__).resolve().parent))
from musickit import ROOT  # noqa: E402

STRUCK = {"Harpsichord", "Harp", "Lute", "Bell", "Timpani", "HandDrum"}
VOICES = ROOT / "src" / "audio" / "music" / "Voices.cs"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    a = ap.parse_args()
    with tempfile.TemporaryDirectory() as td:
        subprocess.run(["dotnet", str(ROOT / "bin/Debug/net8.0/Cathedral.dll"), "--music-audition", td],
                       check=True, capture_output=True)
        meas = {}
        for f in sorted(Path(td).glob("*.wav")):
            x, sr = sf.read(f)
            m = x.mean(1)
            frame = sr // 20
            n = len(m) // frame
            e = (m[: n * frame].reshape(n, frame) ** 2).mean(1)
            db = 10 * np.log10(e + 1e-12)
            loud = e[db > db.max() - 30]
            meas[f.stem] = 10 * np.log10(loud.mean())
    src = VOICES.read_text()
    current = {k: float(v) for k, v in re.findall(r"\[Instrument\.(\w+)\] = ([\d.]+)f", src)}
    lines = []
    for inst, db in meas.items():
        target = -19.0 if inst in STRUCK else -16.0
        corr = 10 ** ((target - db) / 20)
        new = current.get(inst, 1.0) * corr
        print(f"{inst:12s} {db:6.1f} dB  -> x{corr:5.2f}  (table {new:.3f})")
        lines.append(f"        [Instrument.{inst}] = {new:.3f}f,")
    if a.apply:
        body = "\n".join(lines)
        src = re.sub(r"(// LEVELS-BEGIN\n).*?(\s*// LEVELS-END)", lambda m: m.group(1) + body + m.group(2), src, flags=re.S)
        VOICES.write_text(src)
        print("written to", VOICES)


if __name__ == "__main__":
    main()
