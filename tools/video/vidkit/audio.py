"""Mixing the soundtrack: voice, the cursor's clicks, and the music bed under both.

Everything is float32 stereo at 48 kHz in numpy; one WAV comes out and is muxed onto the picture.
"""

from __future__ import annotations

import numpy as np
from scipy import signal

RATE = 48000


def silence(seconds: float) -> np.ndarray:
    return np.zeros((int(round(seconds * RATE)), 2), np.float32)


def place(track: np.ndarray, clip: np.ndarray, at: float, gain: float = 1.0, pan: float = 0.0) -> None:
    """Adds `clip` (mono or stereo) into `track` starting at `at` seconds. Pan -1 left .. 1 right."""
    if clip.ndim == 1:
        l, r = np.cos((pan + 1) * np.pi / 4), np.sin((pan + 1) * np.pi / 4)
        clip = np.stack([clip * l * np.sqrt(2), clip * r * np.sqrt(2)], axis=1)
    i = int(round(at * RATE))
    if i >= len(track) or i + len(clip) <= 0:
        return
    a, b = max(i, 0), min(i + len(clip), len(track))
    track[a:b] += clip[a - i: b - i] * gain


def envelope(active: np.ndarray, attack: float = 0.12, release: float = 0.7) -> np.ndarray:
    """A smooth 0..1 envelope following a boolean mask (per sample)."""
    up = 1 - np.exp(-1 / (attack * RATE))
    down = 1 - np.exp(-1 / (release * RATE))
    # one-pole follower, run at 1/64 rate and stretched: plenty smooth, and fast in pure numpy
    step = 64
    m = active[::step].astype(np.float32)
    out = np.empty_like(m)
    level = 0.0
    a_up, a_down = 1 - (1 - up) ** step, 1 - (1 - down) ** step
    for k, v in enumerate(m):
        level += (v - level) * (a_up if v > level else a_down)
        out[k] = level
    return np.repeat(out, step)[: len(active)]


def duck(music: np.ndarray, voice: np.ndarray, depth: float = 0.55) -> np.ndarray:
    """Lowers `music` by up to `depth` wherever `voice` is speaking."""
    mono = np.abs(voice).mean(axis=1) if voice.ndim == 2 else np.abs(voice)
    # bridge the gaps between words, so the bed stays down for a whole sentence
    active = signal.convolve(mono > 0.01, np.ones(int(0.35 * RATE)), mode="same") > 0
    env = envelope(active)
    return music * (1 - depth * env)[:, None]


def limiter(x: np.ndarray, ceiling: float = 0.94) -> np.ndarray:
    peak = np.max(np.abs(x)) + 1e-9
    if peak > ceiling:
        x = np.tanh(x / peak * 1.2) / np.tanh(1.2) * ceiling
    return x.astype(np.float32)


def rms_db(x: np.ndarray) -> float:
    return float(20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-12))


def normalise(x: np.ndarray, target_db: float) -> np.ndarray:
    """Scales `x` so its RMS sits at `target_db` dBFS (silence-aware: only loud passages count)."""
    mono = x.mean(axis=1) if x.ndim == 2 else x
    frames = mono[: len(mono) // 2400 * 2400].reshape(-1, 2400)
    loud = frames[np.sqrt((frames ** 2).mean(axis=1)) > 1e-3]
    if len(loud) == 0:
        return x
    current = 20 * np.log10(np.sqrt((loud ** 2).mean()) + 1e-12)
    return (x * 10 ** ((target_db - current) / 20)).astype(np.float32)
