"""Metrics on notes.

Each is a proxy for one thing the genre is known for (see targets.json for the bands and why):

    mode_fit            the pitches sit in one diatonic/modal collection at a time       modal, not chromatic
    pc_entropy          how evenly the twelve pitch classes are used (bits, per window)   tonal centre present
    mel_step_ratio      melodic intervals of one or two semitones                         stepwise, chant-like
    mel_leap_ratio      melodic intervals of a fourth or more                             few leaps
    mel_range_phrase    the span of a phrase, semitones                                   narrow range
    mel_density         melody notes per second while the melody plays                    slow
    motif_recurrence    4-note interval+rhythm figures already heard in the last 90 s     motifs, not a walk
    bar_repetition      melody bars repeating an earlier bar exactly or transposed        statement and repeat
    long_novelty        figures shared by windows minutes apart (1 - Jaccard)             still endless
    grid_alignment      onsets on a sixteenth grid                                        a pulse, not drift
    phrase_len_cv       how much phrase lengths vary                                      2+2 / 4+4 phrasing
    harmonic_rhythm_s   how long one harmony lasts                                        slow harmonic rhythm
    dissonance_time     time with a sounding minor second or tritone                      consonant, open
    active_parts        parts playing at once                                             layering
    layer_changes_min   how often the set of playing parts changes                        orchestration moves
    silence_ratio       time with nothing sounding at all                                 no dead holes
"""

from __future__ import annotations

import math
import zlib
from collections import Counter, deque

import numpy as np

from .score import Note, Score

PITCHED_EXCLUDE = {"perc", "sfx"}

DIATONIC = [0, 2, 4, 5, 7, 9, 11]
HARMONIC_MINOR = [0, 2, 3, 5, 7, 8, 11]
PENTATONIC = [0, 3, 5, 7, 10]
WHOLE_TONE = [0, 2, 4, 6, 8, 10]
COLLECTIONS = ([("diatonic", r, {(x + r) % 12 for x in DIATONIC}) for r in range(12)]
               + [("harmonic", r, {(x + r) % 12 for x in HARMONIC_MINOR}) for r in range(12)]
               + [("wholetone", r, {(x + r) % 12 for x in WHOLE_TONE}) for r in range(2)])


def _pitched(score: Score) -> list[Note]:
    return [n for n in score.notes if n.part not in PITCHED_EXCLUDE]


def _windows(score: Score, size: float, hop: float | None = None):
    hop = hop or size
    t = 0.0
    while t < max(score.seconds - size * 0.5, size * 0.5):
        yield t, t + size
        t += hop


def _pc_hist(notes: list[Note], a: float, b: float) -> np.ndarray:
    h = np.zeros(12)
    for n in notes:
        ov = min(n.t + n.d, b) - max(n.t, a)
        if ov > 0:
            h[n.p % 12] += ov * (0.3 + n.v)
    return h


def mode_fit(score: Score, window: float = 20.0) -> tuple[float, float]:
    """(fit, entropy): share of pitch weight inside the best-fitting collection, and pitch-class entropy."""
    notes = _pitched(score)
    fits, ents = [], []
    for a, b in _windows(score, window):
        h = _pc_hist(notes, a, b)
        tot = h.sum()
        if tot <= 0:
            continue
        fits.append(max(sum(h[i] for i in s) for _, _, s in COLLECTIONS) / tot)
        p = h[h > 0] / tot
        ents.append(float(-(p * np.log2(p)).sum()))
    return (float(np.mean(fits)) if fits else float("nan"), float(np.mean(ents)) if ents else float("nan"))


def _phrases(mel: list[Note]) -> list[list[Note]]:
    """Melody split at rests: a gap longer than both 0.6 s and twice the typical inter-onset interval."""
    if not mel:
        return []
    iois = [b.t - a.t for a, b in zip(mel, mel[1:])]
    typical = float(np.median(iois)) if iois else 1.0
    out, cur = [], [mel[0]]
    for a, b in zip(mel, mel[1:]):
        gap = b.t - (a.t + a.d)
        if gap > max(0.6, typical * 1.0) or (b.t - a.t) > max(1.5, typical * 3):
            out.append(cur)
            cur = []
        cur.append(b)
    out.append(cur)
    return out


