"""The blind scores and the mechanical metrics, per version and kind, with paired comparisons.

    python analyze.py --scored scored.jsonl --metrics metrics.json [--json results.json]

Each criterion's mean per version; then, against `first`, the paired difference over cases (each
case's mean over its seeds, the same cases on both sides) with a 95% bootstrap interval. A difference
whose interval holds 0 is not a difference.
"""
import argparse, collections, json, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import person_lib as L      # noqa: E402

CRIT = ["F", "I", "K", "P", "C", "R", "Q"]
NAMES = dict(F="no hallucination", I="info kept", K="kind's task", P="persona", C="context", R="no repeat/echo",
             Q="writing")


def mean(xs):
    xs = [x for x in xs if x is not None]
    return sum(xs) / len(xs) if xs else None


def boot(diffs, n=4000, seed=1):
    rng = random.Random(seed)
    ms = sorted(sum(rng.choice(diffs) for _ in diffs) / len(diffs) for _ in range(n))
    return ms[int(0.025 * n)], ms[int(0.975 * n)]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--scored", required=True)
    ap.add_argument("--metrics", required=True)
    ap.add_argument("--json")
    ap.add_argument("--versions", nargs="*", default=list(L.VERSIONS[:3]))
    a = ap.parse_args()
    sc = [json.loads(l) for l in open(a.scored)]
    met = json.load(open(a.metrics))
    kind_of = {s["case"]: s["kind"] for s in met["samples"]}
    set_of = {s["case"]: s["set"] for s in met["samples"]}
    for s in sc:
        s["kind"], s["set"] = kind_of[s["case"]], set_of[s["case"]]
        s["F0"] = int(s["F"] == 0)
    out = {}
    for group in ["all", "dev", "heldout"] + list(L.KINDS):
        rows = [s for s in sc if group == "all" or s["kind"] == group or s["set"] == group]
        if not rows:
            continue
        print(f"\n== {group}")
        print(f"{'version':10} {'n':>4} " + " ".join(f"{c:>6}" for c in CRIT) + f" {'F=0':>6}")
        g = {}
        for v in a.versions:
            rs = [s for s in rows if s["version"] == v]
            ms = {c: mean([s[c] for s in rs]) for c in CRIT + ["F0"]}
            g[v] = dict(n=len(rs), **ms)
            print(f"{v:10} {len(rs):4} " + " ".join(f"{ms[c]:6.2f}" if ms[c] is not None else f"{'-':>6}" for c in CRIT)
                  + f" {100 * ms['F0']:5.0f}%")
        # paired, by case
        for v in a.versions[1:]:
            line = []
            for c in CRIT + ["F0"]:
                per = collections.defaultdict(dict)
                for s in rows:
                    if s[c] is not None and s["version"] in ("first", v):
                        per[s["case"]].setdefault(s["version"], []).append(s[c])
                diffs = [mean(d[v]) - mean(d["first"]) for d in per.values() if "first" in d and v in d]
                if len(diffs) < 3:
                    line.append(f"{c}:-")
                    continue
                d = mean(diffs)
                lo, hi = boot(diffs)
                star = "*" if lo > 0 or hi < 0 else " "
                g.setdefault("diff_" + v, {})[c] = dict(d=d, lo=lo, hi=hi, n=len(diffs))
                line.append(f"{c}:{d:+.2f}[{lo:+.2f},{hi:+.2f}]{star}")
            print(f"  {v} - first: " + "  ".join(line))
        out[group] = g
    if a.json:
        json.dump(out, open(a.json, "w"), indent=1)


if __name__ == "__main__":
    main()
