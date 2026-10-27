"""Metrics on audio.

The genre's production identity is measurable in broad strokes: dark and warm (energy falls off
steeply, little above 6-8 kHz), soft attacks (a smooth envelope, few sharp onsets), a wash of reverb
(no dead digital silence between notes), slow volume swells rather than per-note dynamics, a modest
stereo image. These do not say a mix is good; they say when it has left the genre.

    lufs                integrated loudness (ITU-R BS.1770)
    lra                 loudness range: 95th - 10th percentile of 3 s short-term loudness, LU
    peak_dbfs           sample peak
    clip_ratio          samples at or above -0.1 dBFS
    tilt_db_oct         slope of the long-term octave-band spectrum, 250 Hz - 8 kHz, dB per octave
    centroid_hz         median spectral centroid
    hf6_share / hf8_share   energy share above 6 kHz / 8 kHz
    low_share           energy share below 150 Hz
    presence_share      energy share 2-5 kHz, where harshness lives
    side_ratio          side / mid energy (stereo width)
    onset_rate          sharp onsets per second (spectral flux peaks)
    attack_sharpness    median flux peak height relative to the median flux: high = percussive
    envelope_roughness  mean |d RMS dB| between 50 ms frames: low = smooth swells
    swell_db            std of 10 s loudness: slow dynamics
    hole_ratio          50 ms frames below -60 dBFS: dead digital silence
    spike_db            99th percentile of 1 s loudness over its median: sudden loud moments
    flatness            median spectral flatness (noisiness)
"""

from __future__ import annotations

import numpy as np
import pyloudnorm
import soundfile as sf
from scipy import signal


def load(path) -> tuple[np.ndarray, int]:
    x, sr = sf.read(str(path), always_2d=True, dtype="float32")
    if x.shape[1] == 1:
        x = np.repeat(x, 2, axis=1)
    return x[:, :2], sr


def band_share(f: np.ndarray, p: np.ndarray, lo: float, hi: float) -> float:
    sel = (f >= lo) & (f < hi)
    return float(p[sel].sum() / max(1e-20, p[(f >= 20)].sum()))