def melodic(score: Score) -> dict:
    mel = score.melody()
    phrases = _phrases(mel)
    ints = [b.p - a.p for ph in phrases for a, b in zip(ph, ph[1:])]
    out: dict = {}
    if ints:
        a = np.abs(np.array(ints))
        out["mel_step_ratio"] = float(np.mean((a >= 1) & (a <= 2)))
        out["mel_repeat_ratio"] = float(np.mean(a == 0))
        out["mel_leap_ratio"] = float(np.mean(a >= 5))
        out["mel_large_leap_ratio"] = float(np.mean(a > 7))
        out["mel_down_ratio"] = float(np.mean(np.array(ints) < 0))
    ranges = [max(n.p for n in ph) - min(n.p for n in ph) for ph in phrases if len(ph) >= 3]
    if ranges:
        out["mel_range_phrase"] = float(np.median(ranges))
    lens = [ph[-1].t + ph[-1].d - ph[0].t for ph in phrases if len(ph) >= 3]
    if len(lens) >= 3:
        out["phrase_len_s"] = float(np.median(lens))
        out["phrase_len_cv"] = float(np.std(lens) / max(1e-6, np.mean(lens)))
    if mel:
        playing = sum(ph[-1].t + ph[-1].d - ph[0].t for ph in phrases)
        out["mel_density"] = len(mel) / max(1.0, playing)
        out["mel_presence"] = playing / max(1.0, score.seconds)
    return out


def _tokens(mel: list[Note]) -> list[tuple[int, int]]:
    """(interval, log2 IOI ratio quantised) per note: a figure survives transposition and tempo."""
    toks = []
    for a, b, c in zip(mel, mel[1:], mel[2:]):
        r1 = max(0.05, b.t - a.t)
        r2 = max(0.05, c.t - b.t)
        toks.append((c.p - b.p, int(round(2 * math.log2(r2 / r1)))))
    return toks


def repetition(score: Score, n: int = 4, memory: float = 90.0) -> dict:
    mel = score.melody()
    toks = _tokens(mel)
    times = [m.t for m in mel[2:]]
    if len(toks) < n + 4:
        return {}
    seen: deque[tuple[float, tuple]] = deque()
    counts: Counter = Counter()
    hits = total = 0
    for i in range(len(toks) - n + 1):
        t = times[i]
        while seen and seen[0][0] < t - memory:
            _, g = seen.popleft()
            counts[g] -= 1
        g = tuple(toks[i:i + n])
        total += 1
        if counts[g] > 0:
            hits += 1
        seen.append((t, g))
        counts[g] += 1
    out = {"motif_recurrence": hits / total}

    # Compressibility of the melody per minute: structure compresses, a random walk does not.
    ratios = []
    for a, b in _windows(score, 60.0):
        seg = [f"{iv},{r}" for (iv, r), t in zip(toks, times) if a <= t < b]
        if len(seg) >= 16:
            raw = ";".join(seg).encode()
            ratios.append(len(zlib.compress(raw, 9)) / len(raw))
    if ratios:
        out["lz_ratio"] = float(np.mean(ratios))

    # Variation across minutes: figures in windows at least four minutes apart.
    if score.seconds >= 360:
        win = 60.0
        sets = []
        for a, b in _windows(score, win):
            sets.append((a, {tuple(toks[i:i + n]) for i in range(len(toks) - n + 1) if a <= times[i] < b}))
        dists = []
        for i, (ta, sa) in enumerate(sets):
            for tb, sb in sets[i + 1:]:
                if tb - ta >= 240 and sa and sb:
                    dists.append(1 - len(sa & sb) / len(sa | sb))
        if dists:
            out["long_novelty"] = float(np.mean(dists))
    return out


def bar_repetition(score: Score) -> dict:
    """Melody bars equal to an earlier bar (same rhythm, same intervals) within the last 32 bars."""
    bars = _bars(score)
    mel = score.melody()
    if not bars or len(mel) < 8:
        return {}
    sigs = []
    j = 0
    for a, b in bars:
        while j < len(mel) and mel[j].t < a - 0.05:
            j += 1
        k = j
        bar = []
        while k < len(mel) and mel[k].t < b - 0.05:
            bar.append(mel[k])
            k += 1
        if len(bar) < 2:
            sigs.append(None)
            continue
        dur = b - a
        rhythm = tuple(int(round((m.t - a) / dur * 16)) for m in bar)
        ivs = tuple(y.p - x.p for x, y in zip(bar, bar[1:]))
        sigs.append((rhythm, ivs))
    hits = total = 0
    for i, s in enumerate(sigs):
        if s is None:
            continue
        total += 1
        if any(sigs[k] == s for k in range(max(0, i - 32), i - 0)):
            hits += 1
    return {"bar_repetition": hits / total} if total else {}


