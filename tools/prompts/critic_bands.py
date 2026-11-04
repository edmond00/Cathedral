"""What a critic run means for a character at each proficiency band.

The side of a verdict is not what a player meets: a verdict clears a band or it does not
(ToolCombinationRules.Threshold — the very tool at Low, a proper substitute at Medium, a clumsy one at
High). So for a body at band B, an item labelled as helping is *usable* when the verdict passed AND its
degree demands no more than B; one labelled as not helping must never be usable at any band.

    should use at B   labelled help, and its labelled band is B or below (an unlabelled degree counts as High)
    usable            the verdict passed and its degree's band is B or below

    python tools/prompts/critic_bands.py run.jsonl [run.jsonl ...] [--set tune|check|survey|dev|held] [--degree RULE]

RULE: `answer` (default) reads the degree off the run's own answer — for `current`, the strict
question's, as ItemUseCritic.WeighAsync gives it. `strict-first-token` re-derives it from a pair run's
recorded readings as the strict question's likeliest help FIRST TOKEN, which cannot tell serves_well
from serves_poorly (both "s") — kept to show why the greedy reply is read too.
"""
import argparse, json, math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import critic_bench as B

BANDS = ["low", "medium", "high"]
RANK = {b: i for i, b in enumerate(BANDS)}
HELP_IDS = {"ItemAppropriateness": ["clearly_helps", "plausibly_helps", "detoured_use"],
            "ToolSubstitution": ["is_the_tool", "serves_well", "serves_poorly"]}


def degree(r, rule):
    """The band the verdict demands, or None when it refused."""
    if not r["help"]:
        return None
    if rule == "answer" or "strict_lp" not in r:
        return B.BAND.get(r["answer"])
    lp = r["strict_lp"][:3]          # the three help options, in order: low, medium, high
    return BANDS[max(range(3), key=lambda i: lp[i])]


def table(rows, rule):
    out = []
    for b in BANDS:
        should = [r for r in rows if r["label"] == "help" and RANK[r["band"] or "high"] <= RANK[b]]
        usable = [r for r in should if (d := degree(r, rule)) is not None and RANK[d] <= RANK[b]]
        wrong = [r for r in rows if r["label"] == "no" and (d := degree(r, rule)) is not None and RANK[d] <= RANK[b]]
        nos = [r for r in rows if r["label"] == "no"]
        out.append(f"{b:6} usable {len(usable):3}/{len(should):<3} ({len(usable) / max(1, len(should)):5.1%})"
                   f"   wrong item usable {len(wrong)}/{len(nos)}")
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("runs", nargs="+")
    ap.add_argument("--set")
    ap.add_argument("--degree", default="answer", choices=["answer", "strict-first-token"])
    a = ap.parse_args()
    keep = {c["id"] for c in B.cases(a.set)} if a.set else None
    for f in a.runs:
        rows = [json.loads(l) for l in open(f)]
        rows = [r for r in rows if keep is None or r["case"] in keep]
        print(f"{os.path.basename(f)}  ({len(rows)} verdicts, degree: {a.degree})")
        for line in table(rows, a.degree):
            print("   ", line)


if __name__ == "__main__":
    main()
