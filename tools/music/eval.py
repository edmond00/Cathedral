#!/usr/bin/env python3
"""The music evaluation harness. See tools/music/README.md.

    eval.py render  --mood Neutral --seconds 300 --seed 1 [--journey j.json] -o runs/x.wav
    eval.py analyse runs/x.wav [--score runs/x.score.json | --midi x.mid] [--sheet]
    eval.py matrix  --label round3 [--moods ...] [--seeds 1 2] [--seconds 300]
    eval.py report  runs/round3 [runs/baseline ...] -o report.html
    eval.py compare runs/A runs/B
    eval.py calibrate reference/ -o targets.reference.json
"""

from __future__ import annotations

import argparse
import json
import math
import subprocess
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from musickit import ROOT, TOOLS, acoustic, score as scoremod, symbolic, targets  # noqa: E402

DLL = ROOT / "bin" / "Debug" / "net8.0" / "Cathedral.dll"
MOODS = ["Neutral", "Tavern", "Lament", "DarkDungeon", "Battle", "WorldView", "Creation", "Childhood"]


def render(mood: str, seconds: float, seed: int, out: Path, journey: Path | None = None,
           extra: list[str] | None = None) -> float:
    out.parent.mkdir(parents=True, exist_ok=True)
    cmd = ["dotnet", str(DLL), "--render-music", str(out), "--seconds", str(seconds),
           "--mood", mood, "--seed", str(seed)]
    if journey:
        cmd += ["--journey", str(journey)]
    cmd += extra or []
    t0 = time.time()
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode != 0:
        sys.stderr.write(r.stdout[-3000:] + r.stderr[-3000:])
        raise SystemExit(f"render failed: {' '.join(cmd)}")
    return time.time() - t0


def analyse(wav: Path, score_path: Path | None, sheet: bool = True) -> dict:
    res: dict = {"wav": str(wav)}
    res["acoustic"] = acoustic.analyse(wav)
    res["acoustic"].update(acoustic.audio_structure(wav))
    sc = None
    if score_path and score_path.exists():
        sc = scoremod.load(score_path)
        res["symbolic"] = symbolic.analyse(sc)
        res["meta"] = sc.meta
    res["scores"] = targets.evaluate(res)
    if sheet:
        from musickit import sheets
        png = wav.with_suffix(".png")
        sheets.draw(wav, sc, png, res)
        res["sheet"] = str(png)
    out = wav.with_suffix(".metrics.json")
    out.write_text(json.dumps(res, indent=1, default=float))
    return res


def _fmt(v):
    if isinstance(v, float):
        return f"{v:.3g}" if math.isfinite(v) else "nan"
    return str(v)


def print_result(res: dict):
    s = res["scores"]
    print(f"== {res['wav']}   score {s['total']:.2f}   (in band {s['in_band']}/{s['n']})")
    for name, row in s["rows"].items():
        mark = "ok " if row["ok"] else "-- "
        print(f"   {mark}{name:22s} {_fmt(row['value']):>9s}   band {row['lo']}..{row['hi']}")


def cmd_render(a):
    out = Path(a.out)
    dt = render(a.mood, a.seconds, a.seed, out, Path(a.journey) if a.journey else None)
    print(f"rendered {a.seconds}s in {dt:.1f}s ({a.seconds / dt:.1f}x real time)")
    if not a.no_analyse:
        print_result(analyse(out, out.with_suffix(".score.json")))


def cmd_analyse(a):
    wav = Path(a.wav)
    sp = Path(a.score) if a.score else (Path(a.midi) if a.midi else wav.with_suffix(".score.json"))
    print_result(analyse(wav, sp, sheet=not a.no_sheet))


