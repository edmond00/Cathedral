"""The city's music, as a loop for the slide before the city (build/city_loop.wav).

    python make_city_loop.py

The game's music never loops: it is composed as it plays. A slide has no game under it, so it borrows the
city's: the place's mood rendered offline (--render-music, the very composer and synthesiser the game
runs), cut on the composer's own section boundaries. The piece is a reel in the session form, its tune
twice round (A A' B B' A' A' B' B'); the second time round (A' A' B' B', thirty-two bars) starts after a
B' and ends on one, so the hall's reverb at the seam is the same on both sides and the loop has no join to
hear. Levelled to the take's own music in the city, so the slide sits at the loudness of the scenes either
side of it.
"""
import json
import subprocess
import sys
from pathlib import Path

import numpy as np
import soundfile as sf

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
MOOD, SEED, SECONDS = "city:27788", 1, 200
RAW = HERE / "build" / "city_render.wav"
OUT = HERE / "build" / "city_loop.wav"
# Matched in RMS to the take's music, nothing more. (A 5 dB cut was tried on the evidence of the built
# soundtrack's loudness, which was wrong: every slide in the video measures the same, -15 dB, with or without
# this loop under it. The narration, unbroken over a slide, is what makes a slide louder than a scene.)
TRIM_DB = 0.0


def main():
    subprocess.run(["dotnet", str(ROOT / "bin/Debug/net8.0/Cathedral.dll"), "--render-music", str(RAW),
                    "--seconds", str(SECONDS), "--mood", MOOD, "--seed", str(SEED)], check=True, cwd=ROOT)
    score = json.loads(RAW.with_suffix(".score.json").read_text())
    piece = [s for s in score["sections"] if s["piece"] == 0]
    labels = [s["label"] for s in piece]
    # the tune's second time round: from the A' that follows the first B' to the end of the piece
    first_b2 = labels.index("B'")
    start = piece[first_b2 + 1]["t"]
    end = next(s["t"] for s in score["sections"] if s["piece"] == 1)
    x, sr = sf.read(str(RAW), dtype="float32", always_2d=True)
    loop = x[int(start * sr):int(end * sr)].copy()
    # at the seam the loop's end runs into its start: blend its head from what really followed its end
    # into its own beginning over 10 ms, against a click
    k = int(0.01 * sr)
    ramp = np.linspace(0, 1, k)[:, None]
    after = x[int(end * sr):int(end * sr) + k]
    loop[:k] = after * (1 - ramp) + loop[:k] * ramp
    # the take's music in the city, sung under the scenes the slide sits between
    take = HERE / "build" / "rec_city3" / "music.wav"
    t, tsr = sf.read(str(take), dtype="float32", always_2d=True)
    ref = t[int(400 * tsr):int(800 * tsr)]
    gain = np.sqrt((ref ** 2).mean()) / max(1e-9, np.sqrt((loop ** 2).mean()))
    gain *= 10 ** (TRIM_DB / 20)
    sf.write(str(OUT), loop * gain, sr)
    print(f"{OUT}: {end - start:.2f}s ({labels[first_b2 + 1:]} of piece 0), gain {20 * np.log10(gain):+.1f} dB")


if __name__ == "__main__":
    sys.exit(main())
