"""Blind scoring: sheets with the version hidden and the order shuffled, and the merge afterwards.

    python tools/prompts/blind.py sheet --samples a.jsonl b.jsonl --cases cases.jsonl --out sheets/r1
        writes r1.txt (what the scorer reads) and r1.key.json (sample id -> case, version, seed)
    python tools/prompts/blind.py merge --key sheets/r1.key.json --scores sheets/r1.scores.txt --out scored.jsonl

The sheet shows, per case, what the game supplied (kind, persona, place, addressee, the neutral line,
anything optional) and the samples of every version for that case shuffled together under opaque ids.
Scores are written one line per sample, in the rubric's order (RUBRIC.md):

    S0412 F2 P1 C- Q2 | +hearth -"learned wayfaring"

F fidelity 0-2, P persona 0-2, C context 0/1 or - for n/a, Q writing 0-2, then the facts after a bar.
"""
import argparse, collections, json, os, random, sys, textwrap

sys.path.insert(0, os.path.dirname(__file__))


def load(path):
    return [json.loads(l) for l in open(path, encoding="utf-8") if l.strip()]


def describe_case(c):
    r = c["record"]
    p = c["persona"]
    bits = [f"kind={r['kind']}", f"persona={p['id']}", f"reminder={p.get('reminder2')!r}"]
    if p.get("style"):
        bits.append(f"style={p['style']!r}")
    for k in ("place", "addressee", "speakerName", "dialogueContext", "observedOther"):
        if r.get(k):
            bits.append(f"{k}={r[k]!r}")
    return "  ".join(bits) + "\n  IN: " + r["neutralText"]


def sheet(a):
    cases = {c["id"]: c for c in load(a.cases)}
    by_case = collections.defaultdict(list)
    # A sample identical to an already-scored one of the same case (the variant did not change that
    # prompt, and the seed is the same) is not shown again: `carry` copies its score instead.
    scored = {}
    for p in a.carry or []:
        for r in load(p):
            scored[(r["case"], r["version"], r["seed"])] = r
    texts = {}
    for path in a.samples:
        for s in load(path):
            texts[(s["case"], s["version"], s["seed"])] = s["text"]
    carried = []
    for path in a.samples:
        for s in load(path):
            if s["case"] not in cases or (a.versions and s["version"] not in a.versions):
                continue
            twin = next((r for (c, v, sd), r in scored.items()
                         if c == s["case"] and sd == s["seed"] and texts.get((c, v, sd)) == s["text"]), None)
            if twin:
                carried.append(dict(twin, version=s["version"], sid="carried"))
                continue
            by_case[s["case"]].append(s)
    if carried:
        with open(a.out + ".carried.jsonl", "w") as f:
            for r in carried:
                f.write(json.dumps(r) + "\n")
    rng = random.Random(a.seed)
    ids = rng.sample(range(1000, 9999), sum(len(v) for v in by_case.values()))
    key, lines, k = {}, [], 0
    order = sorted(by_case)
    rng.shuffle(order)
    for cid in order:
        ss = by_case[cid][:]
        rng.shuffle(ss)
        lines.append(f"## {cid}\n  {describe_case(cases[cid])}")
        for s in ss:
            sid = f"S{ids[k]}"; k += 1
            key[sid] = dict(case=cid, version=s["version"], seed=s["seed"])
            lines.append(textwrap.fill(f"{sid}: {s['text']}", 110, initial_indent="  ", subsequent_indent="        "))
        lines.append("")
    os.makedirs(os.path.dirname(a.out) or ".", exist_ok=True)
    open(a.out + ".txt", "w", encoding="utf-8").write("\n".join(lines))
    json.dump(key, open(a.out + ".key.json", "w"), indent=0)
    print(f"{len(key)} samples over {len(order)} cases -> {a.out}.txt; {len(carried)} identical to a scored one, carried")


def merge(a):
    key = json.load(open(a.key))
    out = open(a.out, "a", encoding="utf-8")
    n = 0
    for line in open(a.scores, encoding="utf-8"):
        line = line.strip()
        if not line.startswith("S"):
            continue
        head, _, facts = line.partition("|")
        parts = head.split()
        sid = parts[0].rstrip(":")
        sc = {p[0]: p[1:] for p in parts[1:]}
        rec = dict(key[sid], sid=sid, F=int(sc["F"]), P=int(sc["P"]),
                   C=None if sc.get("C", "-") == "-" else int(sc["C"]), Q=int(sc["Q"]), facts=facts.strip())
        out.write(json.dumps(rec) + "\n"); n += 1
    print(f"merged {n} scores into {a.out}")


def main():
    ap = argparse.ArgumentParser()
    sub = ap.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("sheet")
    s.add_argument("--samples", nargs="+", required=True)
    s.add_argument("--cases", required=True)
    s.add_argument("--versions", nargs="*")
    s.add_argument("--out", required=True)
    s.add_argument("--seed", type=int, default=7)
    s.add_argument("--carry", nargs="*")
    m = sub.add_parser("merge")
    m.add_argument("--key", required=True)
    m.add_argument("--scores", required=True)
    m.add_argument("--out", required=True)
    a = ap.parse_args()
    (sheet if a.cmd == "sheet" else merge)(a)


if __name__ == "__main__":
    main()