def analyse(path) -> dict:
    x, sr = load(path)
    mono = x.mean(axis=1)
    out: dict = {"duration_s": len(mono) / sr}

    meter = pyloudnorm.Meter(sr)
    out["lufs"] = float(meter.integrated_loudness(x))
    st = []
    hop, win = sr, 3 * sr
    for i in range(0, max(1, len(x) - win), hop):
        seg = x[i:i + win]
        l = meter.integrated_loudness(seg) if len(seg) >= win else -np.inf
        if np.isfinite(l) and l > -70:
            st.append(l)
    if st:
        out["lra"] = float(np.percentile(st, 95) - np.percentile(st, 10))
    ten = []
    for i in range(0, max(1, len(x) - 10 * sr), 5 * sr):
        l = meter.integrated_loudness(x[i:i + 10 * sr])
        if np.isfinite(l) and l > -70:
            ten.append(l)
    if len(ten) >= 3:
        out["swell_db"] = float(np.std(ten))

    peak = float(np.max(np.abs(x)))
    out["peak_dbfs"] = 20 * np.log10(max(peak, 1e-9))
    out["clip_ratio"] = float(np.mean(np.abs(x) >= 10 ** (-0.1 / 20)))

    f, p = signal.welch(mono, sr, nperseg=8192)
    out["hf6_share"] = band_share(f, p, 6000, sr / 2)
    out["hf8_share"] = band_share(f, p, 8000, sr / 2)
    out["low_share"] = band_share(f, p, 20, 150)
    out["presence_share"] = band_share(f, p, 2000, 5000)
    # Octave-band levels 125 Hz .. 8 kHz, and their slope.
    centres = 125 * 2.0 ** np.arange(7)
    levels = []
    for c in centres:
        sel = (f >= c / np.sqrt(2)) & (f < c * np.sqrt(2))
        levels.append(10 * np.log10(max(1e-20, p[sel].sum())))
    out["tilt_db_oct"] = float(np.polyfit(np.arange(1, 7), levels[1:], 1)[0])  # 250 Hz - 8 kHz
    out["_octave_levels"] = [float(v) for v in levels]

    # Short-time features at 50 ms hops.
    n_fft, hop = 2048, int(sr * 0.05)
    fr, tt, Z = signal.stft(mono, sr, nperseg=n_fft, noverlap=n_fft - hop, boundary=None)
    mag = np.abs(Z) + 1e-12
    pw = mag ** 2
    cen = (fr[:, None] * pw).sum(0) / pw.sum(0)
    loud = 10 * np.log10(pw.sum(0) / n_fft + 1e-20)
    audible = loud > loud.max() - 50
    out["centroid_hz"] = float(np.median(cen[audible])) if audible.any() else 0.0
    flat = np.exp(np.mean(np.log(mag), 0)) / np.mean(mag, 0)
    out["flatness"] = float(np.median(flat[audible])) if audible.any() else 0.0

    frame = int(sr * 0.05)
    nfr = len(mono) // frame
    rms = np.sqrt(np.mean(mono[: nfr * frame].reshape(nfr, frame) ** 2, axis=1) + 1e-12)
    rdb = 20 * np.log10(rms)
    out["hole_ratio"] = float(np.mean(rdb < -60))
    sec = 20
    if nfr >= sec * 10:
        r1 = 10 * np.log10(np.mean((rms[: (nfr // sec) * sec] ** 2).reshape(-1, sec), axis=1) + 1e-12)
        out["spike_db"] = float(np.percentile(r1, 99) - np.median(r1))
    alive = rdb > -60
    d = np.abs(np.diff(rdb))
    out["envelope_roughness"] = float(np.mean(d[alive[1:] & alive[:-1]])) if alive.sum() > 2 else 0.0

    # Onsets: positive spectral flux on a log-magnitude spectrogram, peaks above an adaptive threshold.
    lm = np.log1p(mag[fr < 8000] * 100)
    flux = np.maximum(0, np.diff(lm, axis=1)).sum(0)
    if len(flux) > 10:
        med = signal.medfilt(flux, 21) + 1e-9
        peaks, _ = signal.find_peaks(flux, height=med * 1.8 + np.median(flux) * 0.5, distance=2)
        out["onset_rate"] = len(peaks) / out["duration_s"]
        out["attack_sharpness"] = float(np.median(flux[peaks] / med[peaks])) if len(peaks) else 0.0

    mid = (x[:, 0] + x[:, 1]) / 2
    side = (x[:, 0] - x[:, 1]) / 2
    out["side_ratio"] = float(np.sum(side ** 2) / max(1e-12, np.sum(mid ** 2)))
    return out


def chroma_frames(path, hop_s: float = 0.25) -> tuple[np.ndarray, float]:
    """12 x T chroma from audio (CQT-free: STFT bins folded onto pitch classes, 60 Hz - 2 kHz)."""
    x, sr = load(path)
    mono = x.mean(axis=1)
    n_fft = 8192
    hop = int(sr * hop_s)
    fr, _, Z = signal.stft(mono, sr, nperseg=n_fft, noverlap=n_fft - hop, boundary=None)
    mag = np.abs(Z)
    sel = (fr >= 60) & (fr <= 2000)
    pcs = (np.round(12 * np.log2(fr[sel] / 440.0)) + 9) % 12
    C = np.zeros((12, mag.shape[1]))
    for k in range(12):
        C[k] = mag[sel][pcs == k].sum(0)
    C /= np.linalg.norm(C, axis=0, keepdims=True) + 1e-9
    return C, hop_s


def audio_structure(path) -> dict:
    """Repetition heard in the audio itself: does the structure survive the reverb and the mix?

    Chroma in 2 s blocks; the share of blocks that closely match (cosine >= 0.9) a block 8-120 s
    earlier. A random walk over a held drone matches often too, so read it beside motif_recurrence.
    """
    C, hop = chroma_frames(path)
    per = int(2.0 / hop)
    nb = C.shape[1] // per
    if nb < 10:
        return {}
    B = C[:, : nb * per].reshape(12, nb, per).mean(2)
    B /= np.linalg.norm(B, axis=0, keepdims=True) + 1e-9
    S = B.T @ B
    hits = 0
    for i in range(nb):
        lo, hi = max(0, i - 60), i - 4
        if hi > lo and S[i, lo:hi].max() >= 0.9:
            hits += 1
    return {"audio_repeat": hits / nb}
