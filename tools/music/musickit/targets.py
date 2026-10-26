"""Bands, and a render's score against them.

A metric in band scores 1. Out of band it decays with the distance, measured in band widths, so a
near miss costs little and a wild one costs its full weight. The total is the weighted mean. It is
a regression signal: did a change move the music towards the genre or away from it. It is not a
verdict on whether the music is good; nothing here can say that.
"""

from __future__ import annotations

import json
import math
from pathlib import Path

import numpy as np

from . import TOOLS

_T = json.loads((TOOLS / "targets.json").read_text())
_REF = TOOLS / "targets.reference.json"
if _REF.exists():  # bands measured on reference recordings replace the hand-set acoustic ones
    for k, v in json.loads(_REF.read_text()).get("metrics", {}).items():
        _T["metrics"].setdefault(k, {}).update(v)


def band(name: str, mood: str | None = None):
    m = _T["metrics"].get(name)
    if not m:
        return None
    lo, hi = m["lo"], m["hi"]
    if mood and mood in _T["moods"] and name in _T["moods"][mood]:
        o = _T["moods"][mood][name]
        lo, hi = o["lo"], o["hi"]
    return lo, hi


def _row_score(v: float, lo: float, hi: float) -> float:
    if v is None or not math.isfinite(v):
        return 0.0
    if lo <= v <= hi:
        return 1.0
    width = max(hi - lo, 1e-6, abs(hi) * 0.1)
    dist = (lo - v) if v < lo else (v - hi)
    return math.exp(-3 * dist / width)


def evaluate(res: dict) -> dict:
    mood = (res.get("meta") or {}).get("mood")
    values = {**res.get("acoustic", {}), **res.get("symbolic", {})}
    rows, num, den, inb = {}, 0.0, 0.0, 0
    for name, m in _T["metrics"].items():
        if name not in values:
            continue
        lo, hi = band(name, mood)
        v = values[name]
        s = _row_score(v, lo, hi)
        rows[name] = {"value": v, "lo": lo, "hi": hi, "ok": s == 1.0, "score": s, "w": m["w"]}
        num += s * m["w"]
        den += m["w"]
        inb += s == 1.0
    return {"total": num / den if den else 0.0, "in_band": inb, "n": len(rows), "rows": rows}


def summarise(results: list[dict]) -> dict:
    names = sorted({k for r in results for k in r["scores"]["rows"]})
    medians, failing = {}, {}
    for k in names:
        vals = [r["scores"]["rows"][k]["value"] for r in results if k in r["scores"]["rows"]]
        vals = [v for v in vals if v is not None and math.isfinite(v)]
        if vals:
            medians[k] = float(np.median(vals))
        bad = [r for r in results if k in r["scores"]["rows"] and not r["scores"]["rows"][k]["ok"]]
        if bad:
            lo, hi = band(k)
            failing[k] = {"count": len(bad), "median": medians.get(k, float("nan")), "lo": lo, "hi": hi}
    return {"mean_total": float(np.mean([r["scores"]["total"] for r in results])),
            "medians": medians, "failing": failing,
            "renders": [{"wav": r["wav"], "total": r["scores"]["total"]} for r in results]}


def calibrate(rows: list[dict], margin: float = 0.15) -> dict:
    """Acoustic bands from reference recordings: their 10th-90th percentile, widened by a margin."""
    out = {}
    keys = sorted({k for r in rows for k in r["acoustic"] if not k.startswith("_")})
    for k in keys:
        if k not in _T["metrics"]:
            continue
        vals = np.array([r["acoustic"][k] for r in rows if k in r["acoustic"]], dtype=float)
        vals = vals[np.isfinite(vals)]
        if len(vals) < 3:
            continue
        lo, hi = np.percentile(vals, 10), np.percentile(vals, 90)
        pad = (hi - lo) * margin + 1e-9
        out[k] = {"lo": float(lo - pad), "hi": float(hi + pad)}
    return {"_doc": f"Calibrated from {len(rows)} reference recordings.", "metrics": out}
