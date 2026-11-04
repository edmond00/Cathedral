"""How a passing verdict's degree is read, tried against the labelled bands.

A pair run records the strict question's log-probability of each whole id (`strict_seq`, read off one
greedy reply's per-token lists — see critic_bench.id_logprob). The shipped read takes the greedy reply,
i.e. the likeliest id. The alternative tried here adds an offset to the two better degrees before
taking the likeliest:

    degree = argmax(lp[best] + b1, lp[middle] + b2, lp[clumsy])

The offsets are fitted on the tune half and reported on the check half. Over-grant = a better degree
than the label (a weaker hand may use it); under-grant = a worse one (a hand that should may not).

    python tools/prompts/critic_degree.py run.jsonl [--tree ToolSubstitution|ItemAppropriateness]
"""
import argparse, json, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import critic_bench as B

RANK = {"low": 0, "medium": 1, "high": 2}


def degree(r, b1, b2):
    lp = r["strict_seq"][:3]
    score = [lp[0] + b1, lp[1] + b2, lp[2]]
    return ["low", "medium", "high"][max(range(3), key=lambda i: score[i])]


def score(rows, b1, b2):
    exact = over = under = 0
    for r in rows:
        got, want = RANK[degree(r, b1, b2)], RANK[r["band"]]
        exact += got == want
        over += got < want
        under += got > want
    return exact, over, under


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("run")
    ap.add_argument("--tree", default="ToolSubstitution")
    a = ap.parse_args()
    rows = [json.loads(l) for l in open(a.run)]
    rows = [r for r in rows if r["tree"] == a.tree and r["help"] and r["label"] == "help" and r["band"]
            and r.get("strict_seq")]
    halves = {k: {c["id"] for c in B.cases(k)} for k in ("tune", "check")}
    tune = [r for r in rows if r["case"] in halves["tune"]]
    check = [r for r in rows if r["case"] in halves["check"]]
    grid = [x / 2 for x in range(0, 13)]
    best = None
    for b1 in grid:
        for b2 in grid:
            e, o, u = score(tune, b1, b2)
            key = (e - o, -o, -(b1 + b2))       # exact, then fewer over-grants, then the smaller offsets
            if best is None or key > best[0]:
                best = (key, b1, b2)
    _, b1, b2 = best
    for name, part in (("tune", tune), ("check", check), ("all", rows)):
        e0, o0, u0 = score(part, 0, 0)
        e, o, u = score(part, b1, b2)
        print(f"{a.tree} {name:5} n={len(part):2}   greedy: exact {e0} over {o0} under {u0}   "
              f"offsets +{b1}/+{b2}: exact {e} over {o} under {u}")


if __name__ == "__main__":
    main()
