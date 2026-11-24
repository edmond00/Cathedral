"""Blind sheets for the first/second-person experiment, and the merge of their scores.

    python blind.py sheet --sets dev heldout --parts 4 --out sheets/p
        sheets/p1.txt … p4.txt (what the scorers read: cases whole, samples shuffled, versions hidden)
        sheets/p.key.json      (sample id -> case, version, seed)
    python blind.py merge --key sheets/p.key.json --scores sheets/p*.scores.txt --out scored.jsonl

A case's samples (every version, every seed) go to the same sheet, so one scorer compares them side
by side, as RUBRIC.md asks. Score lines: `S4821 F2 I1 K2 P1 C1 R2 Q2 | facts`.
"""
import argparse, collections, glob, json, os, random, re, sys, textwrap

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import person_lib as L      # noqa: E402


def describe(c, neutral, persona):
    r = c["record"]
    bits = [f"kind={r['kind']}", f"persona={persona['id']}", f"reminder={persona.get('reminder2')!r}"]
    if persona.get("style"):
        bits.append(f"style={persona['style']!r}")
    for k in ("place", "observedOther"):
        if r.get(k):
            bits.append(f"{k}={r[k]!r}")
    return "  ".join(bits) + "\n  IN: " + neutral


def sheet(a):
    cases = {c["id"]: c for c in (json.loads(l) for l in open(os.path.join(HERE, "..", "cases.jsonl")))}
    by_case = collections.defaultdict(list)
    for st in a.sets:
        for l in open(os.path.join(HERE, "samples", f"{st}.jsonl")):
            s = json.loads(l)
            by_case[s["case"]].append(s)
    rng = random.Random(a.seed)
    order = sorted(by_case)
    rng.shuffle(order)
    ids = rng.sample(range(1000, 9999), sum(len(v) for v in by_case.values()))
    key, k = {}, 0
    parts = [[] for _ in range(a.parts)]
    for i, cid in enumerate(order):
        ss = by_case[cid][:]
        rng.shuffle(ss)
        first = next((s for s in ss if s["version"] == "first"), ss[0])
        block = [f"## {cid.split(':')[0]}#{i}\n  " + describe(cases[cid], first["neutral"], L.current_persona(cases[cid]))]
        for s in ss:
            sid = f"S{ids[k]}"; k += 1
            key[sid] = dict(case=cid, version=s["version"], seed=s["seed"])
            block.append(textwrap.fill(f"{sid}: {s['text']}", 110, initial_indent="  ", subsequent_indent="        "))
        parts[i % a.parts].append("\n".join(block) + "\n")
    os.makedirs(os.path.dirname(a.out), exist_ok=True)
    for j, p in enumerate(parts, 1):
        open(f"{a.out}{j}.txt", "w", encoding="utf-8").write("\n".join(p))
    json.dump(key, open(a.out + ".key.json", "w"), indent=0)
    print(len(key), "samples in", a.parts, "sheets")


LINE = re.compile(r"^\s*(S\d{4})\s+F(\d)\s+I(\d)\s+K(\d)\s+P(\d)\s+C([01-])\s+R(\d)\s+Q(\d)\s*(?:\|\s*(.*))?$")


def merge(a):
    key = json.load(open(a.key))
    out, seen = [], set()
    for p in a.scores:
        for line in open(p, encoding="utf-8"):
            m = LINE.match(line)
            if not m:
                continue
            sid = m.group(1)
            if sid not in key or sid in seen:
                continue
            seen.add(sid)
            f, i, kk, pp, c, r, q = m.group(2, 3, 4, 5, 6, 7, 8)
            out.append(dict(key[sid], sid=sid, F=int(f), I=int(i), K=int(kk), P=int(pp),
                            C=None if c == "-" else int(c), R=int(r), Q=int(q), facts=(m.group(9) or "").strip()))
    with open(a.out, "w") as fo:
        for r in out:
            fo.write(json.dumps(r) + "\n")
    missing = set(key) - seen
    print(len(out), "scored,", len(missing), "missing")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    sp = ap.add_subparsers(dest="cmd", required=True)
    s1 = sp.add_parser("sheet"); s1.add_argument("--sets", nargs="+", default=["dev"])
    s1.add_argument("--parts", type=int, default=4); s1.add_argument("--out", required=True)
    s1.add_argument("--seed", type=int, default=7)
    s2 = sp.add_parser("merge"); s2.add_argument("--key", required=True)
    s2.add_argument("--scores", nargs="+", required=True); s2.add_argument("--out", required=True)
    a = ap.parse_args()
    sheet(a) if a.cmd == "sheet" else merge(a)
