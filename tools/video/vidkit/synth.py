"""A small offline synthesiser for the game's MIDI.

In the game the music is played live by the Windows GS wavetable through `AmbianceEngine`; a recording
(`--record`) and `--export-music` capture the same stream as a MIDI file instead. This renders such a
file to audio with no soundfont and nothing to install: every General MIDI program the composer uses
(`ProceduralMidiComposer.Patch*`) is mapped to one of a handful of voices built from numpy — additive
organ and flute, detuned-saw pads and strings, a formant choir, Karplus-Strong plucks, square and saw
leads, FM bells — then a long dark reverb, a little tape grit, and a limiter.

It will not sound like the GS synth. It is meant to sound like the same music on a poorer, older
instrument, which is the right side to err on for this game.

Controllers honoured: 7 (volume), 11 (expression), 74 (brightness: the filter cutoff), 91 (reverb send).
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import mido
import numpy as np
from scipy import signal

RATE = 48000


@dataclass
class Note:
    start: float
    end: float
    channel: int
    pitch: int
    velocity: int
    program: int
    bright: float     # 0..1, from CC 74
    gain: float       # CC 7 * CC 11
    reverb: float     # 0..1, from CC 91


def read_notes(path: str | Path) -> tuple[list[Note], float]:
    """Every note of a MIDI file with the controller state it started under, and the file's length."""
    mid = mido.MidiFile(str(path))
    t = 0.0
    program = [0] * 16
    vol = [100] * 16
    expr = [127] * 16
    bright = [64] * 16
    reverb = [40] * 16
    open_notes: dict[tuple[int, int], tuple[float, int]] = {}
    notes: list[Note] = []

    def close(ch: int, p: int, at: float):
        if (ch, p) in open_notes:
            st, vel = open_notes.pop((ch, p))
            notes.append(Note(st, max(at, st + 0.03), ch, p, vel, program[ch], bright[ch] / 127,
                              (vol[ch] / 127) * (expr[ch] / 127), reverb[ch] / 127))

    for msg in mid:
        t += msg.time
        if msg.type == "program_change":
            program[msg.channel] = msg.program
        elif msg.type == "control_change":
            {7: vol, 11: expr, 74: bright, 91: reverb}.get(msg.control, [0] * 16)[msg.channel] = msg.value
        elif msg.type == "note_on" and msg.velocity > 0:
            close(msg.channel, msg.note, t)
            open_notes[(msg.channel, msg.note)] = (t, msg.velocity)
        elif msg.type in ("note_off", "note_on"):
            close(msg.channel, msg.note, t)
    for (ch, p) in list(open_notes):
        close(ch, p, t + 0.5)
    return notes, t


# ── voices ────────────────────────────────────────────────────────────────────

def _env(n: int, a: float, d: float, s: float, r: float, held: int) -> np.ndarray:
    """ADSR over n samples, the note held for `held` samples before its release."""
    t = np.arange(n) / RATE
    held_t = held / RATE
    env = np.where(t < a, t / max(a, 1e-4), s + (1 - s) * np.exp(-(t - a) / max(d, 1e-4)))
    rel = np.exp(-np.maximum(t - held_t, 0) / max(r, 1e-4))
    return (env * np.where(t > held_t, rel, 1.0)).astype(np.float32)


def _saw(phase: np.ndarray) -> np.ndarray:
    return 2.0 * (phase % 1.0) - 1.0


def _lowpass(x: np.ndarray, cutoff: float) -> np.ndarray:
    cutoff = float(np.clip(cutoff, 80, RATE * 0.45))
    return signal.sosfilt(signal.butter(2, cutoff, fs=RATE, output="sos"), x).astype(np.float32)


def _family(program: int, channel: int) -> str:
    if channel == 9:
        return "noise"
    if program == 19 or 16 <= program <= 23:
        return "organ"
    if program in (52, 53, 54):
        return "choir"
    if 40 <= program <= 51:
        return "strings"
    if 72 <= program <= 79 or program == 82:
        return "flute"
    if program in (6, 15) or 24 <= program <= 31 or 104 <= program <= 108:
        return "pluck"
    if program in (80, 81, 83, 84, 85, 86, 87):
        return "lead"
    if 32 <= program <= 39:
        return "bass"
    if program in (98, 102, 8, 9, 10, 11, 12, 13, 14):
        return "bell"
    if program in (122, 123, 124, 125, 126, 127, 103):
        return "noise"
    return "pad"


