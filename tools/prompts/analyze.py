"""Unblinded scores, per version: the five criteria, overall and per kind.

    python tools/prompts/analyze.py --scores scores/r1.jsonl --samples samples/iter.jsonl [--cases cases.jsonl]
"""
import argparse, collections, json, os, statistics as st


def load(p):
    return [json.loads(l) for l in open(p, encoding="utf-8") if l.strip()]


def table(rows, samples, cases, by_kind=True):
    samp = {(s["case"], s["version"], s["seed"]): s for s in samples}
    kind = {c["id"]: c["record"]["kind"] for c in cases}
    groups = collections.defaultdict(list)
    for r in rows:
        s = samp.get((r["case"], r["version"], r["seed"]))
        if not s:
            continue
        r = dict(r, tokens=s["user_tokens"], ms=s.get("prompt_ms") or 0, wall=s["wall"],
                 out_tokens=s.get("predicted_n") or 0)
        groups[(r["version"], "ALL")].append(r)
        if by_kind:
            groups[(r["version"], kind[r["case"]])].append(r)
    out = []
    for (v, k), rs in sorted(groups.items(), key=lambda x: (x[0][1], x[0][0])):
        ctx = [r["C"] for r in rs if r["C"] is not None]
        out.append(dict(version=v, kind=k, n=len(rs),
                        F=st.mean(r["F"] for r in rs), F0=sum(r["F"] == 0 for r in rs),
                        F2=sum(r["F"] == 2 for r in rs) / len(rs),
                        P=st.mean(r["P"] for r in rs), C=st.mean(ctx) if ctx else None, Cn=len(ctx),
                        Q=st.mean(r["Q"] for r in rs), tokens=st.mean(r["tokens"] for r in rs),
                        out_tokens=st.mean(r["out_tokens"] for r in rs)))
    return out


def show(t):
    print(f"{'kind':16} {'version':28} {'n':>3} {'F':>5} {'F0':>3} {'F=2':>5} {'P':>5} {'C':>9} {'Q':>5} {'tok':>5} {'out':>5}")
    for r in t:
        c = f"{r['C']:.2f}/{r['Cn']}" if r["C"] is not None else "-"
        print(f"{r['kind']:16} {r['version']:28} {r['n']:3} {r['F']:5.2f} {r['F0']:3} {r['F2']:5.0%} {r['P']:5.2f} "
              f"{c:>9} {r['Q']:5.2f} {r['tokens']:5.0f} {r['out_tokens']:5.0f}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--scores", nargs="+", required=True)
    ap.add_argument("--samples", nargs="+", required=True)
    ap.add_argument("--cases", default=os.path.join(os.path.dirname(__file__), "cases.jsonl"))
    ap.add_argument("--no-kinds", action="store_true")
    a = ap.parse_args()
    rows = [r for p in a.scores for r in load(p)]
    samples = [s for p in a.samples for s in load(p)]
    show(table(rows, samples, load(a.cases), by_kind=not a.no_kinds))


if __name__ == "__main__":
    main()