def cmd_matrix(a):
    base = TOOLS / "runs" / a.label
    base.mkdir(parents=True, exist_ok=True)
    jobs = [(m, s) for m in a.moods for s in a.seeds]
    procs = []
    for m, s in jobs:
        out = base / f"{m}_s{s}.wav"
        cmd = ["dotnet", str(DLL), "--render-music", str(out), "--seconds", str(a.seconds), "--mood", m, "--seed", str(s)]
        procs.append((out, subprocess.Popen(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)))
        while sum(p.poll() is None for _, p in procs) >= a.jobs:
            time.sleep(0.2)
    for out, p in procs:
        p.wait()
        if p.returncode != 0:
            print(p.stderr.read().decode()[-2000:])
            raise SystemExit(f"render failed: {out}")
    results = []
    for out, _ in procs:
        res = analyse(out, out.with_suffix(".score.json"), sheet=not a.no_sheet)
        results.append(res)
        print_result(res)
    summary = targets.summarise(results)
    (base / "summary.json").write_text(json.dumps(summary, indent=1, default=float))
    print(f"\n{a.label}: mean score {summary['mean_total']:.3f} over {len(results)} renders")
    for k, v in summary["failing"].items():
        print(f"   out of band in {v['count']}/{len(results)}: {k}  (median {_fmt(v['median'])}, band {v['lo']}..{v['hi']})")


def cmd_report(a):
    from musickit import report
    report.build([Path(d) for d in a.dirs], Path(a.out))
    print(f"report written to {a.out}")


def cmd_compare(a):
    A = json.loads((Path(a.a) / "summary.json").read_text())
    B = json.loads((Path(a.b) / "summary.json").read_text())
    print(f"{'metric':24s} {Path(a.a).name:>12s} {Path(a.b).name:>12s}   band")
    for k in A["medians"]:
        if k in B["medians"]:
            band = targets.band(k)
            print(f"{k:24s} {_fmt(A['medians'][k]):>12s} {_fmt(B['medians'][k]):>12s}   {band[0] if band else ''}..{band[1] if band else ''}")
    print(f"{'TOTAL':24s} {A['mean_total']:12.3f} {B['mean_total']:12.3f}")


def cmd_calibrate(a):
    rows = [analyse(w, None, sheet=False) for w in sorted(Path(a.dir).glob("*.wav"))]
    out = targets.calibrate(rows)
    Path(a.out).write_text(json.dumps(out, indent=1))
    print(f"{len(rows)} reference tracks -> {a.out}")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    r = sub.add_parser("render")
    r.add_argument("--mood", default="Neutral")
    r.add_argument("--seconds", type=float, default=300)
    r.add_argument("--seed", type=int, default=1)
    r.add_argument("--journey")
    r.add_argument("--no-analyse", action="store_true")
    r.add_argument("-o", "--out", required=True)
    r.set_defaults(f=cmd_render)
    an = sub.add_parser("analyse")
    an.add_argument("wav")
    an.add_argument("--score")
    an.add_argument("--midi")
    an.add_argument("--no-sheet", action="store_true")
    an.set_defaults(f=cmd_analyse)
    m = sub.add_parser("matrix")
    m.add_argument("--label", required=True)
    m.add_argument("--moods", nargs="+", default=MOODS)
    m.add_argument("--seeds", nargs="+", type=int, default=[1, 2])
    m.add_argument("--seconds", type=float, default=300)
    m.add_argument("--jobs", type=int, default=6)
    m.add_argument("--no-sheet", action="store_true")
    m.set_defaults(f=cmd_matrix)
    rp = sub.add_parser("report")
    rp.add_argument("dirs", nargs="+")
    rp.add_argument("-o", "--out", default=str(TOOLS / "runs" / "report.html"))
    rp.set_defaults(f=cmd_report)
    c = sub.add_parser("compare")
    c.add_argument("a")
    c.add_argument("b")
    c.set_defaults(f=cmd_compare)
    cal = sub.add_parser("calibrate")
    cal.add_argument("dir")
    cal.add_argument("-o", "--out", default=str(TOOLS / "targets.reference.json"))
    cal.set_defaults(f=cmd_calibrate)
    a = ap.parse_args()
    a.f(a)


if __name__ == "__main__":
    main()
