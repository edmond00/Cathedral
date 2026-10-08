"""Voice-over: Piper, local and free, with a little grit.

Piper (https://github.com/rhasspy/piper) runs a VITS voice on the CPU in well under real time. Voices
live in tools/video/voices (`setup.ps1` fetches them); `en_GB-alan-medium` is the default narrator.

The grit is the game's flavour applied to a voice: the game's picture is quantised and its music is
a GS wavetable, so a pristine studio voice over it sounds pasted on. `grit` (0..1) scales one chain —
band-limiting, soft saturation, a light bit-crush and sample-hold, a noise floor with sparse crackle,
and a small room — from barely there (0.15) to an old wax cylinder (1.0). 0.35 is the house setting.
"""

from __future__ import annotations

import hashlib
import json
import wave
from pathlib import Path

import numpy as np
from scipy import signal

from . import VOICES

RATE = 48000
DEFAULT_VOICE = "en_GB-alan-medium"

_loaded: dict[str, object] = {}


def _voice(name: str):
    if name not in _loaded:
        from piper import PiperVoice
        model = VOICES / f"{name}.onnx"
        if not model.exists():
            raise FileNotFoundError(f"voice {name} not found in {VOICES} — run tools/video/setup.ps1")
        _loaded[name] = PiperVoice.load(str(model))
    return _loaded[name]


def available_voices() -> list[str]:
    return sorted(p.stem for p in VOICES.glob("*.onnx"))


def _raw(text: str, voice: str, speed: float) -> tuple[np.ndarray, int]:
    from piper import SynthesisConfig
    v = _voice(voice)
    chunks, rate = [], 22050
    for chunk in v.synthesize(text, syn_config=SynthesisConfig(length_scale=1.0 / max(speed, 0.1))):
        chunks.append(chunk.audio_float_array)
        rate = chunk.sample_rate
    audio = np.concatenate(chunks) if chunks else np.zeros(1, np.float32)
    return audio.astype(np.float32), rate


def grit_chain(x: np.ndarray, rate: int, grit: float, seed: int = 7) -> np.ndarray:
    """The game-flavoured voice chain. `x` is mono float at `rate`; returns the same."""
    if grit <= 0:
        return x
    rng = np.random.default_rng(seed)        # seeded: the same line renders the same every time

    # Band: a narrower window as grit rises — never a telephone, just not a studio.
    lo = 70 + 110 * grit
    hi = 9500 - 5000 * grit
    sos = signal.butter(4, [lo, hi], btype="band", fs=rate, output="sos")
    y = signal.sosfilt(sos, x)

    # Presence bump around 2 kHz keeps the diction readable once the top is gone.
    b, a = signal.iirpeak(2100, 1.4, fs=rate)
    y = y + 0.35 * grit * signal.lfilter(b, a, y)

    # Soft saturation.
    drive = 1 + 3 * grit
    y = np.tanh(y * drive) / np.tanh(drive)

    # Bit-crush and sample-hold: the voice's own small dither.
    bits = 16 - 8 * grit
    q = 2 ** (bits - 1)
    y = np.round(y * q) / q
    hold = 1 + int(round(2 * grit))
    if hold > 1:
        y = np.repeat(y[::hold], hold)[: len(y)]
        y = signal.sosfilt(signal.butter(2, hi * 0.9, fs=rate, output="sos"), y)

    # Noise floor (pink-ish) and sparse crackle.
    n = rng.standard_normal(len(y)).astype(np.float32)
    n = signal.lfilter([0.049922035, -0.095993537, 0.050612699, -0.004408786], [1, -2.494956002, 2.017265875, -0.522189400], n)
    n /= np.max(np.abs(n)) + 1e-9
    y = y + n * (10 ** ((-46 + 12 * grit) / 20))
    crackle = (rng.random(len(y)) < 0.00025 * grit) * rng.standard_normal(len(y)) * 0.18 * grit
    y = y + signal.lfilter([1, -0.6], [1], crackle)

    # A small stone room.
    ir_len = int(rate * (0.18 + 0.25 * grit))
    ir = rng.standard_normal(ir_len) * np.exp(-np.linspace(0, 7, ir_len))
    ir[0] = 0
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    wet = signal.fftconvolve(y, ir)[: len(y)]
    y = y + wet * (0.08 + 0.12 * grit)

    peak = np.max(np.abs(y)) + 1e-9
    return (y / peak * 0.89).astype(np.float32)


def speak(text: str, out: str | Path, voice: str = DEFAULT_VOICE, speed: float = 1.0, grit: float = 0.35,
          cache_dir: str | Path | None = None) -> tuple[Path, float]:
    """Renders `text` to a 48 kHz mono WAV at `out`. Returns the path and its duration in seconds.

    Identical requests are served from `cache_dir` (keyed on the text and every setting), so rebuilding a
    video after changing one line re-voices only that line.
    """
    out = Path(out)
    key = hashlib.sha1(json.dumps([text, voice, speed, grit, 3]).encode()).hexdigest()[:16]
    if cache_dir:
        cached = Path(cache_dir) / f"vo_{key}.wav"
        if cached.exists():
            if cached.resolve() != out.resolve():
                out.write_bytes(cached.read_bytes())
            return out, wav_duration(out)

    audio, rate = _raw(text, voice, speed)
    audio = signal.resample_poly(audio, RATE, rate).astype(np.float32)
    # a breath of silence either side, so a cut never clips a consonant
    pad = np.zeros(int(RATE * 0.12), np.float32)
    audio = np.concatenate([pad, grit_chain(audio, RATE, grit, seed=int(key[:6], 16)), pad])
    write_wav(out, audio, RATE)
    if cache_dir:
        Path(cache_dir).mkdir(parents=True, exist_ok=True)
        (Path(cache_dir) / f"vo_{key}.wav").write_bytes(out.read_bytes())
    return out, len(audio) / RATE


def write_wav(path: str | Path, audio: np.ndarray, rate: int = RATE) -> None:
    a = np.asarray(audio)
    channels = 1 if a.ndim == 1 else a.shape[1]
    data = (np.clip(a, -1, 1) * 32767).astype("<i2")
    with wave.open(str(path), "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(data.tobytes())


def read_wav(path: str | Path, rate: int = RATE) -> np.ndarray:
    """Reads a WAV as mono float32 at `rate`."""
    import soundfile as sf
    a, r = sf.read(str(path), dtype="float32", always_2d=True)
    a = a.mean(axis=1)
    if r != rate:
        a = signal.resample_poly(a, rate, r).astype(np.float32)
    return a


def wav_duration(path: str | Path) -> float:
    with wave.open(str(path), "rb") as w:
        return w.getnframes() / w.getframerate()
