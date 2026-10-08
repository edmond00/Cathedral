"""The game's dither, outside the game.

A transcription of the fragment shader in src/glyph/PostProcessRenderer.cs (mode 1, `Bayer8`) at
the game's resting settings (`Config.PostProcess`: 6 levels per channel, strength 1): each channel is
nudged by an 8x8 ordered threshold and snapped to one of six levels.

Two properties the pipeline leans on:

* **Idempotent on game footage.** A frame the game already dithered sits on the six-level lattice,
  and re-dithering a lattice value returns it unchanged whatever the threshold. So the whole finished
  video can be passed through here once — manim scenes, title cards and fades included — and the
  recorded game footage comes out the same, while everything else takes on its look.
* **Same matrix, same origin.** The shader indexes the matrix from the bottom-left (GL's origin);
  so does this, so a pattern drawn here lines up with one drawn there.
"""

from __future__ import annotations

import subprocess
from pathlib import Path

import numpy as np

from . import ffmpeg

_BAYER8 = np.array([
    [0, 32, 8, 40, 2, 34, 10, 42],
    [48, 16, 56, 24, 50, 18, 58, 26],
    [12, 44, 4, 36, 14, 46, 6, 38],
    [60, 28, 52, 20, 62, 30, 54, 22],
    [3, 35, 11, 43, 1, 33, 9, 41],
    [51, 19, 59, 27, 49, 17, 57, 25],
    [15, 47, 7, 39, 13, 45, 5, 37],
    [63, 31, 55, 23, 61, 29, 53, 21],
], dtype=np.float32) / 64.0 - 0.5

_threshold_cache: dict[tuple[int, int, int], np.ndarray] = {}


def _threshold(h: int, w: int, scale: int) -> np.ndarray:
    key = (h, w, scale)
    t = _threshold_cache.get(key)
    if t is None:
        # GL row 0 is the bottom of the image; cells are `scale` pixels wide.
        ys = ((h - 1 - np.arange(h)) // scale) % 8
        xs = (np.arange(w) // scale) % 8
        t = _BAYER8[ys[:, None], xs[None, :]][:, :, None].astype(np.float32)
        _threshold_cache[key] = t
    return t


def dither_frame(frame: np.ndarray, levels: int = 6, scale: int = 1, strength: float = 1.0, snap: int = 8) -> np.ndarray:
    """Dithers one HxWx3 uint8 RGB frame exactly as the game's post-process does.

    `snap`: a channel within this many units of a lattice level is taken to be already dithered (game
    footage, after the codec) and set to that level exactly. 0 dithers everything.
    """
    h, w, _ = frame.shape
    steps = float(levels - 1)
    if scale > 1:
        # The shader point-samples each cell's centre, so the frame reads as low resolution.
        cy = np.minimum((np.arange(h) // scale) * scale + scale // 2, h - 1)
        cx = np.minimum((np.arange(w) // scale) * scale + scale // 2, w - 1)
        frame = frame[cy[:, None], cx[None, :]]
    x = frame.astype(np.float32) * (1.0 / 255.0)
    d = np.floor(x * steps + 0.5 + _threshold(h, w, scale)) * (1.0 / steps)
    if snap > 0:
        # Footage the game already dithered sits on the lattice give or take what the codec moved it;
        # put it back exactly instead of letting a threshold near 0.5 flip it a whole level.
        level = np.round(x * steps)
        near = np.abs(x * steps - level) < snap * steps / 255.0
        d = np.where(near, level * (1.0 / steps), d)
    if strength < 1.0:
        d = x + (d - x) * strength
    return (np.clip(d, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8)


def dither_video(src: str | Path, dst: str | Path, levels: int = 6, scale: int = 1, strength: float = 1.0,
                 encode: list[str] | None = None, audio_from: str | Path | None = None) -> None:
    """Decodes `src`, dithers every frame, and encodes `dst` (optionally muxing audio from another file)."""
    w, h = ffmpeg.video_size(src)
    decode = subprocess.Popen(
        [ffmpeg.FFMPEG, "-hide_banner", "-loglevel", "error", "-i", str(src), "-f", "rawvideo", "-pix_fmt", "rgb24", "-"],
        stdout=subprocess.PIPE)
    fps = _fps_of(src)
    enc_cmd = [ffmpeg.FFMPEG, "-y", "-hide_banner", "-loglevel", "error",
               "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{w}x{h}", "-r", fps, "-i", "-"]
    if audio_from:
        enc_cmd += ["-i", str(audio_from), "-map", "0:v", "-map", "1:a", "-c:a", "aac", "-b:a", "192k", "-shortest"]
    enc_cmd += (encode or ffmpeg.INTERMEDIATE) + [str(dst)]
    encoder = subprocess.Popen(enc_cmd, stdin=subprocess.PIPE)

    size = w * h * 3
    try:
        while True:
            buf = decode.stdout.read(size)
            if len(buf) < size:
                break
            frame = np.frombuffer(buf, dtype=np.uint8).reshape(h, w, 3)
            encoder.stdin.write(dither_frame(frame, levels, scale, strength).tobytes())
    finally:
        encoder.stdin.close()
        decode.wait()
        if encoder.wait() != 0:
            raise RuntimeError(f"encoding {dst} failed")


def _fps_of(path: str | Path) -> str:
    proc = subprocess.run([ffmpeg.FFMPEG, "-hide_banner", "-i", str(path)], capture_output=True, text=True,
                          encoding="utf-8", errors="replace")
    import re
    m = re.search(r"(\d+(?:\.\d+)?) fps", proc.stderr)
    return m.group(1) if m else "30"