def _bars(score: Score) -> list[tuple[float, float]]:
    """Bar spans from the engine's pieces (bpm, meter); for MIDI without them, 2-second spans."""
    out = []
    if score.pieces:
        for i, pc in enumerate(score.pieces):
            end = score.pieces[i + 1]["t"] if i + 1 < len(score.pieces) else score.seconds
            bar = 60.0 / pc["bpm"] * pc.get("meter", 4)
            t = pc["t"]
            while t + bar <= end + 1e-6:
                out.append((t, t + bar))
                t += bar
        return out
    t = 0.0
    while t + 2 <= score.seconds:
        out.append((t, t + 2))
        t += 2
    return out


def grid(score: Score) -> dict:
    """Onsets within 25 ms of a sixteenth-note grid. Uses the engine's tempo; otherwise the best of 40-140 bpm."""
    on = np.array(sorted(n.t for n in _pitched(score)))
    if len(on) < 20:
        return {}

    def align(bpm: float, t0: float = 0.0) -> float:
        g = 60.0 / bpm / 4
        ph = ((on - t0) / g) % 1.0
        return float(np.mean((ph < 25e-3 / g) | (ph > 1 - 25e-3 / g)))

    if score.pieces:
        vals = []
        for i, pc in enumerate(score.pieces):
            end = score.pieces[i + 1]["t"] if i + 1 < len(score.pieces) else score.seconds
            sel = on[(on >= pc["t"]) & (on < end)]
            if len(sel):
                g = 60.0 / pc["bpm"] / 4
                ph = ((sel - pc["t"]) / g) % 1.0
                vals.append((len(sel), float(np.mean((ph < 25e-3 / g) | (ph > 1 - 25e-3 / g)))))
        return {"grid_alignment": sum(n * v for n, v in vals) / max(1, sum(n for n, _ in vals))}
    best = max(align(b) for b in np.arange(40, 141, 1.0))
    return {"grid_alignment": best}


def harmony(score: Score) -> dict:
    """Harmonic rhythm and dissonance. Chords from the engine when it says; else chroma change per 0.5 s."""
    out: dict = {}
    if score.chords:
        durs = [c["d"] for c in score.chords if c["d"] > 0]
        if durs:
            out["harmonic_rhythm_s"] = float(np.median(durs))
    else:
        notes = _pitched(score)
        step = 0.5
        prev, seg_start, segs = None, 0.0, []
        t = 0.0
        while t < score.seconds:
            h = _pc_hist(notes, t, t + step)
            if h.sum() > 0:
                h = h / np.linalg.norm(h)
                if prev is not None and float(h @ prev) < 0.75:
                    segs.append(t - seg_start)
                    seg_start = t
                prev = h
            t += step
        if segs:
            out["harmonic_rhythm_s"] = float(np.median(segs))

    # Dissonance: sample every 50 ms, the set of sounding pitched notes.
    notes = sorted(_pitched(score), key=lambda n: n.t)
    times = np.arange(0, score.seconds, 0.05)
    starts = np.array([n.t for n in notes])
    ends = np.array([n.t + n.d for n in notes])
    pcs = np.array([n.p for n in notes])
    diss = sounding = 0
    poly = []
    silent = 0
    for t in times:
        idx = np.nonzero((starts <= t) & (ends > t))[0]
        poly.append(len(idx))
        if len(idx) == 0:
            silent += 1
            continue
        sounding += 1
        ps = set(int(p) for p in pcs[idx])
        ics = {min((a - b) % 12, (b - a) % 12) for a in ps for b in ps if a != b}
        if 1 in ics or 6 in ics:
            diss += 1
    out["dissonance_time"] = diss / max(1, sounding)
    out["polyphony_mean"] = float(np.mean(poly)) if poly else 0.0
    out["silence_ratio"] = silent / max(1, len(times))
    return out


def layering(score: Score, window: float = 5.0) -> dict:
    parts_by_win = []
    for a, b in _windows(score, window):
        active = frozenset(n.part for n in score.notes
                           if n.part not in PITCHED_EXCLUDE and n.t < b and n.t + n.d > a)
        parts_by_win.append(active)
    if not parts_by_win:
        return {}
    changes = sum(1 for x, y in zip(parts_by_win, parts_by_win[1:]) if x != y)
    return {"active_parts": float(np.mean([len(p) for p in parts_by_win])),
            "layer_changes_min": changes / max(1e-6, score.seconds / 60.0),
            "distinct_layer_sets": len(set(parts_by_win))}


def analyse(score: Score) -> dict:
    fit, ent = mode_fit(score)
    out = {"mode_fit": fit, "pc_entropy": ent}
    for f in (melodic, repetition, bar_repetition, grid, harmony, layering):
        out.update(f(score))
    out["notes_per_s"] = len(score.notes) / max(1.0, score.seconds)
    return out
