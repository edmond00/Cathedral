"""One PNG per render, for looking at what cannot be heard.

Five panels: the spectrogram of the first two minutes (timbre, attacks, the reverb's smear, the
highs that should not be there), the piano roll of the same two minutes by part with the section
boundaries (phrasing, repetition, layering), the audio chroma self-similarity of the whole render
(form: repeated sections show as off-diagonal stripes, a meander as a blur), loudness and the
number of playing parts over the whole render (swells, entries and exits), and the long-term
octave spectrum against a -3 and a -6 dB/oct line.
"""

from __future__ import annotations

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402
from scipy import signal  # noqa: E402

from . import acoustic  # noqa: E402

PART_COLOURS = {"melody": "#e8c547", "counter": "#c77dff", "drone": "#6c757d", "pad": "#4cc9f0",
                "ostinato": "#80ed99", "bass": "#adb5bd", "perc": "#ff6b6b", "bell": "#ffd6a5",
                "choir": "#90e0ef", "sfx": "#ff006e", "fight": "#ff8fa3", "harmony": "#4cc9f0"}


def draw(wav, sc, png, res, span: float = 120.0):
    x, sr = acoustic.load(wav)
    mono = x.mean(axis=1)
    fig = plt.figure(figsize=(16, 13), facecolor="#111")
    gs = fig.add_gridspec(4, 3, height_ratios=[1.1, 1.3, 0.8, 0.9], hspace=0.35, wspace=0.25)
    style = dict(color="#ddd", fontsize=9)

    # 1. Spectrogram, first `span` seconds.
    ax = fig.add_subplot(gs[0, :])
    seg = mono[: int(span * sr)]
    f, t, Z = signal.stft(seg, sr, nperseg=2048, noverlap=2048 - 512)
    S = 20 * np.log10(np.abs(Z) + 1e-7)
    sel = (f > 30) & (f < 14000)
    ax.pcolormesh(t, f[sel], S[sel], shading="auto", cmap="magma", vmin=S.max() - 80, vmax=S.max())
    ax.set_yscale("log")
    ax.set_ylim(40, 14000)
    ax.axhline(6000, color="#4cc9f0", lw=0.6, ls="--")
    ax.set_title(f"spectrogram 0-{span:.0f}s   (dashed: 6 kHz)", **style)
    _dark(ax)

    # 2. Piano roll.
    ax = fig.add_subplot(gs[1, :])
    if sc is not None:
        for n in sc.notes:
            if n.t > span:
                continue
            c = PART_COLOURS.get(n.part, "#888")
            ax.add_patch(plt.Rectangle((n.t, n.p - 0.4), n.d, 0.8, color=c, alpha=0.35 + 0.6 * min(1, n.v)))
        for s in sc.sections:
            if s["t"] <= span:
                ax.axvline(s["t"], color="#fff", lw=0.6, alpha=0.5)
                ax.text(s["t"] + 0.3, 100, s.get("label", ""), color="#fff", fontsize=8)
        for c in sc.chords:
            if c["t"] <= span:
                ax.axvline(c["t"], color="#555", lw=0.4, ls=":")
        ps = [n.p for n in sc.notes] or [40, 90]
        ax.set_ylim(min(ps) - 2, max(max(ps), 98) + 4)
        ax.set_xlim(0, span)
        handles = [plt.Rectangle((0, 0), 1, 1, color=PART_COLOURS.get(p, "#888")) for p in sc.parts]
        ax.legend(handles, sc.parts, loc="upper right", fontsize=7, ncol=len(sc.parts), facecolor="#222", labelcolor="#ddd")
        meta = sc.pieces[0] if sc.pieces else {}
        ax.set_title(f"piano roll 0-{span:.0f}s   {meta.get('mode', '')} tonic {meta.get('tonic', '')} "
                     f"{meta.get('bpm', '')} bpm {meta.get('meter', '')}/4 form {meta.get('form', '')}", **style)
    _dark(ax)

    # 3. Self-similarity from audio chroma, 2 s blocks, whole render.
    ax = fig.add_subplot(gs[2:, 0])
    C, hop = acoustic.chroma_frames(wav)
    per = int(2.0 / hop)
    nb = C.shape[1] // per
    if nb > 2:
        B = C[:, : nb * per].reshape(12, nb, per).mean(2)
        B /= np.linalg.norm(B, axis=0, keepdims=True) + 1e-9
        ax.imshow(B.T @ B, cmap="viridis", vmin=0.5, vmax=1, origin="lower", extent=[0, nb * 2, 0, nb * 2])
    ax.set_title("chroma self-similarity (s)", **style)
    _dark(ax)

    # 4. Loudness and active parts over the whole render.
    ax = fig.add_subplot(gs[2, 1:])
    frame = int(sr * 0.5)
    nfr = len(mono) // frame
    rms = 20 * np.log10(np.sqrt(np.mean(mono[: nfr * frame].reshape(nfr, frame) ** 2, 1)) + 1e-9)
    tt = np.arange(nfr) * 0.5
    ax.plot(tt, rms, color="#e8c547", lw=0.8)
    ax.set_ylim(-70, 0)
    ax.set_ylabel("RMS dB", **style)
    if sc is not None:
        ax2 = ax.twinx()
        counts = []
        for t0 in tt:
            counts.append(len({n.part for n in sc.notes if n.part not in ("sfx", "perc") and n.t < t0 + 0.5 and n.t + n.d > t0}))
        ax2.step(tt, counts, color="#4cc9f0", lw=0.8, where="post")
        ax2.set_ylim(0, 8)
        ax2.tick_params(colors="#aaa", labelsize=7)
        for s in sc.sections:
            ax.axvline(s["t"], color="#fff", lw=0.4, alpha=0.3)
    ax.set_title("loudness (yellow) and playing parts (blue)", **style)
    _dark(ax)

    # 5. Long-term octave spectrum.
    ax = fig.add_subplot(gs[3, 1])
    lv = np.array(res["acoustic"].get("_octave_levels", []))
    if len(lv):
        xs = np.arange(len(lv))
        ax.plot(xs, lv - lv[0], "o-", color="#e8c547")
        ax.plot(xs, -3 * xs, "--", color="#666", lw=0.8)
        ax.plot(xs, -6 * xs, "--", color="#999", lw=0.8)
        ax.set_xticks(xs, [f"{int(125 * 2 ** i)}" for i in xs])
    ax.set_title(f"octave levels, tilt {res['acoustic'].get('tilt_db_oct', 0):.1f} dB/oct", **style)
    _dark(ax)

    # 6. Score table.
    ax = fig.add_subplot(gs[3, 2])
    ax.axis("off")
    rows = res["scores"]["rows"]
    bad = [f"{k}: {v['value']:.3g} ({v['lo']}..{v['hi']})" for k, v in rows.items() if not v["ok"]]
    txt = f"score {res['scores']['total']:.2f}  in band {res['scores']['in_band']}/{res['scores']['n']}\n" + "\n".join(bad[:14])
    ax.text(0, 1, txt, va="top", family="monospace", **style)

    fig.suptitle(str(wav), color="#ddd", fontsize=10)
    fig.savefig(png, dpi=72, facecolor=fig.get_facecolor())
    plt.close(fig)


def _dark(ax):
    ax.set_facecolor("#181818")
    ax.tick_params(colors="#aaa", labelsize=7)
    for s in ax.spines.values():
        s.set_color("#444")