def render_note(n: Note) -> np.ndarray:
    fam = _family(n.program, n.channel)
    f = 440.0 * 2 ** ((n.pitch - 69) / 12)
    held = int((n.end - n.start) * RATE)
    release = {"pad": 2.2, "strings": 1.0, "choir": 1.2, "organ": 0.25, "flute": 0.3, "lead": 0.12,
               "bass": 0.2, "pluck": 1.6, "bell": 2.5, "noise": 0.4}[fam]
    total = held + int(release * 4 * RATE)
    t = np.arange(total, dtype=np.float32) / RATE
    vib = 1 + 0.0035 * np.sin(2 * np.pi * 5.2 * t) * np.clip(t * 2, 0, 1)
    amp = (n.velocity / 127) ** 1.6 * n.gain
    cut = 600 + 5200 * n.bright

    if fam == "organ":
        x = sum(w * np.sin(2 * np.pi * f * k * t) for k, w in [(1, 1), (2, .5), (3, .3), (4, .25), (6, .12), (8, .1)])
        x *= 1 + 0.08 * np.sin(2 * np.pi * 6.5 * t)
        x *= _env(total, 0.03, 0.3, 0.85, release, held)
    elif fam == "flute":
        x = np.sin(2 * np.pi * f * vib * t) + 0.18 * np.sin(4 * np.pi * f * t)
        breath = _lowpass(np.random.default_rng(n.pitch).standard_normal(total).astype(np.float32), f * 3) * 0.08
        x = (x + breath) * _env(total, 0.07, 0.4, 0.8, release, held)
    elif fam == "choir":
        src = _saw(f * vib * t) + _saw(f * 1.004 * t)
        x = np.zeros_like(src)
        for fc, bw, g in [(700, 110, 1.0), (1150, 130, .6), (2600, 250, .25)]:   # an open "ah"
            b, a = signal.iirpeak(fc, fc / bw, fs=RATE)
            x += g * signal.lfilter(b, a, src)
        x *= _env(total, 0.35, 0.8, 0.8, release, held) * 0.6
    elif fam in ("strings", "pad"):
        spread = [0.993, 1.0, 1.007] if fam == "pad" else [0.997, 1.003]
        x = sum(_saw(f * s * vib * t + i * 0.31) for i, s in enumerate(spread)) / len(spread)
        x = _lowpass(x, cut * (0.6 if fam == "pad" else 0.9))
        a = 0.9 if fam == "pad" else 0.25
        x *= _env(total, a, 1.5, 0.8, release, held) * 0.8
    elif fam == "lead":
        sq = np.sign(np.sin(2 * np.pi * f * t)) if n.program == 80 else _saw(f * t)
        x = _lowpass(sq, cut * 1.2) * _env(total, 0.01, 0.15, 0.6, release, held) * 0.45
    elif fam == "bass":
        x = _lowpass(_saw(f * t) + 0.5 * np.sign(np.sin(2 * np.pi * f * t)), cut * 0.5)
        x *= _env(total, 0.01, 0.3, 0.7, release, held) * 0.7
    elif fam == "pluck":
        period = max(2, int(RATE / f))
        rng = np.random.default_rng(n.pitch * 31 + n.velocity)
        buf = rng.uniform(-1, 1, period).astype(np.float32)
        out = np.empty(total, np.float32)
        decay = 0.996 if n.program == 15 else 0.993
        for i in range(0, total, period):     # Karplus-Strong, one period at a time
            out[i:i + period] = buf[: min(period, total - i)]
            buf = decay * 0.5 * (buf + np.roll(buf, -1))
        x = out * _env(total, 0.002, 2.0, 0.0, release, held) * 0.9
    elif fam == "bell":
        mod = np.sin(2 * np.pi * f * 3.5 * t) * 2.2 * np.exp(-t * 1.5)
        x = np.sin(2 * np.pi * f * t + mod) * _env(total, 0.003, 1.2, 0.0, release, held) * 0.5
    else:  # noise: wind, waves, percussion, the industrial FX patches
        rng = np.random.default_rng(n.pitch * 7 + n.channel)
        x = _lowpass(rng.standard_normal(total).astype(np.float32), 300 + 30 * n.pitch)
        x *= _env(total, 0.2 if n.channel != 9 else 0.002, 0.5, 0.3 if n.channel != 9 else 0.0, release, held) * 0.35
    return (x * amp).astype(np.float32)


def _reverb(x: np.ndarray, seconds: float = 3.2, seed: int = 3) -> np.ndarray:
    rng = np.random.default_rng(seed)
    n = int(RATE * seconds)
    env = np.exp(-np.linspace(0, 8, n))
    ir_l = rng.standard_normal(n) * env
    ir_r = rng.standard_normal(n) * env
    ir_l = _lowpass(ir_l.astype(np.float32), 4500)
    ir_r = _lowpass(ir_r.astype(np.float32), 4500)
    ir_l /= np.sqrt(np.sum(ir_l ** 2)); ir_r /= np.sqrt(np.sum(ir_r ** 2))
    return np.stack([signal.fftconvolve(x, ir_l)[: len(x)], signal.fftconvolve(x, ir_r)[: len(x)]], axis=1)


def render(path: str | Path, length: float | None = None, grit: float = 0.25) -> np.ndarray:
    """Renders a MIDI file to stereo float32 at 48 kHz, `length` seconds long (default: the file's own)."""
    notes, file_len = read_notes(path)
    length = length or file_len + 3.0
    total = int(length * RATE) + RATE * 8
    dry = np.zeros(total, np.float32)
    send = np.zeros(total, np.float32)
    for n in notes:
        if n.start >= length:
            continue
        x = render_note(n)
        i = int(n.start * RATE)
        j = min(total, i + len(x))
        dry[i:j] += x[: j - i]
        send[i:j] += x[: j - i] * (0.35 + 0.65 * n.reverb)

    wet = _reverb(send)
    out = np.stack([dry, dry], axis=1) * 0.75 + wet * 0.55
    out = out[: int(length * RATE)]

    if grit > 0:                         # tape: a little hiss, a little wow, a softened top
        rng = np.random.default_rng(11)
        out = signal.sosfilt(signal.butter(2, 11000 - 6000 * grit, fs=RATE, output="sos"), out, axis=0)
        out += rng.standard_normal(out.shape) * 10 ** ((-60 + 14 * grit) / 20)

    peak = np.max(np.abs(out)) + 1e-9
    out = np.tanh(out / peak * 1.4) / np.tanh(1.4) * 0.9
    return out.astype(np.float32)
