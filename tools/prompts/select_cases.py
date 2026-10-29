"""Picks the case subsets the measurement runs on, stratified so every kind and every kind of persona
(own, swapped, loosened) is represented, and stable from run to run (seeded by case id).

    python tools/prompts/select_cases.py   # writes sets/iter.txt, sets/final_dev.txt, sets/final_heldout.txt

  iter          the dev cases every round of iteration is measured on
  final_dev     the iteration cases plus as many again, for the before/after report
  final_heldout the held-out playthroughs, never looked at until the end
"""
import collections, hashlib, json, os

HERE = os.path.dirname(os.path.abspath(__file__))


def h(s):
    return hashlib.sha1(s.encode()).hexdigest()


def pick(cases, per_kind, variant_share):
    by = collections.defaultdict(list)
    for c in cases:
        by[(c["record"]["kind"], c["variant"])].append(c)
    out = []
    kinds = sorted({k for k, _ in by})
    for kind in kinds:
        want = per_kind
        for variant, share in variant_share.items():
            pool = sorted(by.get((kind, variant), []), key=lambda c: h(c["id"]))
            n = min(len(pool), max(1, round(want * share)) if pool else 0)
            out += pool[:n]
    return out


def main():
    cases = [json.loads(l) for l in open(os.path.join(HERE, "cases.jsonl"), encoding="utf-8")]
    dev = [c for c in cases if c["split"] == "dev"]
    held = [c for c in cases if c["split"] == "heldout"]
    shares = {"own": 0.5, "swap": 0.25, "loose": 0.25}
    it = pick(dev, 4, shares)
    more = pick([c for c in dev if c not in it], 4, shares)
    fin_h = pick(held, 5, shares)
    os.makedirs(os.path.join(HERE, "sets"), exist_ok=True)
    for name, s in [("iter", it), ("final_dev", it + more), ("final_heldout", fin_h)]:
        open(os.path.join(HERE, "sets", name + ".txt"), "w").write(",".join(c["id"] for c in s))
        cov = collections.Counter(c["record"]["kind"] for c in s)
        print(f"{name:14} {len(s):3} cases  {dict(cov)}  personas={len({c['persona']['id'] for c in s})}")


if __name__ == "__main__":
    main()
